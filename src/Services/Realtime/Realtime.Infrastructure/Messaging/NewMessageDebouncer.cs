using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Realtime.Application.Clients;
using RecruitmentPlatform.Contracts.Realtime;

namespace Realtime.Infrastructure.Messaging;

/// <summary>
/// Debounces FCM push notifications theo từng user (toUserId).
/// Gom TẤT CẢ tin nhắn mới từ mọi room trong cửa sổ 2 giây thành 1 FCM push duy nhất.
/// 
/// Lưu ý: SignalR push (realtime) được thực hiện ngay lập tức ở Consumer với đầy đủ thông tin.
/// Debouncer này chỉ phụ trách FCM aggregation (offline delivery).
/// 
/// Ví dụ: Room A gửi 2 tin, Room B gửi 3 tin trong 2 giây
///   → FCM push 1 lần: { toUserId: X, totalNewMessages: 5 }
/// </summary>
public sealed class NewMessageDebouncer : IDisposable
{
    private sealed class PendingBatch
    {
        public int   ToUserId    { get; }
        public int   TotalCount  { get; set; }
        public NewMessageNotificationEvent? LatestEvent { get; set; }
        public Timer? Timer      { get; set; }

        public PendingBatch(int toUserId) => ToUserId = toUserId;
    }

    private static readonly TimeSpan DebounceWindow = TimeSpan.FromSeconds(2);

    private readonly INotificationServiceClient? _notificationClient;
    private readonly ILogger<NewMessageDebouncer> _logger;
    private readonly ConcurrentDictionary<int, PendingBatch> _pending = new();

    public NewMessageDebouncer(
        INotificationServiceClient? notificationClient,
        ILogger<NewMessageDebouncer> logger)
    {
        _notificationClient = notificationClient;
        _logger      = logger;
    }

    /// <summary>
    /// Đăng ký 1 tin nhắn mới để FCM debounce. Cộng dồn vào batch của toUserId và reset/bắt đầu timer 2s.
    /// </summary>
    public void Add(NewMessageNotificationEvent evt)
    {
        var batch = _pending.GetOrAdd(evt.ToUserId, uid => new PendingBatch(uid));

        lock (batch)
        {
            batch.TotalCount++;
            batch.LatestEvent = evt;

            if (batch.Timer == null)
            {
                // Tin nhắn đầu tiên trong window — bắt đầu đếm ngược
                batch.Timer = new Timer(OnTimerFired, evt.ToUserId, DebounceWindow, Timeout.InfiniteTimeSpan);
            }
            else
            {
                // Tin nhắn tiếp theo — reset timer
                batch.Timer.Change(DebounceWindow, Timeout.InfiniteTimeSpan);
            }
        }

        _logger.LogDebug(
            "Debouncer: buffered FCM msg for user {ToUserId}, total pending={Total}",
            evt.ToUserId, batch.TotalCount);
    }

    private void OnTimerFired(object? state)
    {
        if (state is not int toUserId) return;

        if (!_pending.TryRemove(toUserId, out var batch)) return;

        Timer? timer;
        lock (batch)
        {
            timer       = batch.Timer;
            batch.Timer = null;
        }
        timer?.Dispose();

        _logger.LogInformation(
            "Debouncer: FCM push {Total} new message(s) to user {ToUserId}",
            batch.TotalCount, toUserId);

        // Send FCM push notification for offline delivery
        if (_notificationClient != null && batch.LatestEvent != null)
        {
            var senderName = batch.LatestEvent.Author?.Name ?? batch.LatestEvent.FromUserId.ToString();
            var senderAvatar = batch.LatestEvent.Author?.Avatar ?? string.Empty;
            var preview = batch.LatestEvent.Content;

            _ = _notificationClient.SendAggregatedMessageNotificationAsync(
                    toUserId,
                    batch.TotalCount,
                    senderName,
                    senderAvatar,
                    preview,
                    batch.LatestEvent.MessageId,
                    batch.LatestEvent.RoomId)
                .ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        _logger.LogError(
                            task.Exception,
                            "Error sending FCM notification to user {ToUserId}",
                            toUserId);
                    }
                });
        }
    }

    public void Dispose()
    {
        foreach (var batch in _pending.Values)
            lock (batch) { batch.Timer?.Dispose(); }
        _pending.Clear();
    }
}

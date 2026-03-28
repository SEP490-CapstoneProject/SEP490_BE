using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Interfaces;
using System.Text.Json;

namespace Payment.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPlanPriceProvider _planPriceProvider;
    private readonly IPaymentProvider _paymentProvider;  // ✅ Single provider (PayOS)
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IPlanPriceProvider planPriceProvider,
        IPaymentProvider paymentProvider,  // ✅ Inject single provider
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _planPriceProvider = planPriceProvider;
        _paymentProvider = paymentProvider;
        _logger = logger;
    }

    public async Task<CreatePaymentResponse> CreatePaymentAsync(int userId, CreatePaymentRequest request)
    {
        // 1. Get plan price from DB (NOT from client)
        var planInfo = await _planPriceProvider.GetPlanPriceAsync(request.PlanId);
        if (planInfo == null)
            throw new InvalidOperationException($"Plan {request.PlanId} not found");

        // 2. Check for duplicate pending payment (reuse if exists)
        var existingPayment = await _paymentRepository.GetPendingByUserAndPlanAsync(userId, request.PlanId);
        if (existingPayment != null)
        {
            _logger.LogInformation("Reusing existing pending payment {PaymentId} for user {UserId}", 
                existingPayment.Id, userId);
            
            return new CreatePaymentResponse
            {
                PaymentId = existingPayment.Id,
                PaymentUrl = existingPayment.PaymentUrl ?? "",
                OrderCode = existingPayment.OrderCode
            };
        }

        // 3. Create new payment
        var payment = new PaymentEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = request.PlanId,
            SubscriptionId = request.SubscriptionId,  // Store subscription reference
            Amount = planInfo.Value.price,
            Currency = "VND",
            Provider = PaymentProvider.PayOS,  // ✅ Always PayOS
            Status = PaymentStatus.Pending,
            OrderCode = GenerateOrderCode().ToString(),  // Convert long to string
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow
        };

        // 4. Create payment via PayOS provider
        var result = await _paymentProvider.CreatePaymentAsync(payment);
        
        // 5. Update payment with provider result
        payment.PaymentUrl = result.CheckoutUrl;
        payment.Metadata = JsonSerializer.Serialize(new 
        { 
            PaymentLinkId = result.PaymentLinkId,
            Provider = "PayOS"
        });

        // 6. Save to DB
        await _paymentRepository.CreateAsync(payment);

        _logger.LogInformation("Created PayOS payment {PaymentId} for user {UserId}, plan {PlanId}, amount {Amount}", 
            payment.Id, userId, request.PlanId, payment.Amount);

        return new CreatePaymentResponse
        {
            PaymentId = payment.Id,
            PaymentUrl = payment.PaymentUrl,
            OrderCode = payment.OrderCode
        };
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(Guid paymentId)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null) return null;

        return MapToDto(payment);
    }

    public async Task<List<PaymentDto>> GetUserPaymentsAsync(int userId, int page = 1, int pageSize = 10)
    {
        var payments = await _paymentRepository.GetByUserIdAsync(userId, page, pageSize);
        return payments.Select(MapToDto).ToList();
    }

    public async Task<PaginatedResult<PaymentDto>> GetAllPaymentsAsync(AdminPaymentFilterRequest filter)
    {
        var payments = await _paymentRepository.GetAllAsync(
            filter.UserId, 
            filter.Status, 
            filter.FromDate, 
            filter.ToDate, 
            filter.Page, 
            filter.PageSize);

        var totalCount = await _paymentRepository.GetTotalCountAsync(
            filter.UserId, 
            filter.Status, 
            filter.FromDate, 
            filter.ToDate);

        return new PaginatedResult<PaymentDto>
        {
            Items = payments.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PaymentDto?> GetPaymentByOrderCodeAsync(string orderCode)
    {
        var payment = await _paymentRepository.GetByOrderCodeAsync(orderCode);
        if (payment == null) return null;

        return MapToDto(payment);
    }

    private static PaymentDto MapToDto(PaymentEntity payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            UserId = payment.UserId,
            PlanId = payment.PlanId,
            SubscriptionId = payment.SubscriptionId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Provider = payment.Provider.ToString(),
            Status = payment.Status.ToString(),
            TransactionId = payment.TransactionId,
            OrderCode = payment.OrderCode,
            CreatedAt = payment.CreatedAt,
            PaidAt = payment.PaidAt
        };
    }

    /// <summary>
    /// Generates collision-safe order code using timestamp + random.
    /// Format: Unix timestamp milliseconds + random 4 digits
    /// Result: 15-16 digit unique number for PayOS.
    /// </summary>
    private static long GenerateOrderCode()
    {
        var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Random.Shared.Next(1000, 9999);
        
        return timestampMs * 10000 + random;
    }
}

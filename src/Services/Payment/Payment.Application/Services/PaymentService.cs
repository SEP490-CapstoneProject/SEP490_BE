using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Interfaces;

namespace Payment.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPlanPriceProvider _planPriceProvider;
    private readonly Dictionary<PaymentProvider, IPaymentProvider> _paymentProviders;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IPlanPriceProvider planPriceProvider,
        IEnumerable<IPaymentProvider> paymentProviders,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _planPriceProvider = planPriceProvider;
        _logger = logger;

        // Map providers by index (VNPay first, MoMo second in DI registration)
        var providerList = paymentProviders.ToList();
        _paymentProviders = new Dictionary<PaymentProvider, IPaymentProvider>();
        if (providerList.Count >= 1)
            _paymentProviders[PaymentProvider.VNPay] = providerList[0];
        if (providerList.Count >= 2)
            _paymentProviders[PaymentProvider.MoMo] = providerList[1];
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
            Provider = request.Provider,
            Status = PaymentStatus.Pending,
            OrderCode = GenerateOrderCode(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow
        };

        // 4. Get provider and create payment URL
        if (!_paymentProviders.TryGetValue(request.Provider, out var provider))
            throw new InvalidOperationException($"Provider {request.Provider} not supported");

        payment.PaymentUrl = await provider.CreatePaymentUrlAsync(payment);

        // 5. Save to DB
        await _paymentRepository.CreateAsync(payment);

        _logger.LogInformation("Created payment {PaymentId} for user {UserId}, plan {PlanId}, amount {Amount}", 
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

    private static string GenerateOrderCode()
    {
        return $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
    }
}

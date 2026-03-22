using Payment.Application.DTOs;
using Payment.Domain.Enums;

namespace Payment.Application.Interfaces;

public interface IPaymentService
{
    Task<CreatePaymentResponse> CreatePaymentAsync(int userId, CreatePaymentRequest request);
    Task<PaymentDto?> GetPaymentByIdAsync(Guid paymentId);
    Task<List<PaymentDto>> GetUserPaymentsAsync(int userId, int page = 1, int pageSize = 10);
    Task<PaginatedResult<PaymentDto>> GetAllPaymentsAsync(AdminPaymentFilterRequest filter);
    Task<PaymentDto?> GetPaymentByOrderCodeAsync(string orderCode);
}

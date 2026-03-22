namespace Payment.Application.Interfaces;

public interface IPaymentEventPublisher
{
    Task PublishPaymentSucceededAsync(Guid paymentId, int userId, int planId, decimal amount, string provider);
}

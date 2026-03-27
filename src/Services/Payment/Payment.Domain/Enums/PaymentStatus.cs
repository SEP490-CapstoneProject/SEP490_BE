namespace Payment.Domain.Enums;

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Success = 2,
    Failed = 3,
    Cancelled = 4,
    Expired = 5
}

namespace Payment.Domain.Enums;

public enum OutboxStatus
{
    Pending = 0,
    Published = 1,
    Failed = 2,
    DeadLetter = 3
}

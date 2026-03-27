namespace Payment.Application.Interfaces;

public interface IPlanPriceProvider
{
    Task<(decimal price, string planName)?> GetPlanPriceAsync(int planId);
}

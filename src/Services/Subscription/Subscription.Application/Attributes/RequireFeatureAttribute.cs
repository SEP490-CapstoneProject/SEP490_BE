namespace Subscription.Application.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequireFeatureAttribute : Attribute
{
    public string FeatureKey { get; }
    public bool IncrementUsage { get; }

    public RequireFeatureAttribute(string featureKey, bool incrementUsage = true)
    {
        FeatureKey = featureKey;
        IncrementUsage = incrementUsage;
    }
}

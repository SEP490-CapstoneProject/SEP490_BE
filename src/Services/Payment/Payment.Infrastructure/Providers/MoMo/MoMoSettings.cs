namespace Payment.Infrastructure.Providers.MoMo;

public class MoMoSettings
{
    public string PartnerCode { get; set; } = null!;
    public string AccessKey { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public string Endpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";
    public string ReturnUrl { get; set; } = null!;
    public string IpnUrl { get; set; } = null!;
    public string RequestType { get; set; } = "captureWallet";
}

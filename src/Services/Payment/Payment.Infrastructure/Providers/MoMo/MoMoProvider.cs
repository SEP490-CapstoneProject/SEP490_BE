using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Payment.Application.DTOs;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Providers.MoMo;

/// <summary>
/// LEGACY: MoMo provider - no longer in active use
/// Kept for reference and potential rollback
/// </summary>
[Obsolete("Migrated to PayOS. Use PayOSProvider instead.")]
public class MoMoProvider
{
    private readonly MoMoSettings _settings;
    private readonly HttpClient _httpClient;

    public MoMoProvider(IOptions<MoMoSettings> settings, HttpClient httpClient)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
    }

    public async Task<string> CreatePaymentUrlAsync(PaymentEntity payment)
    {
        var requestId = Guid.NewGuid().ToString();
        var rawSignature = $"accessKey={_settings.AccessKey}&amount={payment.Amount:0}&extraData=&ipnUrl={_settings.IpnUrl}&orderId={payment.OrderCode}&orderInfo=Payment for plan {payment.PlanId}&partnerCode={_settings.PartnerCode}&redirectUrl={_settings.ReturnUrl}&requestId={requestId}&requestType={_settings.RequestType}";
        
        var signature = ComputeHmacSha256(rawSignature, _settings.SecretKey);

        var requestBody = new
        {
            partnerCode = _settings.PartnerCode,
            accessKey = _settings.AccessKey,
            requestId,
            amount = payment.Amount.ToString("0"),
            orderId = payment.OrderCode,
            orderInfo = $"Payment for plan {payment.PlanId}",
            redirectUrl = _settings.ReturnUrl,
            ipnUrl = _settings.IpnUrl,
            requestType = _settings.RequestType,
            extraData = "",
            lang = "vi",
            signature
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(_settings.Endpoint, content);
        var responseStr = await response.Content.ReadAsStringAsync();
        
        var jsonDoc = JsonDocument.Parse(responseStr);
        var payUrl = jsonDoc.RootElement.GetProperty("payUrl").GetString();
        
        return payUrl ?? throw new InvalidOperationException("MoMo response missing payUrl");
    }

    public WebhookResult ParseWebhookData(IDictionary<string, string> queryParams)
    {
        var result = new WebhookResult
        {
            RawData = JsonSerializer.Serialize(queryParams)
        };

        queryParams.TryGetValue("orderId", out var orderCode);
        queryParams.TryGetValue("transId", out var transactionId);
        queryParams.TryGetValue("amount", out var amountStr);
        queryParams.TryGetValue("resultCode", out var resultCode);
        queryParams.TryGetValue("message", out var message);

        result.OrderCode = orderCode;
        result.TransactionId = transactionId;
        result.Currency = "VND";

        if (decimal.TryParse(amountStr, out var amount))
            result.Amount = amount;

        result.IsSuccess = resultCode == "0";
        result.IsValid = true;

        if (!result.IsSuccess)
            result.ErrorMessage = message ?? $"Lỗi MoMo: {resultCode}";

        return result;
    }

    public bool ValidateSignature(IDictionary<string, string> queryParams)
    {
        if (!queryParams.TryGetValue("signature", out var receivedSignature))
            return false;

        queryParams.TryGetValue("partnerCode", out var partnerCode);
        queryParams.TryGetValue("orderId", out var orderId);
        queryParams.TryGetValue("requestId", out var requestId);
        queryParams.TryGetValue("amount", out var amount);
        queryParams.TryGetValue("orderInfo", out var orderInfo);
        queryParams.TryGetValue("orderType", out var orderType);
        queryParams.TryGetValue("transId", out var transId);
        queryParams.TryGetValue("resultCode", out var resultCode);
        queryParams.TryGetValue("message", out var message);
        queryParams.TryGetValue("payType", out var payType);
        queryParams.TryGetValue("responseTime", out var responseTime);
        queryParams.TryGetValue("extraData", out var extraData);

        var rawSignature = $"accessKey={_settings.AccessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";
        
        var expectedSignature = ComputeHmacSha256(rawSignature, _settings.SecretKey);

        return string.Equals(expectedSignature, receivedSignature, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}

using Microsoft.Extensions.Options;
using Payment.Application.DTOs;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Providers.VNPay;

/// <summary>
/// LEGACY: VNPay provider - no longer in active use
/// Kept for reference and potential rollback
/// </summary>
[Obsolete("Migrated to PayOS. Use PayOSProvider instead.")]
public class VnPayProvider
{
    private readonly VnPaySettings _settings;

    public VnPayProvider(IOptions<VnPaySettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<string> CreatePaymentUrlAsync(PaymentEntity payment)
    {
        var requestData = new SortedDictionary<string, string>
        {
            { "vnp_Version", _settings.Version },
            { "vnp_Command", _settings.Command },
            { "vnp_TmnCode", _settings.TmnCode },
            { "vnp_Amount", ((long)(payment.Amount * 100)).ToString() },
            { "vnp_CurrCode", _settings.CurrCode },
            { "vnp_TxnRef", payment.OrderCode },
            { "vnp_OrderInfo", $"Payment for plan {payment.PlanId}" },
            { "vnp_OrderType", "billpayment" },
            { "vnp_Locale", _settings.Locale },
            { "vnp_ReturnUrl", _settings.ReturnUrl },
            { "vnp_IpnUrl", _settings.IpnUrl },
            { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
            { "vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss") }
        };

        var url = VnPayLibrary.CreateRequestUrl(_settings.BaseUrl, requestData, _settings.HashSecret);
        return Task.FromResult(url);
    }

    public WebhookResult ParseWebhookData(IDictionary<string, string> queryParams)
    {
        var result = new WebhookResult
        {
            RawData = System.Text.Json.JsonSerializer.Serialize(queryParams)
        };

        queryParams.TryGetValue("vnp_TxnRef", out var orderCode);
        queryParams.TryGetValue("vnp_TransactionNo", out var transactionId);
        queryParams.TryGetValue("vnp_Amount", out var amountStr);
        queryParams.TryGetValue("vnp_ResponseCode", out var responseCode);

        result.OrderCode = orderCode;
        result.TransactionId = transactionId;
        result.Currency = "VND";

        if (decimal.TryParse(amountStr, out var amount))
            result.Amount = amount / 100;

        result.IsSuccess = responseCode == "00";
        result.IsValid = true;

        if (!result.IsSuccess)
            result.ErrorMessage = GetErrorMessage(responseCode);

        return result;
    }

    public bool ValidateSignature(IDictionary<string, string> queryParams)
    {
        return VnPayLibrary.ValidateSignature(queryParams, _settings.HashSecret);
    }

    private static string GetErrorMessage(string? responseCode)
    {
        return responseCode switch
        {
            "07" => "Trừ tiền thành công, giao dịch nghi ngờ",
            "09" => "Giao dịch không thành công: Thẻ/Tài khoản chưa đăng ký InternetBanking",
            "10" => "Giao dịch không thành công: Sai quá 3 lần",
            "11" => "Giao dịch không thành công: Hết thời gian chờ thanh toán",
            "12" => "Giao dịch không thành công: Thẻ/Tài khoản bị khóa",
            "13" => "Giao dịch không thành công: Sai OTP",
            "24" => "Giao dịch không thành công: Khách hàng hủy",
            "51" => "Giao dịch không thành công: Không đủ số dư",
            "65" => "Giao dịch không thành công: Vượt hạn mức",
            "75" => "Ngân hàng đang bảo trì",
            "79" => "Sai mật khẩu quá số lần",
            _ => $"Lỗi không xác định: {responseCode}"
        };
    }
}

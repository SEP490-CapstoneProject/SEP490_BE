using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Payment.Infrastructure.Providers.VNPay;

public static class VnPayLibrary
{
    public static string CreateRequestUrl(string baseUrl, SortedDictionary<string, string> requestData, string hashSecret)
    {
        var queryString = BuildQueryString(requestData);
        var signData = string.Join("&", requestData.Select(kv => $"{kv.Key}={kv.Value}"));
        var secureHash = HmacSha512(hashSecret, signData);
        
        return $"{baseUrl}?{queryString}&vnp_SecureHash={secureHash}";
    }

    public static bool ValidateSignature(IDictionary<string, string> queryParams, string hashSecret)
    {
        if (!queryParams.TryGetValue("vnp_SecureHash", out var receivedHash))
            return false;

        var sortedParams = new SortedDictionary<string, string>(
            queryParams.Where(kv => kv.Key.StartsWith("vnp_") && kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                       .ToDictionary(kv => kv.Key, kv => kv.Value)
        );

        var signData = string.Join("&", sortedParams.Select(kv => $"{kv.Key}={kv.Value}"));
        var expectedHash = HmacSha512(hashSecret, signData);

        return string.Equals(expectedHash, receivedHash, StringComparison.OrdinalIgnoreCase);
    }

    public static string HmacSha512(string key, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private static string BuildQueryString(SortedDictionary<string, string> data)
    {
        return string.Join("&", data.Select(kv => $"{kv.Key}={HttpUtility.UrlEncode(kv.Value)}"));
    }

    public static string GenerateOrderCode()
    {
        return $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
    }
}

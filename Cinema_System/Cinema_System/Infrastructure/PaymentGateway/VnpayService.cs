using System.Net;
using System.Security.Cryptography;
using System.Text;
using Cinema_System.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cinema_System.Infrastructure.PaymentGateway;

// Triển khai cổng VNPay theo chuẩn vpcpay (ký HMAC-SHA512 trên chuỗi tham số đã URL-encode).
public class VnpayService : IVnpayService
{
    private readonly VnpaySettings _s;

    public VnpayService(IOptions<VnpaySettings> options)
    {
        _s = options.Value;
    }

    public bool Enabled => _s.Enabled;

    public string CreatePaymentUrl(decimal amount, string txnRef, string orderInfo, string ipAddr, string returnUrl)
    {
        // Tham số bắt buộc, sắp xếp theo thứ tự khóa.
        var data = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _s.Version,
            ["vnp_Command"] = _s.Command,
            ["vnp_TmnCode"] = _s.TmnCode,
            ["vnp_Amount"] = ((long)(amount * 100)).ToString(),   // VNPay nhân 100
            ["vnp_CurrCode"] = _s.CurrCode,
            ["vnp_TxnRef"] = txnRef,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = _s.Locale,
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_IpAddr"] = ipAddr,
            ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss")
        };

        var hashData = BuildEncodedString(data);
        var secureHash = HmacSha512(_s.HashSecret, hashData);
        return $"{_s.BaseUrl}?{hashData}&vnp_SecureHash={secureHash}";
    }

    public VnpayReturnResult ValidateReturn(IQueryCollection query)
    {
        // Lấy tất cả tham số vnp_ (trừ chữ ký) để tính lại hash.
        var data = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in query)
        {
            if (key.StartsWith("vnp_") && key != "vnp_SecureHash" && key != "vnp_SecureHashType")
                data[key] = value.ToString();
        }

        var hashData = BuildEncodedString(data);
        var computed = HmacSha512(_s.HashSecret, hashData);
        var received = query["vnp_SecureHash"].ToString();

        var valid = computed.Equals(received, StringComparison.OrdinalIgnoreCase);
        return new VnpayReturnResult(valid, query["vnp_TxnRef"].ToString(), query["vnp_ResponseCode"].ToString());
    }

    // Ghép chuỗi key=value (đã URL-encode) nối bằng & theo thứ tự khóa — đúng chuẩn VNPay demo.
    private static string BuildEncodedString(SortedDictionary<string, string> data)
    {
        var sb = new StringBuilder();
        foreach (var kv in data)
        {
            if (string.IsNullOrEmpty(kv.Value)) continue;
            if (sb.Length > 0) sb.Append('&');
            sb.Append(WebUtility.UrlEncode(kv.Key)).Append('=').Append(WebUtility.UrlEncode(kv.Value));
        }
        return sb.ToString();
    }

    private static string HmacSha512(string key, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

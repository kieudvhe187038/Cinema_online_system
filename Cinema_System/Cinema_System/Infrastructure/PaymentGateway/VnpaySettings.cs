namespace Cinema_System.Infrastructure.PaymentGateway;

// Cấu hình cổng VNPay (sandbox). TmnCode/HashSecret lấy từ .env (Vnpay__TmnCode, Vnpay__HashSecret).
public class VnpaySettings
{
    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string Version { get; set; } = "2.1.0";
    public string Command { get; set; } = "pay";
    public string CurrCode { get; set; } = "VND";
    public string Locale { get; set; } = "vn";

    // Chỉ bật VNPay thật khi đã có đủ TmnCode + HashSecret; ngược lại fallback thanh toán giả.
    public bool Enabled => !string.IsNullOrWhiteSpace(TmnCode) && !string.IsNullOrWhiteSpace(HashSecret);
}

using Microsoft.AspNetCore.Http;

namespace Cinema_System.Application.Interfaces;

// Dịch vụ cổng thanh toán VNPay (sandbox): tạo URL thanh toán + xác thực kết quả trả về.
public interface IVnpayService
{
    // VNPay đã được cấu hình (có TmnCode + HashSecret) hay chưa.
    bool Enabled { get; }

    // Tạo URL thanh toán có ký HMAC-SHA512 để redirect người dùng sang VNPay.
    string CreatePaymentUrl(decimal amount, string txnRef, string orderInfo, string ipAddr, string returnUrl);

    // Xác thực chữ ký và đọc kết quả từ query VNPay trả về.
    VnpayReturnResult ValidateReturn(IQueryCollection query);
}

// Kết quả xác thực callback VNPay.
public record VnpayReturnResult(bool Valid, string TxnRef, string ResponseCode);

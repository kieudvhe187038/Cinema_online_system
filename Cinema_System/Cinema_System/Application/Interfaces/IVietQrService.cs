namespace Cinema_System.Application.Interfaces;

// Tạo mã QR chuyển khoản theo chuẩn VietQR (qua API ảnh img.vietqr.io).
public interface IVietQrService
{
    bool Enabled { get; }
    string BankCode { get; }
    string AccountNo { get; }
    string AccountName { get; }

    // Tạo URL ảnh QR kèm số tiền + nội dung chuyển khoản.
    string BuildQrUrl(decimal amount, string content);
}

namespace Cinema_System.Infrastructure.PaymentGateway;

// Cấu hình VietQR (QR chuyển khoản ngân hàng). Thông tin TK lấy từ .env: VietQr__BankCode, VietQr__AccountNo, VietQr__AccountName.
public class VietQrSettings
{
    public string BankCode { get; set; } = string.Empty;     // BIN hoặc mã ngân hàng (vd "970436" hoặc "VCB")
    public string AccountNo { get; set; } = string.Empty;    // Số tài khoản nhận tiền
    public string AccountName { get; set; } = string.Empty;  // Tên chủ tài khoản
    public string Template { get; set; } = "compact2";       // Kiểu ảnh QR của img.vietqr.io

    // Chỉ bật VietQR khi đã có mã ngân hàng + số tài khoản.
    public bool Enabled => !string.IsNullOrWhiteSpace(BankCode) && !string.IsNullOrWhiteSpace(AccountNo);
}

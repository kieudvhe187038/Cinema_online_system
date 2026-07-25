using Cinema_System.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Cinema_System.Infrastructure.PaymentGateway;

// Tạo URL ảnh QR VietQR từ thông tin tài khoản + số tiền + nội dung.
public class VietQrService : IVietQrService
{
    private readonly VietQrSettings _s;

    public VietQrService(IOptions<VietQrSettings> options)
    {
        _s = options.Value;
    }

    public bool Enabled => _s.Enabled;
    public string BankCode => _s.BankCode;
    public string AccountNo => _s.AccountNo;
    public string AccountName => _s.AccountName;

    public string BuildQrUrl(decimal amount, string content)
    {
        var amt = ((long)amount).ToString();
        var info = Uri.EscapeDataString(content);
        var name = Uri.EscapeDataString(_s.AccountName);
        // https://www.vietqr.io/danh-sach-api/api-tao-ma-qr/
        return $"https://img.vietqr.io/image/{_s.BankCode}-{_s.AccountNo}-{_s.Template}.png?amount={amt}&addInfo={info}&accountName={name}";
    }
}

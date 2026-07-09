namespace Cinema_System.Application.DTOs;

public class VatDTO
{
    public Guid Id { get; set; }
    public decimal VatRate { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }

    // Cờ phụ tính ở Service: VAT đã được dùng trong đơn đặt vé (chặn xóa).
    public bool HasUsage { get; set; }
}

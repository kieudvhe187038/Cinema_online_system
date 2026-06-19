using System.ComponentModel.DataAnnotations;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

public class RoomFormViewModel : IValidatableObject
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên phòng")]
    [StringLength(100, ErrorMessage = "Tên phòng tối đa 100 ký tự")]
    [Display(Name = "Tên phòng")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng chọn loại phòng")]
    [Display(Name = "Loại phòng")]
    public Guid? RoomTypeId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = RoomStatus.Active;

    [Range(1, RoomPolicy.MaxRows, ErrorMessage = "Số hàng phải từ 1 đến 40")]
    [Display(Name = "Số hàng")]
    public int TotalRow { get; set; } = 8;

    [Range(1, RoomPolicy.MaxColumns, ErrorMessage = "Số cột phải từ 1 đến 40")]
    [Display(Name = "Số cột")]
    public int TotalColumns { get; set; } = 10;

    /// <summary>Sơ đồ ghế dạng JSON do trình thiết kế gửi lên: <c>[{row,startColumn,seatTypeId}]</c>.</summary>
    public string? SeatsJson { get; set; }

    /// <summary>Danh sách loại phòng cho dropdown (không bind từ form).</summary>
    public IReadOnlyList<(Guid Id, string Name)> RoomTypeOptions { get; set; } =
        new List<(Guid, string)>();

    /// <summary>Các loại ghế dùng làm palette vẽ (không bind từ form).</summary>
    public IReadOnlyList<SeatTypeOptionDTO> SeatTypes { get; set; } = new List<SeatTypeOptionDTO>();

    /// <summary>True nếu phòng đã có vé đặt → khóa chỉnh sửa sơ đồ ghế &amp; kích thước.</summary>
    public bool Locked { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "Vui lòng nhập tên phòng.", new[] { nameof(Name) });
        }

        if (!RoomStatus.AllowedValues.Contains(Status))
        {
            yield return new ValidationResult(
                "Trạng thái không hợp lệ.", new[] { nameof(Status) });
        }

        if (TotalRow < 1 || TotalRow > RoomPolicy.MaxRows)
        {
            yield return new ValidationResult(
                $"Số hàng phải từ 1 đến {RoomPolicy.MaxRows}.", new[] { nameof(TotalRow) });
        }

        if (TotalColumns < 1 || TotalColumns > RoomPolicy.MaxColumns)
        {
            yield return new ValidationResult(
                $"Số cột phải từ 1 đến {RoomPolicy.MaxColumns}.", new[] { nameof(TotalColumns) });
        }
    }
}

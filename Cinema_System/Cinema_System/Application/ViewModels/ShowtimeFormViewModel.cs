using System.ComponentModel.DataAnnotations;
using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

/// <summary>
/// ViewModel dùng chung cho form Tạo mới và Chỉnh sửa suất chiếu.
/// Implement IValidatableObject để kiểm tra nghiệp vụ phức tạp hơn DataAnnotations thông thường.
/// </summary>
public class ShowtimeFormViewModel : IValidatableObject
{
    // ID suất chiếu — Guid.Empty nghĩa là đang tạo mới, có giá trị nghĩa là đang chỉnh sửa
    public Guid Id { get; set; }

    [Display(Name = "Phim")]
    public Guid MovieId { get; set; }

    [Display(Name = "Phòng chiếu")]
    public Guid RoomId { get; set; }

    [Display(Name = "Thời gian bắt đầu")]
    public DateTime StartTime { get; set; } = DateTime.Now;

    [Display(Name = "Thời gian kết thúc")]
    public DateTime EndTime { get; set; } = DateTime.Now.AddHours(2);

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Scheduled";

    // Cờ đánh dấu suất chiếu đã có khách đặt vé chưa (Dùng để kiểm soát UI Edit/Cancel)
    public bool HasBookings { get; set; }

    // Số vé đã bán / tổng số ghế của phòng (hiển thị cho Manager biết mức độ lấp đầy)
    public int SeatsSold { get; set; }
    public int Capacity { get; set; }

    // Danh sách phim và phòng chiếu để populate dropdown trong form (không submit lên server)
    public IEnumerable<ItemOptionDTO> AvailableMovies { get; set; } = new List<ItemOptionDTO>();
    public IEnumerable<ItemOptionDTO> AvailableRooms { get; set; } = new List<ItemOptionDTO>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MovieId == Guid.Empty)
            yield return new ValidationResult("Vui lòng chọn phim.", new[] { nameof(MovieId) });

        if (RoomId == Guid.Empty)
            yield return new ValidationResult("Vui lòng chọn phòng chiếu.", new[] { nameof(RoomId) });

        // Chỉ chặn lịch trong quá khứ đối với thao tác Tạo mới (Id = rỗng). Khi Edit thì cho phép sửa lịch cũ.
        if (Id == Guid.Empty && StartTime < DateTime.Now)
            yield return new ValidationResult("Thời gian bắt đầu phải là hiện tại hoặc tương lai.", new[] { nameof(StartTime) });
    }
}

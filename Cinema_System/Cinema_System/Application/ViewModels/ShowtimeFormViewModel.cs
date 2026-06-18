using System.ComponentModel.DataAnnotations;
using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

public class ShowtimeFormViewModel : IValidatableObject
{
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

    public IEnumerable<ItemOptionDTO> AvailableMovies { get; set; } = new List<ItemOptionDTO>();
    public IEnumerable<ItemOptionDTO> AvailableRooms { get; set; } = new List<ItemOptionDTO>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MovieId == Guid.Empty)
            yield return new ValidationResult("Vui lòng chọn phim.", new[] { nameof(MovieId) });

        if (RoomId == Guid.Empty)
            yield return new ValidationResult("Vui lòng chọn phòng chiếu.", new[] { nameof(RoomId) });

        if (StartTime >= EndTime)
            yield return new ValidationResult("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.", new[] { nameof(EndTime) });

        if (StartTime < DateTime.Now)
            yield return new ValidationResult("Thời gian bắt đầu phải là hiện tại hoặc tương lai.", new[] { nameof(StartTime) });
    }
}

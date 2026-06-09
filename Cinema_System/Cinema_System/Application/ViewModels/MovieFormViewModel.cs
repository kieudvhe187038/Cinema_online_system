using System.ComponentModel.DataAnnotations;
using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

public class MovieFormViewModel : IValidatableObject
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên phim")]
    [StringLength(255, ErrorMessage = "Tên phim tối đa 255 ký tự")]
    [Display(Name = "Tên phim")]
    public string Title { get; set; } = null!;

    [Display(Name = "Slug (URL)")]
    [StringLength(255)]
    public string? Slug { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Trailer URL")]
    [StringLength(500)]
    public string? TrailerUrl { get; set; }

    [Display(Name = "Poster URL")]
    [StringLength(500)]
    public string? PosterUrl { get; set; }

    [Display(Name = "Banner URL")]
    [StringLength(500)]
    public string? BannerUrl { get; set; }

    [Display(Name = "Đạo diễn")]
    [StringLength(255)]
    public string? Director { get; set; }

    [Display(Name = "Diễn viên")]
    public string? CastMembers { get; set; }

    [Display(Name = "Ngôn ngữ")]
    [StringLength(100)]
    public string? Language { get; set; }

    [Display(Name = "Phụ đề")]
    [StringLength(100)]
    public string? Subtitle { get; set; }

    [Range(1, 600, ErrorMessage = "Thời lượng phải từ 1 đến 600 phút")]
    [Display(Name = "Thời lượng (phút)")]
    public int? DurationMinutes { get; set; }

    [Display(Name = "Ngày khởi chiếu")]
    public DateOnly? ReleaseDate { get; set; }

    [Display(Name = "Giới hạn độ tuổi")]
    public string? AgeRating { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Coming Soon";

    [Display(Name = "Thể loại")]
    public List<Guid> SelectedGenreIds { get; set; } = new();

    public List<GenreDTO> AvailableGenres { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ReleaseDate.HasValue && ReleaseDate.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            yield return new ValidationResult(
                "Ngày khởi chiếu phải là hôm nay hoặc trong tương lai.",
                new[] { nameof(ReleaseDate) });
        }
    }
}

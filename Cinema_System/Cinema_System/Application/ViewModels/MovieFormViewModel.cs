using System.ComponentModel.DataAnnotations;
using Cinema_System.Application.DTOs;
using Microsoft.AspNetCore.Http;

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
    [StringLength(3000, ErrorMessage = "Mô tả tối đa 3000 ký tự")]
    public string? Description { get; set; }

    // Trailer nhập trực tiếp bằng URL (vd link YouTube), không upload file.
    [Display(Name = "Trailer URL")]
    [StringLength(500)]
    [Url(ErrorMessage = "Trailer phải là một URL hợp lệ")]
    public string? TrailerUrl { get; set; }

    // Đường dẫn ảnh đã upload (lưu vào DB cột poster_url/banner_url).
    // Khi sửa: giữ lại giá trị cũ nếu không upload ảnh mới.
    [StringLength(500)]
    public string? PosterUrl { get; set; }

    [StringLength(500)]
    public string? BannerUrl { get; set; }

    // Ô upload ảnh từ form (controller lưu file rồi gán đường dẫn vào *Url ở trên).
    [Display(Name = "Poster (ảnh)")]
    public IFormFile? PosterFile { get; set; }

    [Display(Name = "Banner (ảnh)")]
    public IFormFile? BannerFile { get; set; }

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

    // Kiểm tra hợp lệ thêm: ngày khởi chiếu không được ở quá khứ.
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

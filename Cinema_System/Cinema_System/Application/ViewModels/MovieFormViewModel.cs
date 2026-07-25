using System.ComponentModel.DataAnnotations;
using Cinema_System.Application.Common;
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
    [StringLength(2000, ErrorMessage = "Diễn viên tối đa 2000 ký tự")]
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
    [StringLength(50, ErrorMessage = "Giới hạn độ tuổi tối đa 50 ký tự")]
    public string? AgeRating { get; set; }

    // Chỉ để HIỂN THỊ: trạng thái do hệ thống suy ra từ ngày khởi chiếu + lịch chiếu,
    // Manager không chọn tay (form sửa gửi lại qua hidden field để giữ đúng trạng thái khóa lịch).
    [Display(Name = "Trạng thái")]
    public string? Status { get; set; }

    // Suất chiếu sớm nhất (chưa hủy) của phim — form Sửa dùng để xem trước trạng thái khi đổi ngày
    // khởi chiếu (suất nằm trước ngày khởi chiếu => phim chiếu sớm). Chỉ hiển thị, không gửi lên server.
    public DateTime? FirstShowtimeStart { get; set; }

    [Display(Name = "Thể loại")]
    public List<Guid> SelectedGenreIds { get; set; } = new();

    public List<GenreDTO> AvailableGenres { get; set; } = new();

    // Kiểm tra hợp lệ thêm: ngày khởi chiếu (nguồn suy ra trạng thái phim) + giới hạn độ tuổi phải nằm
    // trong danh sách cho phép (chặn request giả mạo gửi giá trị ngoài dropdown, tránh lỗi truncation
    // ở cột age_rating VARCHAR(50)).
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ReleaseDate.HasValue)
        {
            yield return new ValidationResult(
                "Vui lòng chọn ngày khởi chiếu (trạng thái phim được xác định theo ngày này).",
                new[] { nameof(ReleaseDate) });
        }
        // Chỉ chặn ngày quá khứ khi THÊM phim mới; khi sửa vẫn cho giữ/chỉnh ngày đã qua.
        else if (Id == Guid.Empty && ReleaseDate.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            yield return new ValidationResult(
                "Ngày khởi chiếu phải là hôm nay hoặc trong tương lai.",
                new[] { nameof(ReleaseDate) });
        }

        if (!string.IsNullOrWhiteSpace(AgeRating) && !AgeRatingPolicy.DisplayOrder.Contains(AgeRating))
        {
            yield return new ValidationResult(
                "Giới hạn độ tuổi không hợp lệ.",
                new[] { nameof(AgeRating) });
        }
    }
}

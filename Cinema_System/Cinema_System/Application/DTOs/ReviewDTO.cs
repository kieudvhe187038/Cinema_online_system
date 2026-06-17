namespace Cinema_System.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public class ReviewDTO
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid MovieId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? UserName { get; set; }

    public string? MovieTitle { get; set; }
    public string? MovieSlug { get; set; }
}

public class CreateReviewDTO 
{
    public Guid MovieId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn số sao để đánh giá.")]
    [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao.")]
    public int Rating { get; set; }

    [StringLength(100, ErrorMessage = "Bình luận không được quá 100 ký tự.")]
    public string? Comment { get; set; }
}

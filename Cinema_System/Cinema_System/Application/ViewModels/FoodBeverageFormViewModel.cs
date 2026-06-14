using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Cinema_System.Application.ViewModels;

public class FoodBeverageFormViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên món")]
    [StringLength(255, ErrorMessage = "Tên món tối đa 255 ký tự")]
    [Display(Name = "Tên món")]
    public string Name { get; set; } = null!;

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    // Đường dẫn ảnh đã lưu (hiển thị ảnh hiện tại khi sửa, giữ lại nếu không upload mới)
    [StringLength(500)]
    public string? ImageUrl { get; set; }

    // File ảnh upload từ form (controller xử lý lưu rồi gán vào ImageUrl)
    [Display(Name = "Hình ảnh")]
    public IFormFile? ImageFile { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá")]
    [Range(0, 10_000_000, ErrorMessage = "Giá phải từ 0 đến 10,000,000")]
    [Display(Name = "Giá (VNĐ)")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái kho")]
    [Display(Name = "Tình trạng kho")]
    public string StockStatus { get; set; } = "In Stock";
}

using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

public class PromotionService : IPromotionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PromotionService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    // Lấy danh sách voucher: lọc theo mã/trạng thái + phân trang, kèm cờ đã được dùng.
    public async Task<PromotionListViewModel> GetAllAsync(
        string? search, string? status, int page, int pageSize)
    {
        var code = search?.Trim();
        var items = await _unitOfWork.Promotions.GetAllAsync(
            predicate: p =>
                (string.IsNullOrEmpty(code) || p.Code.Contains(code)) &&
                (string.IsNullOrEmpty(status) || p.Status == status),
            orderBy: q => q.OrderByDescending(p => p.ValidFrom));

        var list = items.ToList();
        var total = list.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        page = Math.Clamp(page, 1, totalPages);

        var paged = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var dtos = new List<PromotionDTO>();
        foreach (var p in paged)
        {
            var dto = _mapper.Map<PromotionDTO>(p);
            dto.HasUsage = await _unitOfWork.Bookings.ExistsAsync(b => b.PromotionId == p.Id);
            dtos.Add(dto);
        }

        return new PromotionListViewModel
        {
            Items = dtos,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            TotalCount = total,
            Search = search,
            StatusFilter = status
        };
    }

    // Lấy dữ liệu 1 voucher để đổ vào form sửa.
    public async Task<PromotionFormViewModel?> GetForEditAsync(Guid id)
    {
        var item = await _unitOfWork.Promotions.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<PromotionFormViewModel>(item);
    }

    // Tạo voucher mới (chuẩn hóa mã viết hoa + kiểm tra trùng mã).
    public async Task<Result> CreateAsync(PromotionFormViewModel model)
    {
        var code = NormalizeCode(model.Code);
        if (await _unitOfWork.Promotions.ExistsAsync(p => p.Code == code))
            return Result.Failure("Mã voucher đã tồn tại.");

        var item = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = code
        };
        ApplyForm(item, model);

        await _unitOfWork.Promotions.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    // Cập nhật voucher (kiểm tra trùng mã với voucher khác).
    public async Task<Result> UpdateAsync(PromotionFormViewModel model)
    {
        var item = await _unitOfWork.Promotions.GetByIdAsync(model.Id);
        if (item is null)
            return Result.Failure("Không tìm thấy voucher.");

        var code = NormalizeCode(model.Code);
        if (await _unitOfWork.Promotions.ExistsAsync(p => p.Code == code && p.Id != model.Id))
            return Result.Failure("Mã voucher đã tồn tại.");

        item.Code = code;
        ApplyForm(item, model);

        _unitOfWork.Promotions.Update(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    // Bật/tắt voucher: đổi qua lại giữa "Active" và "Disabled".
    public async Task<Result> ToggleStatusAsync(Guid id)
    {
        var item = await _unitOfWork.Promotions.GetByIdAsync(id);
        if (item is null)
            return Result.Failure("Không tìm thấy voucher.");

        item.Status = item.Status == PromotionStatus.Disabled
            ? PromotionStatus.Active
            : PromotionStatus.Disabled;

        _unitOfWork.Promotions.Update(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    // Xóa voucher (chặn xóa nếu đã được áp dụng vào đơn đặt vé).
    public async Task<Result> DeleteAsync(Guid id)
    {
        var item = await _unitOfWork.Promotions.GetByIdAsync(id);
        if (item is null)
            return Result.Failure("Không tìm thấy voucher.");

        var used = await _unitOfWork.Bookings.ExistsAsync(b => b.PromotionId == id);
        if (used)
            return Result.Failure("Không thể xóa: voucher đã được áp dụng vào đơn đặt vé. Hãy dùng chức năng Tắt.");

        _unitOfWork.Promotions.Remove(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    // Đổ dữ liệu form vào entity (dùng chung cho Create/Update).
    // Mức giảm tối đa chỉ có ý nghĩa khi giảm theo %, loại "Fixed Amount" sẽ bỏ trống.
    private static void ApplyForm(Promotion item, PromotionFormViewModel model)
    {
        item.DiscountType = model.DiscountType;
        item.DiscountAmount = model.DiscountAmount;
        item.MinOrderValue = model.MinOrderValue ?? 0;
        item.MaxDiscountAmount = model.DiscountType == PromotionDiscountType.Percent
            ? model.MaxDiscountAmount
            : null;
        item.ApplicableTarget = model.ApplicableTarget;
        item.ValidFrom = model.ValidFrom;
        item.ValidTo = model.ValidTo;
        item.UsageLimit = model.UsageLimit;
        item.Status = model.Status;
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}

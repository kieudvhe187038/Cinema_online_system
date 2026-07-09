using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

public class VatService : IVatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public VatService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<VatDTO>> GetAllAsync()
    {
        var items = await _unitOfWork.Vats.GetAllAsync(
            orderBy: q => q.OrderByDescending(v => v.CreatedAt));

        var dtos = new List<VatDTO>();
        foreach (var v in items)
        {
            var dto = _mapper.Map<VatDTO>(v);
            dto.HasUsage = await _unitOfWork.Bookings.ExistsAsync(b => b.VatId == v.Id);
            dtos.Add(dto);
        }

        return dtos;
    }

    public async Task<VatFormViewModel?> GetForEditAsync(Guid id)
    {
        var item = await _unitOfWork.Vats.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<VatFormViewModel>(item);
    }

    public async Task<Result> CreateAsync(VatFormViewModel model)
    {
        var item = new Vat
        {
            Id = Guid.NewGuid(),
            VatRate = model.VatRate,
            Description = model.Description?.Trim(),
            Status = model.Status,
            CreatedAt = DateTime.Now
        };

        if (item.Status == VatStatus.Active)
            await DeactivateOthersAsync(item.Id);

        await _unitOfWork.Vats.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateAsync(VatFormViewModel model)
    {
        var item = await _unitOfWork.Vats.GetByIdAsync(model.Id);
        if (item is null)
            return Result.Failure("Không tìm thấy cấu hình VAT.");

        item.VatRate = model.VatRate;
        item.Description = model.Description?.Trim();
        item.Status = model.Status;

        if (item.Status == VatStatus.Active)
            await DeactivateOthersAsync(item.Id);

        _unitOfWork.Vats.Update(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    // Bật/tắt: bật thì đảm bảo đây là VAT Active duy nhất (các luồng đặt vé chỉ lấy 1 VAT Active).
    public async Task<Result> ToggleStatusAsync(Guid id)
    {
        var item = await _unitOfWork.Vats.GetByIdAsync(id);
        if (item is null)
            return Result.Failure("Không tìm thấy cấu hình VAT.");

        if (item.Status == VatStatus.Active)
        {
            item.Status = VatStatus.Inactive;
        }
        else
        {
            item.Status = VatStatus.Active;
            await DeactivateOthersAsync(item.Id);
        }

        _unitOfWork.Vats.Update(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var item = await _unitOfWork.Vats.GetByIdAsync(id);
        if (item is null)
            return Result.Failure("Không tìm thấy cấu hình VAT.");

        var used = await _unitOfWork.Bookings.ExistsAsync(b => b.VatId == id);
        if (used)
            return Result.Failure("Không thể xóa: VAT này đã được áp dụng vào đơn đặt vé. Hãy dùng chức năng Tắt.");

        _unitOfWork.Vats.Remove(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    // Chỉ cho phép 1 cấu hình VAT ở trạng thái Active tại một thời điểm, vì luồng đặt vé
    // (ShowtimeService/CounterBookingService) luôn lấy VAT Active đầu tiên tìm thấy.
    private async Task DeactivateOthersAsync(Guid keepId)
    {
        var others = await _unitOfWork.Vats.GetAllAsync(
            predicate: v => v.Id != keepId && v.Status == VatStatus.Active);

        foreach (var other in others)
        {
            other.Status = VatStatus.Inactive;
            _unitOfWork.Vats.Update(other);
        }
    }
}

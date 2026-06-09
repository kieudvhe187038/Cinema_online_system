using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

public class SeatTypeService : ISeatTypeService
{
    private readonly IUnitOfWork _unitOfWork;

    public SeatTypeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SeatTypeDTO>> GetAllAsync()
    {
        var seatTypes = await _unitOfWork.SeatTypes.GetAllAsync(
            orderBy: q => q.OrderBy(t => t.Name));

        var result = new List<SeatTypeDTO>();
        foreach (var t in seatTypes)
        {
            var seatCount = await _unitOfWork.Seats.CountAsync(s => s.SeatTypeId == t.Id);
            var usedInPricing = await _unitOfWork.PriceSeatConfigs.ExistsAsync(p => p.SeatTypeId == t.Id);

            result.Add(new SeatTypeDTO
            {
                Id = t.Id,
                Name = t.Name,
                Capacity = t.Capacity,
                ColumnSpan = t.ColumnSpan,
                SeatCount = seatCount,
                InUse = seatCount > 0 || usedInPricing
            });
        }

        return result;
    }

    public async Task<SeatTypeFormViewModel?> GetForEditAsync(Guid id)
    {
        var seatType = await _unitOfWork.SeatTypes.GetByIdAsync(id);
        if (seatType is null) return null;

        return new SeatTypeFormViewModel
        {
            Id = seatType.Id,
            Name = seatType.Name,
            Capacity = seatType.Capacity,
            ColumnSpan = seatType.ColumnSpan
        };
    }

    public async Task<Result> CreateAsync(SeatTypeFormViewModel model)
    {
        var nameTaken = await _unitOfWork.SeatTypes.ExistsAsync(t => t.Name == model.Name);
        if (nameTaken)
            return Result.Failure("Tên loại ghế đã tồn tại.");

        var seatType = new SeatType
        {
            Name = model.Name.Trim(),
            Capacity = model.Capacity,
            ColumnSpan = model.ColumnSpan
        };

        await _unitOfWork.SeatTypes.AddAsync(seatType);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> UpdateAsync(SeatTypeFormViewModel model)
    {
        var seatType = await _unitOfWork.SeatTypes.GetByIdAsync(model.Id);
        if (seatType is null)
            return Result.Failure("Không tìm thấy loại ghế.");

        var nameTaken = await _unitOfWork.SeatTypes.ExistsAsync(
            t => t.Name == model.Name && t.Id != model.Id);
        if (nameTaken)
            return Result.Failure("Tên loại ghế đã tồn tại.");

        seatType.Name = model.Name.Trim();
        seatType.Capacity = model.Capacity;
        seatType.ColumnSpan = model.ColumnSpan;

        _unitOfWork.SeatTypes.Update(seatType);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var seatType = await _unitOfWork.SeatTypes.GetByIdAsync(id);
        if (seatType is null)
            return Result.Failure("Không tìm thấy loại ghế.");

        var usedBySeats = await _unitOfWork.Seats.ExistsAsync(s => s.SeatTypeId == id);
        var usedInPricing = await _unitOfWork.PriceSeatConfigs.ExistsAsync(p => p.SeatTypeId == id);
        if (usedBySeats || usedInPricing)
            return Result.Failure("Không thể xóa: loại ghế đang được sử dụng bởi ghế hoặc cấu hình giá.");

        _unitOfWork.SeatTypes.Remove(seatType);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }
}

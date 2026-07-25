using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

public class SeatTypeService : ISeatTypeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SeatTypeService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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

            var dto = _mapper.Map<SeatTypeDTO>(t);
            dto.SeatCount = seatCount;
            dto.InUse = seatCount > 0 || usedInPricing;
            result.Add(dto);
        }

        return result;
    }

    public async Task<SeatTypeFormViewModel?> GetForEditAsync(Guid id)
    {
        var seatType = await _unitOfWork.SeatTypes.GetByIdAsync(id);
        if (seatType is null) return null;

        var model = _mapper.Map<SeatTypeFormViewModel>(seatType);
        model.InUse = await _unitOfWork.Seats.ExistsAsync(s => s.SeatTypeId == id);
        return model;
    }

    public async Task<Result> CreateAsync(SeatTypeFormViewModel model)
    {
        var name = model.Name.Trim();
        var nameTaken = await _unitOfWork.SeatTypes.ExistsAsync(t => t.Name == name);
        if (nameTaken)
            return Result.Failure("Tên loại ghế đã tồn tại.");

        var seatType = new SeatType
        {
            Name = name,
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

        var name = model.Name.Trim();
        var nameTaken = await _unitOfWork.SeatTypes.ExistsAsync(
            t => t.Name == name && t.Id != model.Id);
        if (nameTaken)
            return Result.Failure("Tên loại ghế đã tồn tại.");

        // Đổi "Số ô chiếm" của loại ghế đã dùng trong sơ đồ ghế sẽ làm lệch hình học lưới
        // (ghế rơi ra ngoài biên phòng hoặc chồng lấn ghế bên cạnh) vì span được suy ra
        // từ ColumnSpan hiện tại, không lưu cố định theo từng ghế lúc đặt.
        if (model.ColumnSpan != seatType.ColumnSpan
            && await _unitOfWork.Seats.ExistsAsync(s => s.SeatTypeId == model.Id))
        {
            return Result.Failure(
                "Không thể đổi số ô chiếm: loại ghế đã được dùng trong sơ đồ ghế của phòng. " +
                "Hãy tạo loại ghế mới nếu cần số ô chiếm khác.");
        }

        seatType.Name = name;
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

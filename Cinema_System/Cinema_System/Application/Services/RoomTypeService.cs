using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

public class RoomTypeService : IRoomTypeService
{
    private readonly IUnitOfWork _unitOfWork;

    public RoomTypeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<RoomTypeDTO>> GetAllAsync()
    {
        var roomTypes = await _unitOfWork.RoomTypes.GetAllAsync(
            orderBy: q => q.OrderBy(t => t.Name));

        var result = new List<RoomTypeDTO>();
        foreach (var t in roomTypes)
        {
            var roomCount = await _unitOfWork.Rooms.CountAsync(r => r.RoomTypeId == t.Id);
            var usedInPricing = await _unitOfWork.PriceRoomTypeConfigs.ExistsAsync(p => p.RoomTypeId == t.Id);

            result.Add(new RoomTypeDTO
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                RoomCount = roomCount,
                InUse = roomCount > 0 || usedInPricing
            });
        }

        return result;
    }

    public async Task<RoomTypeFormViewModel?> GetForEditAsync(Guid id)
    {
        var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(id);
        if (roomType is null) return null;

        return new RoomTypeFormViewModel
        {
            Id = roomType.Id,
            Name = roomType.Name,
            Description = roomType.Description
        };
    }

    public async Task<Result> CreateAsync(RoomTypeFormViewModel model)
    {
        var nameTaken = await _unitOfWork.RoomTypes.ExistsAsync(t => t.Name == model.Name);
        if (nameTaken)
            return Result.Failure("Tên loại phòng đã tồn tại.");

        var roomType = new RoomType
        {
            Name = model.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim()
        };

        await _unitOfWork.RoomTypes.AddAsync(roomType);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> UpdateAsync(RoomTypeFormViewModel model)
    {
        var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(model.Id);
        if (roomType is null)
            return Result.Failure("Không tìm thấy loại phòng.");

        var nameTaken = await _unitOfWork.RoomTypes.ExistsAsync(
            t => t.Name == model.Name && t.Id != model.Id);
        if (nameTaken)
            return Result.Failure("Tên loại phòng đã tồn tại.");

        roomType.Name = model.Name.Trim();
        roomType.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

        _unitOfWork.RoomTypes.Update(roomType);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(id);
        if (roomType is null)
            return Result.Failure("Không tìm thấy loại phòng.");

        var usedByRooms = await _unitOfWork.Rooms.ExistsAsync(r => r.RoomTypeId == id);
        var usedInPricing = await _unitOfWork.PriceRoomTypeConfigs.ExistsAsync(p => p.RoomTypeId == id);
        if (usedByRooms || usedInPricing)
            return Result.Failure("Không thể xóa: loại phòng đang được sử dụng bởi phòng hoặc cấu hình giá.");

        _unitOfWork.RoomTypes.Remove(roomType);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }
}

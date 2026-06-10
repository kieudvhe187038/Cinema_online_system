using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

public class FoodBeverageService : IFoodBeverageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public FoodBeverageService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<FoodBeverageListViewModel> GetAllAsync(
        string? search, string? status, int page, int pageSize)
    {
        var items = await _unitOfWork.FoodBeverages.GetAllAsync(
            predicate: f =>
                (string.IsNullOrEmpty(search) || f.Name.Contains(search)) &&
                (string.IsNullOrEmpty(status) || f.StockStatus == status),
            orderBy: q => q.OrderBy(f => f.Name));

        var list = items.ToList();
        var total = list.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        page = Math.Clamp(page, 1, totalPages);

        var paged = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var dtos = new List<FoodBeverageDTO>();
        foreach (var f in paged)
        {
            var dto = _mapper.Map<FoodBeverageDTO>(f);
            dto.HasOrders = await _unitOfWork.BookingFoods.ExistsAsync(b => b.FbId == f.Id);
            dtos.Add(dto);
        }

        return new FoodBeverageListViewModel
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

    public async Task<FoodBeverageFormViewModel?> GetForEditAsync(Guid id)
    {
        var item = await _unitOfWork.FoodBeverages.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<FoodBeverageFormViewModel>(item);
    }

    public async Task<Result> CreateAsync(FoodBeverageFormViewModel model)
    {
        var nameTaken = await _unitOfWork.FoodBeverages.ExistsAsync(
            f => f.Name == model.Name.Trim());
        if (nameTaken)
            return Result.Failure("Tên món đã tồn tại trong menu.");

        var item = new FoodBeverage
        {
            Id = Guid.NewGuid(),
            Name = model.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim(),
            Price = model.Price,
            StockStatus = model.StockStatus
        };

        await _unitOfWork.FoodBeverages.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateAsync(FoodBeverageFormViewModel model)
    {
        var item = await _unitOfWork.FoodBeverages.GetByIdAsync(model.Id);
        if (item is null)
            return Result.Failure("Không tìm thấy món ăn/thức uống.");

        var nameTaken = await _unitOfWork.FoodBeverages.ExistsAsync(
            f => f.Name == model.Name.Trim() && f.Id != model.Id);
        if (nameTaken)
            return Result.Failure("Tên món đã tồn tại trong menu.");

        item.Name = model.Name.Trim();
        item.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        item.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim();
        item.Price = model.Price;
        item.StockStatus = model.StockStatus;

        _unitOfWork.FoodBeverages.Update(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ToggleVisibilityAsync(Guid id)
    {
        var item = await _unitOfWork.FoodBeverages.GetByIdAsync(id);
        if (item is null)
            return Result.Failure("Không tìm thấy món ăn/thức uống.");

        item.StockStatus = item.StockStatus == "Discontinued" ? "In Stock" : "Discontinued";

        _unitOfWork.FoodBeverages.Update(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var item = await _unitOfWork.FoodBeverages.GetByIdAsync(id);
        if (item is null)
            return Result.Failure("Không tìm thấy món ăn/thức uống.");

        var hasOrders = await _unitOfWork.BookingFoods.ExistsAsync(b => b.FbId == id);
        if (hasOrders)
            return Result.Failure("Không thể xóa: món này đã có trong lịch sử đặt hàng. Hãy dùng chức năng Ẩn.");

        _unitOfWork.FoodBeverages.Remove(item);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }
}

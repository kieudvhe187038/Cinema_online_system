using Cinema_System.Application.Common;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

/// <summary>Quản lý thể loại phim (Genres) cho khu Manager: CRUD + chặn xóa khi đang có phim dùng.</summary>
public class GenreService : IGenreService
{
    private readonly IUnitOfWork _unitOfWork;

    public GenreService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Danh sách thể loại: lọc theo tên (search) rồi phân trang; chỉ đếm số phim cho các dòng của TRANG hiện tại.
    public async Task<PagedResult<GenreListItemViewModel>> GetPagedAsync(string? search, int page, int pageSize)
    {
        var keyword = search?.Trim();
        var genres = (await _unitOfWork.Genres.GetAllAsync(
            predicate: string.IsNullOrEmpty(keyword) ? null : g => g.Name.Contains(keyword),
            orderBy: q => q.OrderBy(g => g.Name))).ToList();

        var total = genres.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        page = Math.Clamp(page, 1, totalPages);

        var items = new List<GenreListItemViewModel>();
        foreach (var g in genres.Skip((page - 1) * pageSize).Take(pageSize))
        {
            // Genre <-> Movie là quan hệ nhiều-nhiều: đếm số phim có gắn thể loại này.
            var movieCount = await _unitOfWork.Movies.CountAsync(m => m.Genres.Any(x => x.Id == g.Id));
            items.Add(new GenreListItemViewModel { Id = g.Id, Name = g.Name, MovieCount = movieCount });
        }

        return new PagedResult<GenreListItemViewModel>
        {
            Items = items,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<GenreFormViewModel?> GetForEditAsync(Guid id)
    {
        var genre = await _unitOfWork.Genres.GetByIdAsync(id);
        if (genre is null) return null;

        return new GenreFormViewModel
        {
            Id = genre.Id,
            Name = genre.Name,
            InUse = await _unitOfWork.Movies.ExistsAsync(m => m.Genres.Any(x => x.Id == id))
        };
    }

    public async Task<Result> CreateAsync(GenreFormViewModel model)
    {
        var name = model.Name.Trim();

        var nameTaken = await _unitOfWork.Genres.ExistsAsync(g => g.Name == name);
        if (nameTaken)
            return Result.Failure("Tên thể loại đã tồn tại.");

        await _unitOfWork.Genres.AddAsync(new Genre
        {
            Id = Guid.NewGuid(),
            Name = name
        });
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> UpdateAsync(GenreFormViewModel model)
    {
        var genre = await _unitOfWork.Genres.GetByIdAsync(model.Id);
        if (genre is null)
            return Result.Failure("Không tìm thấy thể loại.");

        var name = model.Name.Trim();

        var nameTaken = await _unitOfWork.Genres.ExistsAsync(
            g => g.Name == name && g.Id != model.Id);
        if (nameTaken)
            return Result.Failure("Tên thể loại đã tồn tại.");

        genre.Name = name;
        _unitOfWork.Genres.Update(genre);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var genre = await _unitOfWork.Genres.GetByIdAsync(id);
        if (genre is null)
            return Result.Failure("Không tìm thấy thể loại.");

        var inUse = await _unitOfWork.Movies.ExistsAsync(m => m.Genres.Any(x => x.Id == id));
        if (inUse)
            return Result.Failure("Không thể xóa: thể loại đang được gán cho ít nhất một phim.");

        _unitOfWork.Genres.Remove(genre);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }
}

using System.Linq.Expressions;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Application.Services;

public class UserService : IUserService
{
    private const string StatusActive = "Active";
    private const string StatusInactive = "Locked";

    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserListViewModel> GetUsersAsync(
        string? search, Guid? roleId, string? status, int page, int pageSize)
    {
        Expression<Func<User, bool>> predicate = u =>
            (search == null || u.FullName.Contains(search) || u.Email.Contains(search)
                || (u.Phone != null && u.Phone.Contains(search)))
            && (roleId == null || u.RoleId == roleId)
            && (status == null || u.Status == status);

        var users = (await _unitOfWork.Users.GetAllAsync(
            predicate,
            include: q => q.Include(u => u.Role),
            orderBy: q => q.OrderByDescending(u => u.CreatedAt))).ToList();

        var totalCount = users.Count;
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var items = users
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToDTO)
            .ToList();

        return new UserListViewModel
        {
            Users = items,
            Roles = await GetRolesAsync(),
            Search = search,
            RoleId = roleId,
            Status = status,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<UserDTO?> GetUserByIdAsync(Guid id)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Id == id,
            include: q => q.Include(u => u.Role));

        return user is null ? null : MapToDTO(user);
    }

    public async Task<UserEditViewModel?> GetUserForEditAsync(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null) return null;

        return new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            DateOfBirth = user.DateOfBirth,
            RoleId = user.RoleId,
            Roles = await GetRolesAsync()
        };
    }

    public async Task<IEnumerable<RoleDTO>> GetRolesAsync()
    {
        var roles = await _unitOfWork.Roles.GetAllAsync(
            orderBy: q => q.OrderBy(r => r.Name));

        return roles.Select(r => new RoleDTO
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description
        }).ToList();
    }

    public async Task<Result> UpdateUserAsync(UserEditViewModel model)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(model.Id);
        if (user is null)
            return Result.Failure("Không tìm thấy người dùng.");

        var emailTaken = await _unitOfWork.Users.ExistsAsync(
            u => u.Email == model.Email && u.Id != model.Id);
        if (emailTaken)
            return Result.Failure("Email đã được sử dụng bởi tài khoản khác.");

        if (!string.IsNullOrWhiteSpace(model.Phone))
        {
            var phoneTaken = await _unitOfWork.Users.ExistsAsync(
                u => u.Phone == model.Phone && u.Id != model.Id);
            if (phoneTaken)
                return Result.Failure("Số điện thoại đã được sử dụng bởi tài khoản khác.");
        }

        var roleExists = await _unitOfWork.Roles.ExistsAsync(r => r.Id == model.RoleId);
        if (!roleExists)
            return Result.Failure("Vai trò không hợp lệ.");

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.Phone = model.Phone;
        user.DateOfBirth = model.DateOfBirth;
        user.RoleId = model.RoleId;
        user.UpdatedAt = DateTime.Now;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> SetStatusAsync(Guid id, bool active)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return Result.Failure("Không tìm thấy người dùng.");

        user.Status = active ? StatusActive : StatusInactive;
        user.UpdatedAt = DateTime.Now;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> AssignRoleAsync(Guid id, Guid roleId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return Result.Failure("Không tìm thấy người dùng.");

        var roleExists = await _unitOfWork.Roles.ExistsAsync(r => r.Id == roleId);
        if (!roleExists)
            return Result.Failure("Vai trò không hợp lệ.");

        user.RoleId = roleId;
        user.UpdatedAt = DateTime.Now;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<string>> ResetPasswordAsync(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return Result<string>.Failure("Không tìm thấy người dùng.");

        var tempPassword = GenerateTempPassword();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
        user.UpdatedAt = DateTime.Now;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Result<string>.Success(tempPassword);
    }

    private static string GenerateTempPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "@#$%&*";
        const string all = upper + lower + digits + special;

        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(12);
        var chars = new char[12];

        chars[0] = upper[bytes[0] % upper.Length];
        chars[1] = lower[bytes[1] % lower.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        chars[3] = special[bytes[3] % special.Length];
        for (int i = 4; i < chars.Length; i++)
            chars[i] = all[bytes[i] % all.Length];

        return new string(chars);
    }

    private static UserDTO MapToDTO(User u)
    {
        return new UserDTO
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.Phone,
            DateOfBirth = u.DateOfBirth,
            RewardPoints = u.RewardPoints,
            Status = u.Status,
            RoleId = u.RoleId,
            RoleName = u.Role?.Name ?? string.Empty,
            CreatedAt = u.CreatedAt
        };
    }
}

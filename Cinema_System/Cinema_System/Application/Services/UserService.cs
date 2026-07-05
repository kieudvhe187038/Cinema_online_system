using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Services;

public class UserService : IUserService
{
    private const string StatusActive = "Active";
    private const string StatusInactive = "Locked";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserListViewModel> GetUsersAsync(
        string? search, Guid? roleId, string? status, int page, int pageSize)
    {
        // Lọc + phân trang tại SQL (Skip/Take) — chỉ tải đúng 1 trang, tránh kéo cả bảng.
        var (rows, totalCount) = await _unitOfWork.Users.GetPagedWithRoleAsync(
            search, roleId, status, page, pageSize);

        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var items = _mapper.Map<List<UserDTO>>(rows);

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
        var user = await _unitOfWork.Users.GetByIdWithRoleAsync(id);

        return user is null ? null : _mapper.Map<UserDTO>(user);
    }

    public async Task<UserEditViewModel?> GetUserForEditAsync(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null) return null;

        var vm = _mapper.Map<UserEditViewModel>(user);
        vm.Roles = await GetRolesAsync();
        return vm;
    }

    public async Task<IEnumerable<RoleDTO>> GetRolesAsync()
    {
        var roles = await _unitOfWork.Roles.GetAllAsync(
            orderBy: q => q.OrderBy(r => r.Name));

        return _mapper.Map<List<RoleDTO>>(roles);
    }

    public async Task<Result> UpdateUserAsync(UserEditViewModel model)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(model.Id);
        if (user is null)
            return Result.Failure("Không tìm thấy người dùng.");

        var fullName = model.FullName.Trim();
        var email = model.Email.Trim();
        var phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();

        var emailTaken = await _unitOfWork.Users.ExistsAsync(
            u => u.Email == email && u.Id != model.Id);
        if (emailTaken)
            return Result.Failure("Email đã được sử dụng bởi tài khoản khác.");

        if (phone is not null)
        {
            var phoneTaken = await _unitOfWork.Users.ExistsAsync(
                u => u.Phone == phone && u.Id != model.Id);
            if (phoneTaken)
                return Result.Failure("Số điện thoại đã được sử dụng bởi tài khoản khác.");
        }

        var roleExists = await _unitOfWork.Roles.ExistsAsync(r => r.Id == model.RoleId);
        if (!roleExists)
            return Result.Failure("Vai trò không hợp lệ.");

        user.FullName = fullName;
        user.Email = email;
        user.Phone = phone;
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
}

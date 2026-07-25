using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface IUserService
{
    Task<UserListViewModel> GetUsersAsync(
        string? search, Guid? roleId, string? status, int page, int pageSize);

    Task<UserDTO?> GetUserByIdAsync(Guid id);

    Task<UserEditViewModel?> GetUserForEditAsync(Guid id);

    Task<IEnumerable<RoleDTO>> GetRolesAsync();

    Task<Result> UpdateUserAsync(UserEditViewModel model);

    Task<Result> SetStatusAsync(Guid id, bool active);

    Task<Result> AssignRoleAsync(Guid id, Guid roleId);

    Task<Result<string>> ResetPasswordAsync(Guid id);
}

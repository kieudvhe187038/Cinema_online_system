using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

public class UserListViewModel
{
    public IEnumerable<UserDTO> Users { get; set; } = new List<UserDTO>();

    public IEnumerable<RoleDTO> Roles { get; set; } = new List<RoleDTO>();

    public string? Search { get; set; }

    public Guid? RoleId { get; set; }

    public string? Status { get; set; }

    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }
}

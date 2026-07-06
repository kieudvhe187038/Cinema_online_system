using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface IAdminDashboardService
{
    Task<AdminDashboardViewModel> GetDashboardAsync();
}

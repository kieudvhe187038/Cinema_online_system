using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

// Dịch vụ tổng hợp số liệu cho trang Tổng quan quản lý.
public interface IDashboardService
{
    // Lấy các chỉ số KPI + danh sách đơn đặt vé gần đây.
    Task<DashboardViewModel> GetDashboardAsync();
}

using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

// Dịch vụ báo cáo & thống kê kinh doanh cho quản lý.
public interface IReportService
{
    // Tổng hợp báo cáo doanh thu / vé bán / phương thức thanh toán trong khoảng ngày.
    Task<ReportViewModel> GetReportAsync(DateOnly? from, DateOnly? to);
}

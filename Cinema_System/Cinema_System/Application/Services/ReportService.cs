using ClosedXML.Excel;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Application.Services;

// Báo cáo & thống kê kinh doanh cho quản lý (doanh thu, vé bán, phương thức thanh toán).
public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReportViewModel> GetReportAsync(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dateFrom = from ?? today.AddDays(-29);   // mặc định 30 ngày gần nhất
        var dateTo = to ?? today;
        if (dateTo < dateFrom) dateTo = dateFrom;     // chống nhập ngược

        var fromDt = dateFrom.ToDateTime(TimeOnly.MinValue);
        var toEndDt = dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue);   // hết ngày dateTo

        // Đơn đã thanh toán trong khoảng, kèm phim + vé (split query tránh nhân dòng).
        var bookings = (await _unitOfWork.Bookings.GetAllAsync(
            predicate: b => b.PaymentStatus == "Paid" && b.CreatedAt >= fromDt && b.CreatedAt < toEndDt,
            include: q => q
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Tickets)
                .AsSplitQuery())).ToList();

        // ── KPI ──
        var totalRevenue = bookings.Sum(b => b.FinalAmount);
        var totalBookings = bookings.Count;
        var totalTickets = bookings.Sum(b => b.Tickets.Count(t => t.Status != "Cancelled"));
        var avgOrder = totalBookings > 0 ? Math.Round(totalRevenue / totalBookings, 0) : 0m;

        // ── Doanh thu theo ngày (điền đủ mọi ngày trong khoảng, kể cả ngày = 0) ──
        var revByDay = bookings
            .Where(b => b.CreatedAt.HasValue)
            .GroupBy(b => DateOnly.FromDateTime(b.CreatedAt!.Value))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.FinalAmount));
        var daily = new List<DailyRevenuePoint>();
        for (var d = dateFrom; d <= dateTo; d = d.AddDays(1))
            daily.Add(new DailyRevenuePoint { Date = d, Revenue = revByDay.TryGetValue(d, out var r) ? r : 0m });

        // ── Top phim theo doanh thu vé (dựa trên giá vé lúc đặt, bỏ vé đã hủy) ──
        var topMovies = bookings
            .SelectMany(b => b.Tickets
                .Where(t => t.Status != "Cancelled")
                .Select(t => new { Title = b.Showtime.Movie.Title, t.PriceAtBooking }))
            .GroupBy(x => x.Title)
            .Select(g => new TopMovieItem
            {
                Title = g.Key,
                TicketsSold = g.Count(),
                Revenue = g.Sum(x => x.PriceAtBooking)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToList();

        // ── Tỉ lệ phương thức thanh toán (từ các payment thành công trong khoảng) ──
        var payments = await _unitOfWork.Payments.GetAllAsync(
            predicate: p => p.Status == "Success" && p.PaidAt >= fromDt && p.PaidAt < toEndDt);
        var payMethods = payments
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new PaymentMethodItem
            {
                Method = g.Key,
                Count = g.Count(),
                Amount = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new ReportViewModel
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            TotalRevenue = totalRevenue,
            TotalBookings = totalBookings,
            TotalTickets = totalTickets,
            AvgOrderValue = avgOrder,
            DailyRevenue = daily,
            TopMovies = topMovies,
            PaymentMethods = payMethods
        };
    }

    // Xuất báo cáo ra 1 sheet Excel gồm: khoảng thời gian, KPI, doanh thu theo ngày, top phim, phương thức thanh toán.
    public async Task<(byte[] Content, string FileName)> ExportExcelAsync(DateOnly? from, DateOnly? to)
    {
        var vm = await GetReportAsync(from, to);

        const string moneyFmt = "#,##0 \"₫\"";
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Báo cáo");
        var row = 1;

        // Tiêu đề + khoảng thời gian.
        ws.Cell(row, 1).Value = "BÁO CÁO & THỐNG KÊ";
        ws.Range(row, 1, row, 4).Merge();
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 15;
        row++;
        ws.Cell(row, 1).Value = $"Khoảng thời gian: {vm.DateFrom:dd/MM/yyyy} - {vm.DateTo:dd/MM/yyyy}";
        ws.Range(row, 1, row, 4).Merge();
        ws.Cell(row, 1).Style.Font.Italic = true;
        row += 2;

        // ── Chỉ số tổng quan ──
        row = WriteSectionTitle(ws, row, "Chỉ số tổng quan");
        WriteKpi(ws, row++, "Tổng doanh thu", vm.TotalRevenue, moneyFmt);
        WriteKpi(ws, row++, "Số đơn", vm.TotalBookings, "#,##0");
        WriteKpi(ws, row++, "Vé bán", vm.TotalTickets, "#,##0");
        WriteKpi(ws, row++, "Trung bình / đơn", vm.AvgOrderValue, moneyFmt);
        row++;

        // ── Doanh thu theo ngày ──
        row = WriteSectionTitle(ws, row, "Doanh thu theo ngày");
        row = WriteHeader(ws, row, "Ngày", "Doanh thu");
        foreach (var d in vm.DailyRevenue)
        {
            ws.Cell(row, 1).Value = d.Date.ToString("dd/MM/yyyy");
            ws.Cell(row, 2).Value = d.Revenue;
            ws.Cell(row, 2).Style.NumberFormat.Format = moneyFmt;
            row++;
        }
        row++;

        // ── Top 5 phim theo doanh thu vé ──
        row = WriteSectionTitle(ws, row, "Top 5 phim theo doanh thu vé");
        row = WriteHeader(ws, row, "Hạng", "Phim", "Số vé", "Doanh thu");
        var rank = 1;
        foreach (var m in vm.TopMovies)
        {
            ws.Cell(row, 1).Value = rank++;
            ws.Cell(row, 2).Value = m.Title;
            ws.Cell(row, 3).Value = m.TicketsSold;
            ws.Cell(row, 4).Value = m.Revenue;
            ws.Cell(row, 4).Style.NumberFormat.Format = moneyFmt;
            row++;
        }
        row++;

        // ── Phương thức thanh toán ──
        row = WriteSectionTitle(ws, row, "Phương thức thanh toán");
        row = WriteHeader(ws, row, "Phương thức", "Số giao dịch", "Số tiền");
        foreach (var p in vm.PaymentMethods)
        {
            ws.Cell(row, 1).Value = p.Method;
            ws.Cell(row, 2).Value = p.Count;
            ws.Cell(row, 3).Value = p.Amount;
            ws.Cell(row, 3).Style.NumberFormat.Format = moneyFmt;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var fileName = $"BaoCao_{vm.DateFrom:yyyyMMdd}_{vm.DateTo:yyyyMMdd}.xlsx";
        return (ms.ToArray(), fileName);
    }

    // Dòng tiêu đề mục (bôi đậm, có nền), trả về dòng kế tiếp.
    private static int WriteSectionTitle(IXLWorksheet ws, int row, string title)
    {
        ws.Cell(row, 1).Value = title;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 12;
        return row + 1;
    }

    // Dòng KPI: nhãn + giá trị.
    private static void WriteKpi(IXLWorksheet ws, int row, string label, decimal value, string fmt)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 2).Value = value;
        ws.Cell(row, 2).Style.NumberFormat.Format = fmt;
    }

    // Hàng tiêu đề bảng (bôi đậm + nền xám), trả về dòng kế tiếp.
    private static int WriteHeader(IXLWorksheet ws, int row, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
        }
        return row + 1;
    }
}

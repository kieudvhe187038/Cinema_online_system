using Cinema_System.Application.Interfaces;
using Cinema_System.Application.Services;
using Cinema_System.Helpers;
using Cinema_System.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Đăng ký SubfolderViewLocationExpander để controller trong các area con
// (Admin / Manager / Staff) tìm được view ở /Views/{Area}/{Controller}/{Action}.cshtml.
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationExpanders.Add(new SubfolderViewLocationExpander());
    });

var connectionStr = builder.Configuration.GetConnectionString("MyCnn");
builder.Services.AddDbContext<CinemaWebDbContext>(options =>
    options.UseSqlServer(connectionStr));

// Register Unit of Work + application services
builder.Services.AddScoped<IUnitOfWork, Cinema_System.Infrastructure.UnitOfWork.UnitOfWork>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPointConfigService, PointConfigService>();
builder.Services.AddScoped<ISeatTypeService, SeatTypeService>();
builder.Services.AddScoped<IRoomTypeService, RoomTypeService>();

// Inter2 (Staff) - luồng đặt vé tại quầy
builder.Services.AddScoped<IStaffContextService, StaffContextService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<ICounterBookingService, CounterBookingService>();

var app = builder.Build();

// Dùng culture cố định (dấu "." cho số thập phân) để parse/format số nhất quán
// trên mọi máy — tránh lỗi nhập "0.0001" bị hiểu sai trên Windows tiếng Việt.
var invariantCulture = new[] { System.Globalization.CultureInfo.InvariantCulture };
app.UseRequestLocalization(new Microsoft.AspNetCore.Builder.RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(
        System.Globalization.CultureInfo.InvariantCulture),
    SupportedCultures = invariantCulture,
    SupportedUICultures = invariantCulture
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

// Trang Home/Movies (public) đã bị xóa — điều hướng gốc về trang quản trị.
app.MapGet("/", () => Results.Redirect("/Admin/User"));

app.Run();

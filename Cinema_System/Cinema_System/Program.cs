using System.Security.Claims;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.Mappings;
using Cinema_System.Application.Services;
using Cinema_System.Helpers;
using Cinema_System.Infrastructure.Data;
using Cinema_System.Infrastructure.Email;
using Cinema_System.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;

// Nạp biến môi trường từ file .env (nếu có) trước khi build cấu hình.
// Các key dạng EmailSettings__User sẽ tự map vào section EmailSettings.
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Đăng ký SubfolderViewLocationExpander để controller trong các area con
// (Admin / Manager / Staff) tìm được view ở /Views/{Area}/{Controller}/{Action}.cshtml.
builder.Services.AddControllersWithViews(options =>
    {
        // Việt hóa các thông báo lỗi mặc định của model binder (ví dụ "The value '' is invalid."),
        // vốn hiện ra trước khi DataAnnotations/IValidatableObject chạy nên không tự dịch được.
        var provider = options.ModelBindingMessageProvider;
        provider.SetValueIsInvalidAccessor(v => $"Giá trị '{v}' không hợp lệ.");
        provider.SetAttemptedValueIsInvalidAccessor((v, f) => $"Giá trị '{v}' không hợp lệ cho {f}.");
        provider.SetValueMustNotBeNullAccessor(v => "Trường này là bắt buộc.");
        provider.SetMissingBindRequiredValueAccessor(f => $"Thiếu giá trị cho trường '{f}'.");
        provider.SetMissingKeyOrValueAccessor(() => "Trường này là bắt buộc.");
        provider.SetUnknownValueIsInvalidAccessor(f => $"Giá trị nhập vào cho {f} không hợp lệ.");
        provider.SetValueMustBeANumberAccessor(f => $"Trường {f} phải là một số.");
        provider.SetNonPropertyValueMustBeANumberAccessor(() => "Giá trị phải là một số.");
        provider.SetMissingRequestBodyRequiredValueAccessor(() => "Dữ liệu gửi lên không được để trống.");
    })
    .AddRazorOptions(options =>
    {
        options.ViewLocationExpanders.Add(new SubfolderViewLocationExpander());
    });

// Cho phép tìm view trong thư mục con /Views/Admin/{controller} và /Views/Manager/{controller}
// (module quản trị của taido đặt view theo cấu trúc thư mục con).
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new SubfolderViewLocationExpander());
});
// AutoMapper 16.x: quét toàn bộ assembly Application để nạp mọi Profile
// (UserProfile, AuthMappingProfile, MovieProfile, FoodBeverageProfile, ProfileMappingProfile).
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MovieProfile).Assembly));

// Kết nối đến cơ sở dữ liệu SQL Server của ứng dụng.
var connectionStr = builder.Configuration.GetConnectionString("MyCnn");
builder.Services.AddDbContext<CinemaWebDbContext>(options =>
    options.UseSqlServer(connectionStr));

// Cấu hình email (SMTP) đọc từ section "EmailSettings".
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Dependency Injection cho tầng Infrastructure & Application.
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnitOfWork, Cinema_System.Infrastructure.UnitOfWork.UnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IShowtimeService, ShowtimeService>();
builder.Services.AddScoped<IFoodBeverageService, FoodBeverageService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IPriceService, PriceService>();
builder.Services.AddScoped<IVatService, VatService>();
// Cổng thanh toán VNPay (sandbox). Secret nạp từ .env: Vnpay__TmnCode, Vnpay__HashSecret.
builder.Services.Configure<Cinema_System.Infrastructure.PaymentGateway.VnpaySettings>(builder.Configuration.GetSection("Vnpay"));
builder.Services.AddScoped<IVnpayService, Cinema_System.Infrastructure.PaymentGateway.VnpayService>();
// VietQR (QR chuyển khoản). Thông tin TK nạp từ .env: VietQr__BankCode, VietQr__AccountNo, VietQr__AccountName.
builder.Services.Configure<Cinema_System.Infrastructure.PaymentGateway.VietQrSettings>(builder.Configuration.GetSection("VietQr"));
builder.Services.AddScoped<IVietQrService, Cinema_System.Infrastructure.PaymentGateway.VietQrService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
// Module quản trị của taido (quản lý người dùng, tỉ lệ điểm, loại phòng/ghế).
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPointConfigService, PointConfigService>();
builder.Services.AddScoped<IShowtimeIncidentService, ShowtimeIncidentService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
// Ghi Audit_Logs tại các endpoint quản trị (cần IHttpContextAccessor để lấy user + IP).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditLogWriter, AuditLogWriter>();
builder.Services.AddScoped<ISeatTypeService, SeatTypeService>();
builder.Services.AddScoped<IRoomTypeService, RoomTypeService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

// Đăng ký chatbot AI service và HTTP client dùng để gọi Google Gemini API.
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IShowtimeScheduleService, ShowtimeScheduleService>();

// Đăng ký Background Service tự động cập nhật trạng thái lịch chiếu
builder.Services.AddHostedService<ShowtimeStatusBackgroundService>();

// Session lưu thông tin đăng ký tạm thời (chờ xác nhận OTP).
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IRoomManagementService, RoomManagementService>();

// Cookie Authentication.
var authBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromDays(15);
        options.SlidingExpiration = true;

        // Cookie sống tới 15 ngày (sliding) nên vai trò/trạng thái ghi trong cookie có thể
        // cũ hơn DB rất nhiều. Đối chiếu lại mỗi request: tài khoản bị khóa -> đăng xuất
        // ngay; đổi vai trò -> phát hành lại claim Role mới mà không cần đăng nhập lại.
        options.Events.OnValidatePrincipal = async context =>
        {
            var idClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (idClaim is null || !Guid.TryParse(idClaim, out var userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
            var user = await unitOfWork.Users.GetByIdWithRoleAsync(userId);

            if (user is null || !string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var cookieRole = context.Principal!.FindFirstValue(ClaimTypes.Role);
            if (!string.Equals(cookieRole, user.Role?.Name, StringComparison.Ordinal))
            {
                var identity = (ClaimsIdentity)context.Principal!.Identity!;
                var oldRoleClaim = identity.FindFirst(ClaimTypes.Role);
                if (oldRoleClaim is not null) identity.RemoveClaim(oldRoleClaim);
                identity.AddClaim(new Claim(ClaimTypes.Role, user.Role?.Name ?? string.Empty));
                context.ShouldRenew = true;
            }
        };
    });

// Đăng nhập Google (chỉ bật khi đã cấu hình ClientId/ClientSecret trong .env).
var googleClientId = builder.Configuration["GoogleSettings:ClientId"];
var googleClientSecret = builder.Configuration["GoogleSettings:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder
        .AddCookie("External") // cookie tạm giữ danh tính Google trong lúc xử lý callback
        .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.SignInScheme = "External";

            // Khi user bấm "Huỷ" trên màn hình Google, hoặc cookie correlation bị mất
            // ("Correlation failed"), middleware sẽ ném AuthenticationFailureException trước khi
            // tới controller. Xử lý tại đây để redirect về trang login thay vì hiện trang lỗi.
            options.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/login?error=google_cancelled");
                return Task.CompletedTask;
            };
        });
}

// Inter2 (Staff) - luồng đặt vé tại quầy
builder.Services.AddScoped<IStaffContextService, StaffContextService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IBookingManagementService, BookingManagementService>();
builder.Services.AddScoped<ICounterBookingService, CounterBookingService>();

// Inter3 (Staff) - check-in vé
builder.Services.AddScoped<ITicketCheckinService, TicketCheckinService>();

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStatusCodePagesWithReExecute("/Home/NotFoundPage", "?statusCode={0}");
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

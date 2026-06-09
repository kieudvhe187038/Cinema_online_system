using Cinema_System.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAutoMapper(typeof(Cinema_System.Application.Mappings.ProfileMappingProfile).Assembly);

var connectionStr = builder.Configuration.GetConnectionString("MyCnn");
builder.Services.AddDbContext<CinemaWebDbContext>(options =>
    options.UseSqlServer(connectionStr));

builder.Services.AddScoped<Cinema_System.Application.Interfaces.IUnitOfWork,
    Cinema_System.Infrastructure.UnitOfWork.UnitOfWork>();
builder.Services.AddScoped<Cinema_System.Application.Interfaces.IProfileService,
    Cinema_System.Application.Services.ProfileService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

using Cinema_System.Application.Interfaces;
using Cinema_System.Application.Services;
using Cinema_System.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionStr = builder.Configuration.GetConnectionString("MyCnn");
builder.Services.AddDbContext<CinemaWebDbContext>(options =>
    options.UseSqlServer(connectionStr));

// Register application services
builder.Services.AddScoped<IMovieService, MovieService>();

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

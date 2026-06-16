using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Application.Services;

public class StaffContextService : IStaffContextService
{
    private const string RoleStaff = "Staff";
    private const string StatusActive = "Active";

    private readonly IUnitOfWork _unitOfWork;

    public StaffContextService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<User?> GetCurrentStaffAsync()
    {
        // Tạm thời (chưa có đăng nhập): lấy nhân viên (role Staff) đang hoạt động đầu tiên.
        // Khi tích hợp đăng nhập, chỉ cần thay phần lấy user ở đây bằng user hiện tại.
        var staff = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Status == StatusActive && u.Role.Name == RoleStaff,
            include: q => q.Include(u => u.Role));

        return staff;
    }
}

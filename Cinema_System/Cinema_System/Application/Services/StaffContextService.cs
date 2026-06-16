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
        // Ưu tiên nhân viên (Staff) đang hoạt động; nếu chưa có thì lấy bất kỳ
        // tài khoản nội bộ (Manager/Admin) active để vẫn thao tác được khi seed thiếu.
        var staff = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Status == StatusActive && u.Role.Name == RoleStaff,
            include: q => q.Include(u => u.Role));

        return staff;
    }
}

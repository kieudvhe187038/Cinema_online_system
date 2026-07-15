using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

public class StaffContextService : IStaffContextService
{
    private readonly IUnitOfWork _unitOfWork;

    public StaffContextService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<User?> GetCurrentStaffAsync(Guid staffId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(staffId);

        // Cookie đăng nhập có thể còn hiệu lực tới 15 ngày sau khi Admin đã khóa tài
        // khoản — chặn ở đây để nhân viên bị vô hiệu hóa không thể tạo đơn/check-in
        // dù session cũ chưa hết hạn.
        if (user is null || !string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
            return null;

        return user;
    }
}

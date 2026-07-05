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
        return await _unitOfWork.Users.GetByIdAsync(staffId);
    }
}

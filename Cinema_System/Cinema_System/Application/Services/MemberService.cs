using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;

namespace Cinema_System.Application.Services;

public class MemberService : IMemberService
{
    private readonly IUnitOfWork _unitOfWork;

    public MemberService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MemberDTO?> LookupByPhoneAsync(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var normalized = phone.Trim();
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Phone == normalized);

        if (user is null)
            return null;

        return new MemberDTO
        {
            Id = user.Id,
            FullName = user.FullName,
            Phone = user.Phone,
            Email = user.Email,
            RewardPoints = user.RewardPoints ?? 0
        };
    }
}

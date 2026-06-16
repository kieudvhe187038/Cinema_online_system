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

        // Bỏ mọi khoảng trắng để "0912 345 678" khớp với "0912345678" lưu trong DB.
        var normalized = new string(phone.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (normalized.Length == 0)
            return null;

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

using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;

namespace Cinema_System.Application.Services;

public class MemberService : IMemberService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public MemberService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<MemberDTO?> LookupByPhoneAsync(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        // Bỏ mọi khoảng trắng để "0912 345 678" khớp với "0912345678" lưu trong DB.
        var normalized = new string(phone.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (normalized.Length == 0)
            return null;

        var user = await _unitOfWork.Users.GetByPhoneAsync(normalized);

        return user is null ? null : _mapper.Map<MemberDTO>(user);
    }
}

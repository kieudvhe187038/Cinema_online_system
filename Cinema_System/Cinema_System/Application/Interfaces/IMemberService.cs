using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.Interfaces;

/// <summary>Tra cứu thành viên theo số điện thoại để gắn vào đơn offline và tích điểm.</summary>
public interface IMemberService
{
    Task<MemberDTO?> LookupByPhoneAsync(string? phone);
}

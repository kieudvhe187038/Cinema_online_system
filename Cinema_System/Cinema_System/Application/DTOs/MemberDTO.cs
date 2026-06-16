namespace Cinema_System.Application.DTOs;

/// <summary>Kết quả tra cứu thành viên theo SĐT (Member Lookup).</summary>
public class MemberDTO
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public int RewardPoints { get; set; }
}

using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.Interfaces;

// Hợp đồng nghiệp vụ Hồ sơ; Controller phụ thuộc interface này (DI), không phụ thuộc class cụ thể.
public interface IProfileService
{
    Task<ProfileDto?> GetProfileAsync(Guid userId);
    Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
    Task<(bool Ok, string? Error)> ChangePasswordAsync(Guid userId, string oldPass, string newPass);
    Task<List<PointHistoryDto>> GetPointHistoryAsync(Guid userId);
}

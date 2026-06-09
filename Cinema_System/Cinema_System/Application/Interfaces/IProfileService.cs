using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.Interfaces
{
    // Nơi chứa nghiệp vụ hồ sơ. Controller chỉ gọi service này.
    public interface IProfileService
    {
        Task<ProfileDto?> GetProfileAsync(Guid userId);
        Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
        Task<(bool Ok, string? Error)> ChangePasswordAsync(Guid userId, string oldPass, string newPass);
    }
}

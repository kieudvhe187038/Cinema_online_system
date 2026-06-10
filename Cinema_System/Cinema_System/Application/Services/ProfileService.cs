using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        public ProfileService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }
        public async Task<ProfileDto?> GetProfileAsync(Guid userId)
        {
            var user = await _uow.Repository<User>()
                .FirstOrDefaultAsync(u => u.Id == userId, u => u.Role);
            if (user == null) return null;

            return _mapper.Map<ProfileDto>(user);
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var repo = _uow.Repository<User>();
            var user = await repo.GetByIdAsync(userId);
            if (user == null) return false;

            user.FullName = dto.FullName;
            user.Phone = dto.Phone;
            if (!string.IsNullOrEmpty(dto.AvatarUrl))
                user.AvatarUrl = dto.AvatarUrl;
            user.UpdatedAt = DateTime.Now;

            repo.Update(user);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Ok, string? Error)> ChangePasswordAsync(Guid userId, string oldPass, string newPass)
        {
            var repo = _uow.Repository<User>();
            var user = await repo.GetByIdAsync(userId);
            if (user == null) return (false, "Không tìm thấy người dùng");

            bool matched;
            try
            {
                matched = BCrypt.Net.BCrypt.Verify(oldPass, user.PasswordHash);
            }
            catch
            {
                matched = false;
            }
            if (!matched)
                return (false, "Mật khẩu hiện tại không đúng");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPass);
            user.UpdatedAt = DateTime.Now;
            repo.Update(user);
            await _uow.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<PointHistoryDto>> GetPointHistoryAsync(Guid userId)
        {
            // Lấy tất cả giao dịch điểm của user
            var list = await _uow.Repository<RewardPointHistory>()
                .GetAllAsync(h => h.UserId == userId);

            // Mới nhất lên đầu, rồi map sang DTO
            return list
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => _mapper.Map<PointHistoryDto>(h))
                .ToList();
        }
    }
}

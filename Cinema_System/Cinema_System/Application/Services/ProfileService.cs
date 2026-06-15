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

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto data)
        {
            var userRepo = _uow.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);
            if (user == null) return false;

            user.FullName = data.FullName;
            user.Phone = data.Phone;
            if (!string.IsNullOrEmpty(data.AvatarUrl))
                user.AvatarUrl = data.AvatarUrl;
            user.UpdatedAt = DateTime.Now;

            userRepo.Update(user);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Ok, string? Error)> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
        {
            var userRepo = _uow.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);
            if (user == null) return (false, "Không tìm thấy người dùng");

            bool isOldPasswordCorrect;
            try
            {
                isOldPasswordCorrect = BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash);
            }
            catch
            {
                isOldPasswordCorrect = false;
            }
            if (!isOldPasswordCorrect)
                return (false, "Mật khẩu hiện tại không đúng");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.Now;
            userRepo.Update(user);
            await _uow.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<PointHistoryDto>> GetPointHistoryAsync(Guid userId)
        {
            // Lấy tất cả giao dịch điểm của user
            var histories = await _uow.Repository<RewardPointHistory>()
                .GetAllAsync(h => h.UserId == userId);

            // Mới nhất lên đầu, rồi map sang DTO
            return histories
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => _mapper.Map<PointHistoryDto>(h))
                .ToList();
        }
    }
}

using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services
{
    // Tầng Application: xử lý nghiệp vụ hồ sơ, truy cập DB qua IUnitOfWork (không đụng DbContext trực tiếp).
    public class ProfileService : IProfileService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public ProfileService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        // Lấy hồ sơ user (kèm Role) -> DTO
        public async Task<ProfileDto?> GetProfileAsync(Guid userId)
        {
            var user = await _uow.Repository<User>()
                .FirstOrDefaultAsync(u => u.Id == userId, u => u.Role); // u => u.Role: JOIN lấy luôn vai trò
            if (user == null) return null;

            return _mapper.Map<ProfileDto>(user);
        }

        // Cập nhật họ tên / SĐT / avatar
        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto data)
        {
            var userRepo = _uow.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);
            if (user == null) return false;

            // Map tay để chỉ ghi đè field cần đổi, tránh set null lên field khác
            user.FullName = data.FullName;
            user.Phone = data.Phone;
            if (!string.IsNullOrEmpty(data.AvatarUrl)) user.AvatarUrl = data.AvatarUrl; // chỉ đổi ảnh khi có upload
            user.UpdatedAt = DateTime.Now;

            userRepo.Update(user);
            await _uow.SaveChangesAsync();
            return true;
        }

        // Đổi mật khẩu: kiểm tra mật khẩu cũ rồi lưu mật khẩu mới đã hash
        public async Task<(bool Ok, string? Error)> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
        {
            var userRepo = _uow.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);
            if (user == null) return (false, "Không tìm thấy người dùng");

            // Bọc try/catch vì hash mẫu trong seed có thể sai định dạng BCrypt -> Verify ném lỗi
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

            // Chặn đặt mật khẩu mới trùng mật khẩu hiện tại
            if (oldPassword == newPassword)
                return (false, "Mật khẩu mới phải khác mật khẩu hiện tại");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword); // luôn lưu hash, không lưu thô
            user.UpdatedAt = DateTime.Now;
            userRepo.Update(user);
            await _uow.SaveChangesAsync();
            return (true, null);
        }

        // Lấy lịch sử điểm, mới nhất lên đầu
        public async Task<List<PointHistoryDto>> GetPointHistoryAsync(Guid userId)
        {
            var histories = await _uow.Repository<RewardPointHistory>()
                .GetAllAsync(h => h.UserId == userId);

            return histories
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => _mapper.Map<PointHistoryDto>(h))
                .ToList();
        }
    }
}

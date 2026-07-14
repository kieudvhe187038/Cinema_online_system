using System.Globalization;
using System.Linq.Expressions;
using Cinema_System.Application.Common;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cinema_System.Application.Services
{
    public class ShowtimeIncidentService : IShowtimeIncidentService
    {
        private const string RewardRateKey = "reward_point_rate";
        private readonly IUnitOfWork _unitOfWork;

        public ShowtimeIncidentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // Thống kê + danh sách suất chiếu (kèm cờ đã có sự cố)
        public async Task<IncidentListViewModel> GetIndexAsync(string? scope, int page, int pageSize = 8)
        {
            scope = string.IsNullOrWhiteSpace(scope) ? "all" : scope.ToLower();

            var now = DateTime.Now;
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            // Số liệu thật từ DB
            var stats = new IncidentStatsViewModel
            {
                ShowtimesToday = await _unitOfWork.Showtimes.CountAsync(s => s.StartTime >= today && s.StartTime < tomorrow),
                IncidentsThisMonth = await _unitOfWork.ShowtimeIncidents.CountAsync(i => i.CreatedAt >= monthStart),
                TotalIncidents = await _unitOfWork.ShowtimeIncidents.CountAsync()
            };

            // Lọc theo phạm vi
            Expression<Func<Showtime, bool>>? predicate = scope switch
            {
                // "Đang chiếu" = suất đang diễn ra theo giờ thực (hoặc được đánh dấu Live)
                "live" => s => (s.StartTime <= now && s.EndTime > now) || s.Status == "Live",
                "today" => s => s.StartTime >= today && s.StartTime < tomorrow,
                _ => null
            };

            var showtimes = (await _unitOfWork.Showtimes.GetAllAsync(
                predicate,
                includeProperties: new[] { "Movie", "Room", "Room.RoomType" },
                orderBy: q => q.OrderByDescending(s => s.StartTime))).ToList();

            var paged = PagedResult<Showtime>.Create(showtimes, page, pageSize);
            var pageIds = paged.Items.Select(s => s.Id).ToList();

            // Đếm theo từng suất trong trang (lọc IN cho nhẹ)
            var paidBookings = await _unitOfWork.Bookings.GetAllAsync(
                b => b.PaymentStatus == "Paid" && pageIds.Contains(b.ShowtimeId));

            var tickets = await _unitOfWork.Tickets.GetAllAsync(t => pageIds.Contains(t.ShowtimeId));
            var seatsByShowtime = tickets.GroupBy(t => t.ShowtimeId).ToDictionary(g => g.Key, g => g.Count());

            var incidentShowtimeIds = (await _unitOfWork.ShowtimeIncidents.GetAllAsync(
                i => i.ShowtimeId != null && pageIds.Contains(i.ShowtimeId.Value)))
                .Select(i => i.ShowtimeId!.Value).ToHashSet();

            var items = paged.Items.Select(s => new IncidentShowtimeItemViewModel
            {
                ShowtimeId = s.Id,
                MovieTitle = s.Movie?.Title ?? "(Phim)",
                PosterUrl = s.Movie?.PosterUrl,
                RoomName = s.Room?.Name ?? "-",
                RoomTypeName = s.Room?.RoomType?.Name ?? "-",
                StartTime = s.StartTime,
                Status = s.Status,
                RoomStatus = s.Room?.Status,
                SeatsSold = seatsByShowtime.GetValueOrDefault(s.Id),
                Capacity = s.Room?.TotalSeats ?? 0,
                HasIncident = incidentShowtimeIds.Contains(s.Id)
            }).ToList();

            return new IncidentListViewModel
            {
                Stats = stats,
                Items = items,
                Scope = scope,
                CurrentPage = paged.CurrentPage,
                TotalPages = paged.TotalPages,
                TotalItems = paged.TotalCount
            };
        }

        // Dựng form khai báo (nạp dropdown). Nếu suất ở phòng bảo trì -> gợi ý sẵn mô tả + tick hủy.
        public async Task<DeclareIncidentViewModel> BuildDeclareFormAsync(Guid? showtimeId = null)
        {
            var vm = new DeclareIncidentViewModel();

            if (showtimeId.HasValue && showtimeId.Value != Guid.Empty)
            {
                vm.ShowtimeId = showtimeId.Value;

                var showtime = await _unitOfWork.Showtimes.FirstOrDefaultAsync(
                    s => s.Id == showtimeId.Value, includeProperties: new[] { "Room" });

                if (showtime?.Room?.Status is "Maintenance" or "Inactive")
                {
                    vm.Description = $"Phòng {showtime.Room.Name} đang bảo trì, không thể phục vụ suất chiếu.";
                    vm.CancelShowtime = true;
                }
            }

            await FillOptionsAsync(vm);
            return vm;
        }

        // Ghi nhận sự cố + hoàn điểm + (tùy chọn) hủy suất
        public async Task<Result> DeclareAsync(DeclareIncidentViewModel form, Guid managerId)
        {
            var showtimeId = form.ShowtimeId!.Value;
            var showtime = await _unitOfWork.Showtimes.FirstOrDefaultAsync(s => s.Id == showtimeId);
            if (showtime is null) return Result.Failure("Không tìm thấy suất chiếu.");

            if (form.RefundPointsRate < 0 || form.RefundPointsRate > 5)
                return Result.Failure("Hệ số hoàn điểm phải từ 0 đến 5.");

            if (form.CompensationPromoId.HasValue &&
                !await _unitOfWork.Promotions.ExistsAsync(p => p.Id == form.CompensationPromoId.Value))
                return Result.Failure("Voucher bồi thường không hợp lệ.");

            var rateConfig = await _unitOfWork.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == RewardRateKey);
            var rewardRate = ParseDecimal(rateConfig?.ConfigValue);
            var now = DateTime.Now;

            await RefundShowtimePaidCustomersAsync(showtimeId, form.RefundPointsRate, rewardRate, now, new Dictionary<Guid, User>());

            await _unitOfWork.ShowtimeIncidents.AddAsync(new ShowtimeIncident
            {
                Id = Guid.NewGuid(),
                ShowtimeId = showtimeId,
                Description = form.Description,
                RefundPointsRate = form.RefundPointsRate,
                CompensationPromo = form.CompensationPromoId,
                CreatedBy = managerId,
                CreatedAt = now
            });

            if (form.CancelShowtime && showtime.Status != "Cancelled")
            {
                showtime.Status = "Cancelled";
                _unitOfWork.Showtimes.Update(showtime);
            }

            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        // Nạp dropdown cho form
        private async Task FillOptionsAsync(DeclareIncidentViewModel vm)
        {
            var showtimes = (await _unitOfWork.Showtimes.GetAllAsync(
                s => s.Status != "Completed",
                includeProperties: new[] { "Movie", "Room" },
                orderBy: q => q.OrderByDescending(s => s.StartTime))).Take(100).ToList();

            vm.ShowtimeOptions = showtimes.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Movie?.Title} · {s.Room?.Name} · {s.StartTime:HH:mm dd/MM}"
            }).ToList();

            var promos = (await _unitOfWork.Promotions.GetAllAsync(
                p => p.Status == "Active",
                orderBy: q => q.OrderBy(p => p.Code))).ToList();

            vm.PromoOptions = promos.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.DiscountType == "Percent"
                    ? $"{p.Code} (giảm {p.DiscountAmount}%)"
                    : $"{p.Code} (giảm {p.DiscountAmount:#,##0}đ)"
            }).ToList();
        }

        private static decimal ParseDecimal(string? value)
            => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;

        // Hoàn điểm cho mọi đơn ĐÃ THANH TOÁN của 1 suất. Trả về số lượt khách được hoàn.
        // userCache: dùng CHUNG 1 instance User qua nhiều suất (tránh EF track trùng key khi 1 khách có vé ở nhiều suất).
        private async Task<int> RefundShowtimePaidCustomersAsync(
            Guid showtimeId, decimal refundRate, decimal rewardRate, DateTime now, Dictionary<Guid, User> userCache)
        {
            var paidBookings = (await _unitOfWork.Bookings.GetAllAsync(
                b => b.ShowtimeId == showtimeId && b.PaymentStatus == "Paid" && b.UserId != null)).ToList();

            int count = 0;
            foreach (var booking in paidBookings)
            {
                var points = (int)Math.Round(booking.FinalAmount * rewardRate * refundRate, MidpointRounding.AwayFromZero);
                if (points <= 0) continue;

                await _unitOfWork.RewardPointHistories.AddAsync(new RewardPointHistory
                {
                    Id = Guid.NewGuid(),
                    UserId = booking.UserId!.Value,
                    BookingId = booking.Id,
                    PointsChanged = points,
                    ActionType = "Refund_Rollback",
                    Description = "Hoàn điểm do sự cố / bảo trì phòng",
                    CreatedAt = now
                });

                var uid = booking.UserId!.Value;
                if (!userCache.TryGetValue(uid, out var user))
                {
                    user = await _unitOfWork.Users.GetByIdAsync(uid);
                    if (user != null) userCache[uid] = user;
                }
                if (user != null)
                {
                    user.RewardPoints = (user.RewardPoints ?? 0) + points;
                    _unitOfWork.Users.Update(user);
                }
                count++;
            }
            return count;
        }

        // Khôi phục suất chiếu đã hủy do sự cố (Cancelled -> Scheduled). KHÔNG thu lại điểm đã hoàn.
        public async Task<Result> RestoreShowtimeAsync(Guid showtimeId)
        {
            var showtime = await _unitOfWork.Showtimes.FirstOrDefaultAsync(s => s.Id == showtimeId);
            if (showtime is null) return Result.Failure("Không tìm thấy suất chiếu.");
            if (showtime.Status != "Cancelled") return Result.Failure("Suất chiếu không ở trạng thái đã hủy.");
            // Chỉ khôi phục suất CHƯA qua giờ (khôi phục suất đã qua sẽ bị background service chuyển Completed ngay)
            if (showtime.StartTime <= DateTime.Now) return Result.Failure("Suất chiếu đã qua giờ chiếu, không thể khôi phục để bán lại.");

            showtime.Status = "Scheduled";
            _unitOfWork.Showtimes.Update(showtime);
            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        // Dựng form bảo trì (mặc định từ bây giờ -> +2 giờ)
        public async Task<MaintainRoomViewModel> BuildMaintainFormAsync()
        {
            var vm = new MaintainRoomViewModel
            {
                FromTime = DateTime.Now,
                ToTime = DateTime.Now.AddHours(2)
            };
            await FillRoomAndPromoOptionsAsync(vm);
            return vm;
        }

        // Hủy + hoàn hàng loạt: mọi suất CHƯA diễn của phòng nằm trong [Từ; Đến]
        public async Task<Result<string>> MaintainRoomAsync(MaintainRoomViewModel form, Guid managerId)
        {
            var roomId = form.RoomId!.Value;
            if (!await _unitOfWork.Rooms.ExistsAsync(r => r.Id == roomId))
                return Result<string>.Failure("Không tìm thấy phòng.");

            var now = DateTime.Now;
            var from = form.FromTime!.Value;
            var to = form.ToTime!.Value;

            if (from < now) return Result<string>.Failure("Không được chọn thời gian trong quá khứ.");
            if (to <= from) return Result<string>.Failure("Thời gian kết thúc phải sau thời gian bắt đầu.");

            if (form.CompensationPromoId.HasValue &&
                !await _unitOfWork.Promotions.ExistsAsync(p => p.Id == form.CompensationPromoId.Value))
                return Result<string>.Failure("Voucher bồi thường không hợp lệ.");

            var rateConfig = await _unitOfWork.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == RewardRateKey);
            var rewardRate = ParseDecimal(rateConfig?.ConfigValue);

            var showtimes = (await _unitOfWork.Showtimes.GetAllAsync(
                s => s.RoomId == roomId && s.Status != "Cancelled" &&
                     s.StartTime > now && s.StartTime >= from && s.StartTime <= to)).ToList();

            if (showtimes.Count == 0)
                return Result<string>.Failure("Không có suất chiếu nào của phòng trong khoảng thời gian đã chọn.");

            int totalCustomers = 0;
            var userCache = new Dictionary<Guid, User>();   // dùng chung cả vòng lặp -> tránh track trùng User
            foreach (var st in showtimes)
            {
                totalCustomers += await RefundShowtimePaidCustomersAsync(st.Id, form.RefundPointsRate, rewardRate, now, userCache);

                await _unitOfWork.ShowtimeIncidents.AddAsync(new ShowtimeIncident
                {
                    Id = Guid.NewGuid(),
                    ShowtimeId = st.Id,
                    Description = form.Description,
                    RefundPointsRate = form.RefundPointsRate,
                    CompensationPromo = form.CompensationPromoId,
                    CreatedBy = managerId,
                    CreatedAt = now
                });

                st.Status = "Cancelled";
                _unitOfWork.Showtimes.Update(st);
            }

            await _unitOfWork.SaveChangesAsync();
            return Result<string>.Success($"Đã bảo trì phòng: hủy {showtimes.Count} suất chiếu, hoàn điểm cho {totalCustomers} lượt khách.");
        }

        // Nạp dropdown phòng + voucher cho form bảo trì
        private async Task FillRoomAndPromoOptionsAsync(MaintainRoomViewModel vm)
        {
            var rooms = (await _unitOfWork.Rooms.GetAllAsync(
                includeProperties: new[] { "RoomType" },
                orderBy: q => q.OrderBy(r => r.Name))).ToList();
            vm.RoomOptions = rooms.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = $"{r.Name} ({r.RoomType?.Name})"
            }).ToList();

            var promos = (await _unitOfWork.Promotions.GetAllAsync(
                p => p.Status == "Active", orderBy: q => q.OrderBy(p => p.Code))).ToList();
            vm.PromoOptions = promos.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.DiscountType == "Percent"
                    ? $"{p.Code} (giảm {p.DiscountAmount}%)"
                    : $"{p.Code} (giảm {p.DiscountAmount:#,##0}đ)"
            }).ToList();
        }

    }
}
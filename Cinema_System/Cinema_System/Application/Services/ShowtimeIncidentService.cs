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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IPointConfigService _pointConfig;

        public ShowtimeIncidentService(IUnitOfWork unitOfWork, IEmailService emailService, IPointConfigService pointConfig)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _pointConfig = pointConfig;
        }

        // Thống kê + danh sách suất chiếu (kèm cờ đã có sự cố)
        public async Task<IncidentListViewModel> GetIndexAsync(string? scope, int page, int pageSize = 8, string? search = null)
        {
            scope = string.IsNullOrWhiteSpace(scope) ? "all" : scope.ToLower();
            var keyword = search?.Trim();

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

            // Màn này là màn THAO TÁC (khai báo sự cố / hủy / khôi phục) → chỉ hiển thị suất còn
            // thao tác được, tức CHƯA kết thúc. Suất đã kết thúc thì mọi thao tác đều bị chặn
            // (không khai báo/hủy được, khôi phục cũng bị từ chối) nên bỏ hẳn khỏi danh sách,
            // kể cả suất đã có sự cố (lịch sử sự cố xem ở Báo cáo & Thống kê / Audit Log).
            var incidentIds = (await _unitOfWork.ShowtimeIncidents.GetAllAsync(i => i.ShowtimeId != null))
                .Select(i => i.ShowtimeId!.Value).ToHashSet();

            var showtimes = (await _unitOfWork.Showtimes.GetAllAsync(
                includeProperties: new[] { "Movie", "Room", "Room.RoomType" },
                orderBy: q => q.OrderByDescending(s => s.StartTime))).ToList();

            bool IsEnded(Showtime s) => s.Status == "Completed" || s.EndTime < now;
            showtimes = showtimes.Where(s => !IsEnded(s)).ToList();

            // Lọc theo phạm vi
            if (scope == "live")   // "Đang chiếu" = đang diễn ra theo giờ thực (hoặc đánh dấu Live)
                showtimes = showtimes.Where(s => (s.StartTime <= now && s.EndTime > now) || s.Status == "Live").ToList();
            else if (scope == "today")
                showtimes = showtimes.Where(s => s.StartTime >= today && s.StartTime < tomorrow).ToList();
            else if (scope == "incident")   // "Đã có sự cố" = suất đã được khai báo sự cố
                showtimes = showtimes.Where(s => incidentIds.Contains(s.Id)).ToList();

            // Tìm kiếm theo tên phim hoặc tên phòng (gõ không dấu vẫn ra)
            var searchKey = VietnameseText.ToSearchKey(keyword);
            if (searchKey.Length > 0)
            {
                showtimes = showtimes.Where(s =>
                    VietnameseText.Contains(s.Movie?.Title, searchKey) ||
                    VietnameseText.Contains(s.Room?.Name, searchKey)).ToList();
            }

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
                EndTime = s.EndTime,
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
                Search = keyword,
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
            var showtime = await _unitOfWork.Showtimes.FirstOrDefaultAsync(
                s => s.Id == showtimeId, includeProperties: new[] { "Movie", "Room" });
            if (showtime is null) return Result.Failure("Không tìm thấy suất chiếu.");

            // Sự cố chỉ áp dụng cho suất CHƯA kết thúc (đồng bộ với danh sách suất trong form).
            if (showtime.Status == "Completed" || showtime.EndTime < DateTime.Now)
                return Result.Failure("Không thể khai báo sự cố cho suất chiếu đã kết thúc.");

            if (form.RefundPointsRate < 0 || form.RefundPointsRate > 5)
                return Result.Failure("Hệ số hoàn điểm phải từ 0 đến 5.");

            if (form.CompensationPromoId.HasValue &&
                !await _unitOfWork.Promotions.ExistsAsync(p => p.Id == form.CompensationPromoId.Value))
                return Result.Failure("Voucher bồi thường không hợp lệ.");

            // Tỉ lệ tích điểm lấy từ cấu hình dùng chung (Manager > Cấu hình điểm thưởng).
            var rewardRate = (await _pointConfig.GetPolicyAsync()).EarnRate;
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

            var willCancel = form.CancelShowtime && showtime.Status != "Cancelled";
            if (willCancel)
            {
                showtime.Status = "Cancelled";
                _unitOfWork.Showtimes.Update(showtime);
            }

            await _unitOfWork.SaveChangesAsync();

            // Chỉ khi HỦY suất mới gửi email báo lý do cho toàn bộ khách đã thanh toán.
            if (willCancel)
                await NotifyCancelledShowtimeAsync(showtime, form.Description);

            return Result.Success();
        }

        // Gửi email thông báo hủy suất + lý do cho mọi khách đã thanh toán suất đó.
        private async Task NotifyCancelledShowtimeAsync(Showtime showtime, string reason)
        {
            var paidBookings = (await _unitOfWork.Bookings.GetAllAsync(
                b => b.ShowtimeId == showtime.Id && b.PaymentStatus == "Paid" && b.UserId != null)).ToList();

            // Mỗi khách chỉ nhận 1 email dù đặt nhiều đơn.
            var userIds = paidBookings.Select(b => b.UserId!.Value).Distinct().ToList();
            if (userIds.Count == 0) return;

            var movieTitle = showtime.Movie?.Title ?? "phim";
            var roomName = showtime.Room?.Name ?? "phòng chiếu";
            var timeText = showtime.StartTime.ToString("HH:mm dd/MM/yyyy");
            var subject = $"[CineStar] Thông báo hủy suất chiếu {movieTitle}";

            foreach (var uid in userIds)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(uid);
                if (user is null || string.IsNullOrWhiteSpace(user.Email)) continue;

                var body = $@"
<p>Xin chào <strong>{user.FullName}</strong>,</p>
<p>CineStar rất tiếc phải thông báo suất chiếu bạn đã đặt vé đã bị <strong>hủy</strong>:</p>
<ul>
  <li>Phim: <strong>{movieTitle}</strong></li>
  <li>Phòng: {roomName}</li>
  <li>Thời gian: {timeText}</li>
</ul>
<p><strong>Lý do:</strong> {reason}</p>
<p>Điểm thưởng bồi thường đã được cộng vào tài khoản của bạn. Xin lỗi vì sự bất tiện này.</p>
<p>Trân trọng,<br/>CineStar</p>";

                // Một email lỗi không được làm hỏng cả quá trình khai báo sự cố.
                try { await _emailService.SendAsync(user.Email, subject, body); }
                catch { /* bỏ qua, đã hoàn điểm + ghi nhận sự cố thành công */ }
            }
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
    }
}
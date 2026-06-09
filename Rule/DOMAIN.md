# DOMAIN.md — Tham Chiếu Nhanh Domain & Database

> File hỗ trợ Claude code nhanh: liệt kê Entity và quan hệ chính. Nguồn: `Domain/Entities/`.
> Khi thêm/sửa Entity, cập nhật file này và ghi `.claude/MEM.md`.

---

## 1. Danh Sách Entity (nhóm theo nghiệp vụ)

### Người dùng & Phân quyền
- `User` — tài khoản người dùng.
- `Role` — vai trò (Admin, Staff, Member...).
- `PasswordResetToken` — token đặt lại mật khẩu.
- `RewardPointHistory` — lịch sử tích/đổi điểm thành viên.

### Phim & Lịch chiếu
- `Movie` — phim (Title, Slug, Status: "Now Showing"/"Coming Soon"...).
- `Genre` — thể loại (quan hệ n-n với `Movie`).
- `Showtime` — suất chiếu (gắn `Movie` + `Room`).
- `ShowtimeIncident` — sự cố suất chiếu.
- `Review` — đánh giá phim của người dùng.

### Rạp, Phòng & Ghế
- `Cinema` — rạp.
- `Room` — phòng chiếu (thuộc `Cinema`, có `RoomType`).
- `RoomType` — loại phòng (2D, 3D, IMAX...).
- `Seat` — ghế (thuộc `Room`, có `SeatType`).
- `SeatType` — loại ghế (thường, VIP, couple...).
- `SeatHold` — giữ ghế tạm thời (realtime seat locking).

### Đặt vé & Thanh toán
- `Booking` — đơn đặt vé.
- `Ticket` — vé (gắn `Booking`, `Seat`, `Showtime`).
- `BookingFood` — bắp nước kèm theo đơn.
- `FoodBeverage` — danh mục bắp nước.
- `Payment` — giao dịch thanh toán.
- `Promotion` — khuyến mãi/voucher.

### Cấu hình giá (linh hoạt)
- `PriceBaseConfig` — giá gốc theo phim.
- `PriceRoomTypeConfig` — phụ phí theo loại phòng.
- `PriceSeatConfig` — phụ phí theo loại ghế.
- `PriceTimeConfig` — phụ phí theo khung giờ.
- `Vat` — cấu hình thuế VAT.

### Hệ thống & Lưu vết
- `SystemConfig` — cấu hình hệ thống dạng key-value.
- `AuditLog` — nhật ký thao tác (audit trail).
- `EmailLog` — nhật ký gửi email.
- `ChatbotLog` — nhật ký hội thoại AI chatbot.

---

## 2. Quy Ước Entity

- Khóa chính: `Guid Id`.
- Thời gian: `CreatedAt`, `UpdatedAt` kiểu `DateTime?`.
- `public partial class`, navigation property khởi tạo sẵn `= new List<>()`.
- POCO thuần — không attribute EF/MVC (cấu hình map đặt trong `Infrastructure/Data`).
- `DbContext`: `CinemaWebDbContext` (`Infrastructure/Data`).

---

## 3. Khi Thêm Feature Mới — Thứ Tự Làm

1. **Domain:** thêm/sửa Entity (POCO) nếu cần.
2. **Infrastructure:** cấu hình mapping trong `CinemaWebDbContext` + Repository nếu cần; viết script `SQL/`.
3. **Application:** tạo DTO/ViewModel, Interface `I...Service`, implement Service (đi qua UnitOfWork/Repository).
4. **Presentation:** Controller gọi Service; tạo View `.cshtml` theo `color.md`.
5. Đăng ký DI trong `Program.cs`.
6. Cập nhật `.claude/MEM.md` + `DOMAIN.md` (nếu đổi entity).

# MEM.md - Shared Team Memory (Project Modification Log)

> Mỗi khi hoàn thành 1 task (thêm chức năng, sửa bug, đổi DB...), append 1 khối log
> theo format bên dưới để cả nhóm + AI đọc lại biết ai đã làm gì, vì sao.

---

### [2026-06-07] Profile - Xem hồ sơ (View User Profile) (By: dung)
- **What changed:** Thêm chức năng xem hồ sơ người dùng. Tạo `ProfileViewModel`,
  `ProfileController.Index`, view `Views/Profile/Index.cshtml` (hiển thị avatar, thông tin,
  ngày sinh + tuổi). Tính tuổi bằng computed property `Age` từ `DateOfBirth`.
- **Why:** Yêu cầu Inter 1 - màn hồ sơ nhân viên. Là màn nền để sau gắn Cập nhật/Đổi mật khẩu.
- **Impact/Notes for Team:**
  - Dùng entity + `CinemaWebDbContext` đã scaffold sẵn của nhóm (KHÔNG tự viết entity).
  - Entity `User.DateOfBirth` là kiểu `DateOnly` -> khi map sang ViewModel phải đổi
    `.ToDateTime(TimeOnly.MinValue)`.
  - View dùng Tailwind nhúng CDN ngay trong file (vì `_Layout` chung của nhóm là Bootstrap) -> KHÔNG sửa `_Layout`.
  - Chưa có Login (việc của kieudv) nên user hiện tại tạm hardcode trong
    `ProfileController.GetCurrentUserId()` = staff4 `00000000-0000-0000-0002-00000000000e`.
    Khi có Login phải đổi sang đọc Session/Claims.

### [2026-06-08] Profile - Áp dụng kiến trúc N-Tier (By: dung)
- **What changed:** Refactor chức năng hồ sơ theo N-Tier: thêm
  `IGenericRepository`/`GenericRepository`, `IUnitOfWork`/`UnitOfWork` (Infrastructure);
  `IProfileService`/`ProfileService` + `ProfileDto`/`UpdateProfileDto` (Application);
  `ProfileMappingProfile` (AutoMapper). `ProfileController` KHÔNG còn đụng `DbContext`.
- **Why:** Tuân thủ Rule/RULE.md - Controller phải gọi Service -> UnitOfWork -> Repository -> DB,
  không bypass tầng.
- **Impact/Notes for Team:**
  - Luồng chuẩn cho các module sau: Controller -> IProfileService -> IUnitOfWork ->
    IGenericRepository<T> -> CinemaWebDbContext.
  - Đã đăng ký DI trong `Program.cs`: `IUnitOfWork`, `IProfileService`, `AddAutoMapper(...)`.
  - Có thể TÁI DÙNG `IGenericRepository<T>` / `IUnitOfWork` cho mọi entity khác (Movie, Booking...).
  - Map dùng AutoMapper cho phần ĐỌC (User->Dto->ViewModel); phần CẬP NHẬT map tay để
    tránh ghi đè null lên field không muốn đổi (vd avatar).

### [2026-06-08] Profile - Cập nhật hồ sơ + Tải ảnh đại diện (By: dung)
- **What changed:** Thêm `UpdateProfileViewModel`, `ProfileController.Edit` (GET/POST),
  view `Edit.cshtml`. Cho sửa họ tên, số điện thoại; upload avatar lưu vào
  `wwwroot/uploads/avatars/` rồi lưu đường dẫn vào `User.AvatarUrl`.
- **Why:** Yêu cầu Inter 1 - màn Update Profile (gồm cả đổi ảnh đại diện).
- **Impact/Notes for Team:**
  - Form upload phải có `enctype="multipart/form-data"`.
  - Controller chỉ LƯU FILE (IO) rồi đưa đường dẫn string cho Service; Service không biết IFormFile.
  - Branch: mỗi chức năng 1 branch riêng (stacked) -> `dung-feature/profile-view` ->
    `dung-feature/edit-profile` -> `dung-feature/upload-avatar`.

### [2026-06-09] Profile - Đổi mật khẩu + BCrypt + Brand UI (By: dung)
- **What changed:** Thêm chức năng đổi mật khẩu (`ChangePasswordViewModel`,
  `ProfileController.ChangePassword`, `ChangePassword.cshtml`). Nâng bảo mật: dùng
  `BCrypt.Net-Next` để verify mật khẩu cũ (`BCrypt.Verify`) và lưu mật khẩu mới dạng hash
  (`BCrypt.HashPassword`). Đổi toàn bộ 3 view hồ sơ sang bảng màu thương hiệu
  (primary #F37021 cam, secondary #00488D, tertiary #002F59) + font Plus Jakarta Sans / Be Vietnam Pro.
- **Why:** Hoàn tất Inter 1; không lưu mật khẩu thô (chuẩn bảo mật); đồng bộ giao diện theo brand nhóm.
- **Impact/Notes for Team:**
  - `BCrypt.Verify` được bọc try/catch vì các hash mẫu trong seed KHÔNG đúng định dạng BCrypt
    (sẽ ném lỗi). User đăng ký mới sau này sẽ có hash chuẩn -> verify chạy đúng.
  - Khi làm chức năng Đăng ký / Login (kieudv), nhớ hash mật khẩu bằng `BCrypt.HashPassword`
    để khớp với logic đổi mật khẩu này.
  - Bảng màu + font khai báo bằng `tailwind.config` nhúng trong từng view (vì dùng Tailwind CDN,
    không có file config riêng). Tên màu: primary/primary-dark/secondary/tertiary/light.
  - Hoàn tất Inter 1 (dung): Xem hồ sơ, Cập nhật hồ sơ, Tải ảnh đại diện, Đổi mật khẩu.

# MEM.md - Shared Team Memory

Nhật ký thay đổi mã nguồn / CSDL / quyết định kỹ thuật của dự án.

---

### [2026-06-09] Chức năng Đăng nhập (Account Login) (By: vkieu)

- **What changed:**
  - **Backend (N-Tier + AutoMapper):**
    - *Application:* `Common/Result<T>`, `DTOs/LoginDto` + `DTOs/UserDto`, `ViewModels/LoginViewModel`, `Interfaces/IGenericRepository`/`IUserRepository`/`IUnitOfWork`/`IAuthService`, `Mappings/UserProfile` (AutoMapper map User → UserDto), `Services/AuthService`.
    - *Infrastructure:* `Repositories/GenericRepository` + `UserRepository`, `UnitOfWork/UnitOfWork`.
    - *Presentation:* `AccountController` (Login GET/POST, Logout, AccessDenied), Views `Account/Login.cshtml` + `Account/AccessDenied.cshtml`, link Đăng nhập/Đăng xuất ở `_Layout.cshtml`.
    - `Program.cs`: đăng ký AutoMapper, DI (UnitOfWork/Repository/AuthService), **Cookie Authentication** (`UseAuthentication` trước `UseAuthorization`).
    - Thêm package `AutoMapper 14.0.0`, `BCrypt.Net-Next 4.0.3`.
    - `SQL/DevResetPasswords.sql`: đặt mật khẩu `Admin@123` cho admin/manager1/staff1 (chỉ dùng dev).
  - **Giao diện (`Views/Account/Login.cshtml`):** dựng theo `demo.html` thương hiệu "CineStar" — Tailwind CDN + theme cam/xanh, Google Fonts (Plus Jakarta Sans, Be Vietnam Pro), Material Symbols, nền rạp full-screen, nút hiện/ẩn mật khẩu, nút Google (logo SVG 4 màu), link "Quên mật khẩu?"/"Đăng ký ngay" (placeholder `#`). View đặt `Layout = null` (trang độc lập, không dùng `_Layout` Bootstrap). Đã tinh chỉnh font/spacing/cỡ chữ cho cân đối.
  - **UX:** JS real-time tự ẩn lỗi khi người dùng nhập đúng (không cần submit): gõ vào ô → ẩn khối lỗi chung `#loginError`; Email đúng định dạng / Mật khẩu không trống → xoá lỗi từng ô.

- **Why:** Yêu cầu chức năng đăng nhập dựa trên cấu trúc DB (`Users.password_hash`, `Role`) và kiến trúc N-Tier sẵn có, dùng AutoMapper map Entity → DTO; xác thực bằng Cookie + Claims (kèm Role để phân quyền); giao diện bám theo file thiết kế demo.

- **Impact/Notes for Team:**
  - Mật khẩu lưu dạng băm **BCrypt** — tạo mới phải dùng `BCrypt.Net.BCrypt.HashPassword(...)`. `AuthService.VerifyPassword` bắt `SaltParseException` để bỏ qua hash seed mẫu không hợp lệ.
  - Seed gốc dùng hash GIẢ (`$2a$10$samplehash...`) → KHÔNG đăng nhập được; chạy `SQL/DevResetPasswords.sql` trước khi test (đăng nhập: `admin@cinemaweb.vn` / `Admin@123`).
  - Claims đã set `ClaimTypes.Role` → module sau dùng được `[Authorize(Roles = "ADMIN")]` ngay.
  - Trang Login KHÔNG dùng `_Layout` chung (nav/footer toàn cục không áp). Nút Google + link Đăng ký/Quên mật khẩu mới là placeholder, chưa có backend.
  - Tailwind nạp qua CDN — hợp dev/demo; production nên build Tailwind tĩnh.
  - Trong `.cshtml`, ký tự `@` của JS (regex email) phải viết `\x40` để tránh lỗi Razor `RZ1005`.
  - `AutoMapper 14.0.0` còn cảnh báo NuGet audit `NU1903` (chưa có bản vá cao hơn) — chấp nhận cho môi trường học tập.

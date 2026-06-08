# Global Agent Instructions

IMPORTANT: Claude, you are strictly required to scan, read, and follow the localized project configurations hidden in the subfolders. 

Before processing any user request, you MUST automatically call your file tools to read:
- `Rule/.claudeignore`
- `Rule/RULE.md`
- `Rule/MEM.md`
- `Rule/color.md`
- `Rule/code.md`
- `Rule/DOMAIN.md`
- `README.md`

Execute this auto-discovery workflow immediately at the start of every session to ensure you adhere to our N-Tier Architecture, Git Branching rules, and Commit Conventions.

---

# Tiến Trình Công Việc (Progress Log)

### [2026-06-08] Feature: Quản Lý Người Dùng (View / Edit / Disable / Reset Password / Assign Role)

**Trạng thái:** ✅ Hoàn tất code — ⏳ Chưa build/test (môi trường này không chạy được `dotnet build`).

**Quyết định kỹ thuật (do người dùng chọn):**
- Xây **full UnitOfWork + GenericRepository** đúng RULE.md (UserService KHÔNG inject thẳng DbContext).
- Reset mật khẩu: hệ thống **sinh mật khẩu tạm ngẫu nhiên** (12 ký tự, đủ loại), hash lưu DB, hiển thị **1 lần** cho admin qua TempData.
- Thêm package **BCrypt.Net-Next 4.0.3** để hash đúng định dạng `$2a$` như seed data.

**File đã thêm mới:**
- `Application/Interfaces/IGenericRepository.cs` — generic repo (Get/GetAll có include+orderBy, Exists, Add, Update, Remove).
- `Application/Interfaces/IUnitOfWork.cs` — expose `Users`, `Roles`, `SaveChangesAsync`.
- `Infrastructure/Repositories/GenericRepository.cs` — bản implement (read dùng `AsNoTracking`).
- `Infrastructure/UnitOfWork/UnitOfWork.cs` — lazy-init repos, dùng chung 1 DbContext.
- `Application/Common/Result.cs` — `Result` / `Result<T>` cho phản hồi service.
- `Application/DTOs/UserDTO.cs`, `Application/DTOs/RoleDTO.cs`.
- `Application/ViewModels/UserListViewModel.cs` (lọc + phân trang), `UserEditViewModel.cs` (DataAnnotations validation).
- `Application/Interfaces/IUserService.cs`, `Application/Services/UserService.cs` — toàn bộ business logic.
- `Controllers/UsersController.cs` — Index, Details, Edit (GET/POST), ToggleStatus, AssignRole, ResetPassword (POST + AntiForgeryToken).
- `Views/Users/Index.cshtml` (danh sách + lọc theo tên/email/SĐT/vai trò/trạng thái + phân trang), `Details.cshtml` (xem + reset + assign role + toggle), `Edit.cshtml` (form sửa + client validation). Style theo `color.md` (CSS variables trong site.css).

**File đã sửa:**
- `Cinema_System.csproj` — thêm `BCrypt.Net-Next` 4.0.3.
- `Program.cs` — đăng ký DI: `IUnitOfWork → UnitOfWork`, `IUserService → UserService`.

**Nghiệp vụ đã cài đặt:**
- **Xem:** danh sách (lọc + phân trang 10/trang, join Role) + trang chi tiết.
- **Sửa:** họ tên, email, SĐT, ngày sinh, vai trò — kiểm tra trùng email/SĐT, role hợp lệ.
- **Vô hiệu / kích hoạt:** đổi `Status` Active ↔ Inactive (KHÔNG xóa cứng — bảo toàn dữ liệu liên quan booking/ticket).
- **Reset mật khẩu:** sinh mật khẩu tạm, BCrypt hash, trả về hiển thị 1 lần.
- **Phân role:** dropdown đổi `RoleId` (CUSTOMER / STAFF / MANAGER / ADMIN theo seed).

**⚠️ Việc cần làm tiếp (TODO cho team):**
1. Chạy `dotnet restore` + `dotnet build` để xác nhận package BCrypt tải về và biên dịch sạch.
2. Test UI luồng: list → filter → edit → reset → toggle → assign role.
3. **Chưa có Authentication/Authorization** — controller `UsersController` hiện ai cũng truy cập được. Cần thêm `[Authorize(Roles="ADMIN")]` sau khi có module đăng nhập.
4. Cân nhắc ghi `AuditLog` cho các thao tác nhạy cảm (disable, reset password, đổi role).
5. Thêm link "Quản Lý Người Dùng" vào menu admin khi có khu vực admin.

**Lưu ý kiến trúc:** UnitOfWork/Repository giờ đã tồn tại — `MovieService` (đang inject thẳng DbContext) nên được refactor về pattern này trong lần chạm kế tiếp.
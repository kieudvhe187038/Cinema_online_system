# MEM.md - Shared Team Memory

Nhật ký thay đổi mã nguồn / CSDL / quyết định kỹ thuật của dự án.

---

### [2026-06-09] Module Tài khoản: Đăng nhập / Đăng ký OTP (By: vkieu)

- **Kiến trúc:** N-Tier đầy đủ — Application (DTO, ViewModel, Service, Interface, AutoMapper Profile), Infrastructure (GenericRepository + UserRepository, UnitOfWork, SmtpEmailService), Presentation. Map VM↔DTO và Entity→DTO bằng **AutoMapper** (`UserProfile`, `AuthMappingProfile`) — không map tay trong Controller.
- **Controllers tách riêng:** `LoginController` (Login/Logout/AccessDenied + nhớ đăng nhập) và `RegisterController` (Register/VerifyOtp/ResendOtp), cùng kế thừa `AuthControllerBase` (chứa `SignInUserAsync` dùng chung). Views ở `Views/Login/` và `Views/Register/`. Dùng **attribute routing** URL sạch: `/login`, `/logout`, `/access-denied`, `/register`, và OTP của đăng ký đặt dưới prefix `/register/verify-otp`, `/register/resend-otp` (chừa chỗ cho OTP đổi/quên mật khẩu sau này, vd `/forgot-password/verify-otp`). Đăng xuất redirect về `/login`.
- **Đăng nhập:** Cookie auth + Claims (kèm `ClaimTypes.Role` → dùng `[Authorize(Roles=...)]`). Giữ phiên **persistent 15 ngày** (qua cả tắt trình duyệt), out khi Đăng xuất hoặc hết hạn. Checkbox "Lưu thông tin đăng nhập" nhớ **email + mật khẩu** (mã hoá bằng Data Protection) vào cookie `RememberedLogin` để tự điền lần sau (độc lập thời hạn phiên). Lưu ý: ô password phải set `value` tường minh vì `asp-for` bỏ value cho type=password; lưu mật khẩu có rủi ro bảo mật. (KHÔNG khoá đăng nhập theo số lần sai.)
- **Đăng ký + OTP:** Lưu dữ liệu chờ vào **Session** (mật khẩu + OTP băm BCrypt); tạo User (role CUSTOMER, status Active) chỉ sau khi OTP đúng → tự đăng nhập. Hằng số OTP gom ở `Application/Common/OtpPolicy` (Expiry 5', ResendCooldown 5', MaxVerifyAttempts **5**) — dùng chung Service & Controller. Trang VerifyOtp **đếm ngược thời gian sống OTP**. **Gửi lại OTP: lần đầu miễn cooldown, từ lần 2 mới chờ 5'** (đếm bằng `PendingRegistration.ResendCount`/`LastSentAt`). Form đăng ký: 2 ô mật khẩu full-width có nút hiện/ẩn. Không đổi schema DB.
- **Quên mật khẩu (3 bước riêng):** `ForgotPasswordController` + `PasswordResetService` (OTP qua email, dùng chung `OtpPolicy`/`IEmailService`, phiên chờ `PendingPasswordReset` ở Session). Flow: `/forgot-password` (nhập email) → `/forgot-password/verify-otp` (xác thực OTP, set cờ `OtpVerified`) → `/forgot-password/reset` (đặt mật khẩu mới) → cập nhật `PasswordHash` (BCrypt) → `/login`. Trang `/reset` **chặn truy cập nếu `OtpVerified=false`**; gửi lại OTP reset cờ verify. `/forgot-password/resend-otp` (cooldown 5'). Email không tồn tại → báo lỗi (không gửi). KHÔNG dùng bảng `Password_Reset_Tokens` (chọn OTP+Session cho nhất quán với đăng ký). Link "Quên mật khẩu?" ở Login đã trỏ tới đây.
- **Validate input:** Mật khẩu **6–72 ký tự, regex `^[!-~]+$`** (cho chữ/số/ký tự đặc biệt ASCII như @#$; CHẶN tiếng Việt có dấu, emoji, khoảng trắng; 72 = giới hạn BCrypt 72 byte) — áp cho cả đăng ký và đặt lại mật khẩu. Họ tên chỉ chữ cái + khoảng trắng (`^[\p{L}\p{M} ]+$`). SĐT `^0\d{9,10}$`. **Ngày sinh** (`RegisterViewModel : IValidatableObject`): không tương lai, không quá 120 năm, phải đủ ≥12 tuổi. Validation server-side (các trang auth không nạp jQuery unobtrusive); input có thêm `maxlength`/`pattern` HTML5.
- **Đăng nhập Google (OAuth2):** package `Microsoft.AspNetCore.Authentication.Google`. Program.cs đăng ký Google (scheme "Google", `SignInScheme="External"` — cookie tạm) CHỈ khi có `GoogleSettings:ClientId/ClientSecret`. `LoginController.ExternalLogin` (POST `/external-login`) → `Challenge(Google)`; callback `/external-login/callback` đọc claim email/name từ scheme "External", gọi `AuthService.ExternalLoginAsync` (CHỈ tìm theo email, không tạo). **Đã có tài khoản → đăng nhập luôn. CHƯA có → lưu thông tin Google vào Session (`PendingExternalLogin`) và chuyển sang `/external-login/complete` (`CompleteProfile`) để bổ sung SĐT + ngày sinh; submit mới gọi `CompleteExternalRegistrationAsync` tạo user (CUSTOMER, PasswordHash=null) rồi đăng nhập.** Trang complete chặn nếu không có phiên. Chưa cấu hình → nút Google báo "chưa được cấu hình". Creds để trong `.env` (`GoogleSettings__ClientId/ClientSecret`); Google Console cần thêm redirect URI `http://localhost:5056/signin-google` (và bản https). **DB:** đổi `UK_Users_phone` (UNIQUE) → filtered unique index `UX_Users_phone WHERE phone IS NOT NULL` (cho phép nhiều user Google không có SĐT) — script `SQL/Migration_AllowNullPhone.sql`, DbContext cập nhật `.HasFilter`.
- **Email:** Gmail SMTP. Cấu hình không bí mật (Host/Port) ở `appsettings.json`; **secret để trong `.env`** (`EmailSettings__User/Password/FromEmail`, nạp bằng DotNetEnv, đã gitignore — xem `.env.example`). Chưa điền creds → `SmtpEmailService` log OTP ra console (dev). Gửi lỗi → trả Result.Failure (không 500).
- **Mật khẩu:** băm BCrypt. Seed gốc dùng hash GIẢ → chạy `SQL/DevResetPasswords.sql` để test (admin@cinemaweb.vn / Admin@123).
- **Giao diện:** theme CineStar (Tailwind CDN) qua layout chung `Shared/_AuthLayout.cshtml` cho Login/Register/VerifyOtp.
- **Bẫy đã xử lý:** (1) prop non-nullable trong ViewModel bị "required" ngầm → field không post sẽ làm ModelState invalid âm thầm, phải `ModelState.Remove(nameof(...))`. (2) Ký tự `@` trong JS file `.cshtml` phải viết `\x40` (tránh lỗi Razor). (3) `AutoMapper 14.0.0` còn cảnh báo audit `NU1903`. (4) Session in-memory — chạy nhiều instance cần store dùng chung.

### [2026-06-15] Fix carousel indicators not updating on slide change (By: admin)
- **What changed:** Modified carousel indicators in [Views/Home/Index.cshtml](Views/Home/Index.cshtml#L60-L65) to use Tailwind classes instead of inline styles for updating active dot state. Changed `showSlide()` function to add/remove `bg-white/80` and `bg-white/50` classes rather than setting `style.background`.
- **Why:** Inline styles couldn't override Tailwind CSS classes with `!important` flag. Now dots correctly highlight when switching slides by adding `active` class and toggling opacity classes.
- **Impact/Notes for Team:** Carousel indicators now properly update when slides change via next/prev buttons or dot clicks. Dots use Tailwind classes consistently with the initial markup.

### [2026-06-15] Fix banner responsive issue at zoom 80% (By: admin)
- **What changed:** Added `w-full h-full object-cover` classes to all banner images in [Views/Home/Index.cshtml](Views/Home/Index.cshtml#L31-L48) to ensure images fill the entire carousel container without gaps at any zoom level.
- **Why:** When zooming to 80% using Ctrl+scroll, the banner images had black/empty spaces on the sides because images weren't filling the full container width. The `object-cover` property ensures images scale to cover the container while maintaining aspect ratio.
- **Impact/Notes for Team:** Banner now displays full-width without black borders at any zoom level. All carousel slide images use consistent sizing: `w-full h-full object-cover`.

### [2026-06-14] Exclude stopped movies from filter/search and improve variable names (By: admin)
- **What changed:** Updated movie filtering/service logic to exclude movies with status `Stopped` from search results, active filters, and available status options. Refactored ambiguous variables like `vm`, `items`, `movies`, and `q` to more specific names in `MoviesController`, `HomeController`, and `MovieService`.
- **Why:** The UI should not display or allow filtering by movies that have already stopped screening. Clear variable names improve code readability and maintainability.
- **Impact/Notes for Team:** Status lists and filtered movie sets now omit `Stopped`; any future service calls should use explicit variable names for view models and paged movie collections.

### [2026-06-16] Merge module Phim (vhung) vào master — đồng bộ hạ tầng dùng chung (By: vkieu)
- **What changed:** Gộp `vhung-main` (homepage/view-list/search/filter phim) vào `master` đã có module Tài khoản. Hợp nhất các file hạ tầng dùng chung: `IGenericRepository`/`GenericRepository` lấy bản đầy đủ của vhung (thêm `GetAllAsync(predicate, includeProperties, orderBy)`, `ExistsAsync`, `CountAsync`, `FirstOrDefaultAsync(..., includeProperties)`) — tương thích ngược với các lời gọi đơn giản của module Tài khoản. `IUnitOfWork`/`UnitOfWork` giữ `IUserRepository Users` (chuyên biệt của tôi) + bổ sung toàn bộ repo của vhung (Roles, SystemConfigs, SeatTypes, Seats, PriceSeatConfigs, RoomTypes, Rooms, PriceRoomTypeConfigs, Movies, Genres) theo lối khởi tạo lazy. `Program.cs` đăng ký DI cả 2 module (Auth + `IMovieService`). `_Layout.cshtml` dùng header Tailwind CineStar mới của vhung **nhưng đấu nối nút Đăng nhập/Đăng ký/Đăng xuất vào LoginController/RegisterController thật** (thay cho nút tĩnh).
- **Why:** Hai nhánh cùng tạo mới các file hạ tầng (add/add conflict). Cần một bản hợp nhất là superset để cả tính năng tài khoản lẫn tính năng phim chạy chung.
- **Impact/Notes for Team:** **AutoMapper dùng 16.1.1** (đã bỏ gói `AutoMapper.Extensions.Microsoft.DependencyInjection` 12.0.1 của vhung — không tương thích v16, DI extension đã tích hợp sẵn). Đăng ký Profile phải theo API v16: `AddAutoMapper(cfg => cfg.AddMaps(typeof(MovieMappingProfile).Assembly))` (quét cả assembly, nạp mọi Profile) — KHÔNG dùng `AddAutoMapper(Assembly)` (đã bỏ ở v16). `IUnitOfWork.Users` là `IUserRepository` (không phải `IGenericRepository<User>`); nếu cần CRUD generic vẫn dùng được vì `IUserRepository : IGenericRepository<User>`. Filtered index `UX_Users_phone WHERE phone IS NOT NULL` của module Tài khoản vẫn giữ nguyên trong DbContext. Build pass.

### [2026-06-16] Dọn & tái cấu trúc module Phim sau merge (By: vkieu)
- **What changed:** (1) Xóa code thừa: `MovieMappingProfile` bỏ 15 `ForMember` map trùng tên (AutoMapper tự map convention), bỏ `using System.Linq` thừa, bỏ `orderBy` SQL bị `OrderBy` in-memory ghi đè. (2) Đẩy lọc/tìm kiếm **xuống SQL** thay vì `GetAllAsync()` rồi `.Where()` in-memory: `GetFilteredMoviesAsync` (predicate genre/ageRating/status, dùng `movie.Genres.Any(...)`), `GetVisibleMoviesAsync`, đã đúng cho `SearchMoviesAsync`/`GetSpecialShowtimeMoviesAsync`. (3) Thêm `Application/Common/PagedResult<T>` (có `Create(source, page, pageSize)` gom logic phân trang dùng chung) — `IMovieService.GetMoviesPageAsync`/`SearchMoviesAsync` **trả `PagedResult<MovieDTO>` thay vì `MoviesPageViewModel`**; `MoviesController.BuildViewModel` mới map sang ViewModel (đúng N-Tier: Service trả DTO, Presentation tạo ViewModel). (4) Gom hằng số vào `Application/Common/MovieConstants.cs`: `MovieStatus` (NowShowing/ComingSoon/StoppedLower), `ShowtimeStatus`, `AgeRatingPolicy.DisplayOrder`, `MoviePaging.DefaultPageSize` (=3, thay `pageSize=3` rải rác ở Controller).
- **Why:** 4 vấn đề chất lượng do review phát hiện sau merge: in-memory filtering tốn DB, trùng logic phân trang, Service rò rỉ ViewModel (sai tầng), hằng số (status/pageSize/age order) rải rác nhiều nơi.
- **Impact/Notes for Team:** Service phim giờ trả `PagedResult<MovieDTO>` — ai gọi phải tự map sang ViewModel ở Controller (xem `MoviesController.BuildViewModel`). Đặt phân trang/loại phim mới: thêm trạng thái vào `MovieConstants`, đừng hardcode chuỗi. **CẦN smoke-test với DB thật**: các predicate EF mới (đặc biệt `movie.Genres.Any(...)` + `string.IsNullOrWhiteSpace(bienCaptured)` trong filter) — pattern đã dùng OK ở `GetSpecialShowtimeMoviesAsync` nên rủi ro thấp, nhưng nên chạy thử trang lọc/tìm kiếm. 2 method `GetAllMoviesAsync`/`GetMovieByIdAsync` vẫn chưa có nơi gọi (scaffolding cho trang chi tiết) — giữ lại.

### [2026-06-16] Bố cục lại trang Home & Movies, gom card phim (By: vkieu)
- **What changed:** (1) **Chuyển bộ lọc phim từ Home sang Movies**, lọc **trong tab đang xem** (now/coming/special). Chỉ giữ lọc **Thể loại + Độ tuổi** (bỏ lọc Trạng thái vì 3 tab đã là bộ chọn trạng thái). `GetMoviesPageAsync(tab, page, pageSize, genre?, ageRating?)` lấy entity theo tab (kèm `Genres`+`Showtimes` qua `GetTabMoviesAsync`) rồi lọc thể loại/độ tuổi **trong bộ nhớ** (tập tab nhỏ; thể loại cần navigation `Genres` nên không đẩy qua DTO được). `HomeController`/`HomeViewModel` bỏ sạch logic lọc. (2) **Tách partial `Views/Shared/_MovieCard.cshtml`** (nhận `MovieDTO` + badge tùy chọn qua `ViewData["Badge"]`/`["BadgeClass"]`) — thay 4 khối card lặp ở Home + grid ở Movies. (3) **Home**: 3 danh sách thành **slider cuộn ngang 4 phim/hàng** (flex `overflow-x-auto` + nút trước/sau, JS `[data-slider]`); local function Razor `MovieSliderAsync` dựng chung. (4) **Movies**: lưới **4 cột** (`xl:grid-cols-4`), **8 phim/trang** (`MoviePaging.DefaultPageSize` 3→8), **bỏ header "Danh sách phim"**, chỉ còn 3 tab **căn giữa**; phân trang tab giữ `genre`/`ageRating` trên link. (5) **Footer** (`_Layout`): bỏ cột "Chấp Nhận Thanh Toán" + "Tải Ứng Dụng", grid 4→3 cột.
- **Why:** Yêu cầu chỉnh UX: gom lọc về đúng màn danh sách phim, Home dạng slider gọn, Movies phân trang rõ ràng, footer bớt rườm rà.
- **Impact/Notes for Team:** Thêm `@using Microsoft.AspNetCore.Mvc.ViewFeatures` vào `_ViewImports` (để dùng `ViewDataDictionary` truyền badge cho partial). Badge card truyền qua `ViewData`, KHÔNG sửa trực tiếp ViewData gốc — tạo `new ViewDataDictionary(ViewData)` mỗi card. `MoviePaging.DefaultPageSize` giờ = **8** (chỉ Movies dùng; Home là slider không phân trang). Hai method service `GetFilteredMoviesAsync`/`GetAllMovieStatusesAsync` giờ **không còn nơi gọi** (lọc đã đổi sang trong-tab) — tạm giữ, có thể dọn sau. Cần smoke-test trang lọc trong tab với DB thật.

---

## Nhật ký module Hồ sơ (gộp từ `dung-main` — giữ lại để tham khảo lịch sử)

### [2026-06-07] Profile - Xem hồ sơ (View User Profile) (By: dung)
- **What changed:** Thêm chức năng xem hồ sơ người dùng (`ProfileViewModel`, `ProfileController.Index`, `Views/Profile/Index.cshtml`). Tính tuổi bằng computed property `Age` từ `DateOfBirth`.
- **Why:** Màn hồ sơ — nền để gắn Cập nhật/Đổi mật khẩu.
- **Impact/Notes for Team:** `User.DateOfBirth` là `DateOnly` → map sang ViewModel phải `.ToDateTime(TimeOnly.MinValue)`. Lúc này chưa có Login nên user tạm hardcode (xem mục merge bên dưới — đã đổi sang Claims).

### [2026-06-08] Profile - Áp dụng kiến trúc N-Tier (By: dung)
- **What changed:** Refactor hồ sơ theo N-Tier: `IGenericRepository`/`GenericRepository`, `IUnitOfWork`/`UnitOfWork`, `IProfileService`/`ProfileService` + DTO, `ProfileMappingProfile`. Controller không còn đụng `DbContext`.
- **Why:** Tuân thủ RULE.md — Controller → Service → UnitOfWork → Repository → DB.
- **Impact/Notes for Team:** Map AutoMapper cho phần ĐỌC; phần CẬP NHẬT map tay để tránh ghi đè null. (Lưu ý: bản infra này đã được thay bằng infra của master khi merge — xem mục merge.)

### [2026-06-08] Profile - Cập nhật hồ sơ + Tải ảnh đại diện (By: dung)
- **What changed:** `UpdateProfileViewModel`, `ProfileController.Edit` (GET/POST), `Edit.cshtml`. Sửa họ tên, SĐT; upload avatar vào `wwwroot/uploads/avatars/`, lưu đường dẫn vào `User.AvatarUrl`.
- **Why:** Màn Update Profile (gồm đổi ảnh đại diện).
- **Impact/Notes for Team:** Form upload cần `enctype="multipart/form-data"`. Controller chỉ LƯU FILE rồi đưa đường dẫn string cho Service; Service không biết `IFormFile`.

### [2026-06-09] Profile - Đổi mật khẩu + BCrypt + Brand UI (By: dung)
- **What changed:** Đổi mật khẩu (`ChangePasswordViewModel`, `ProfileController.ChangePassword`, `ChangePassword.cshtml`) dùng `BCrypt.Verify`/`HashPassword`. 3 view hồ sơ theo bảng màu brand (primary #F37021, secondary #00488D, tertiary #002F59).
- **Why:** Không lưu mật khẩu thô; đồng bộ giao diện brand.
- **Impact/Notes for Team:** `BCrypt.Verify` bọc try/catch vì hash seed mẫu sai định dạng. Trùng hướng với module Tài khoản (vkieu cũng dùng BCrypt).

### [2026-06-10] Profile - Xem lịch sử điểm + Validate SĐT (By: dung)
- **What changed:** **Point History**: `PointHistoryViewModel`/`PointHistoryDto`, `GetPointHistoryAsync`, map `RewardPointHistory`→DTO, `ProfileController.PointHistory`, `PointHistory.cshtml`. Validate SĐT `[Required]` + `^(0\d{9})$`.
- **Why:** Hoàn thiện Inter 1; chặn SĐT sai.
- **Impact/Notes for Team:** Entity điểm thưởng: `RewardPointHistory` (`PointsChanged`/`ActionType`/`Description`/`CreatedAt`).

### [2026-06-16] Merge module Hồ sơ (dung) vào master — đồng bộ infra & xác thực (By: vkieu)
- **What changed:** Gộp `dung-main` (xem/sửa hồ sơ, upload avatar, đổi mật khẩu, lịch sử điểm) vào `master`. **Bỏ bản infra riêng của dung** (`IGenericRepository`/`GenericRepository`/`IUnitOfWork`/`UnitOfWork` kiểu `Repository<T>()` + `params Expression includes`) — **giữ infra của master** (named repos + `IUserRepository Users`, `IGenericRepository` dùng `string[] includeProperties`/`orderBy`). Thêm `IGenericRepository<RewardPointHistory> RewardPointHistories` vào `IUnitOfWork`/`UnitOfWork`. **Viết lại `ProfileService`** dùng `_unitOfWork.Users` + `RewardPointHistories` (đẩy `OrderByDescending(CreatedAt)` xuống SQL qua `orderBy`). **`ProfileController`**: bỏ user hardcode → đọc `ClaimTypes.NameIdentifier` từ Claims + gắn `[Authorize]`; phân trang lịch sử điểm dùng chung `PagedResult<T>`. Tất cả file C# module hồ sơ đổi sang **file-scoped namespace** cho đồng bộ. `Program.cs` thêm DI `IProfileService`. Header `_Layout` đấu link "Xin chào, {tên}" → trang Hồ sơ; sidebar `_CineStarLayout` đấu nối Hồ sơ/Lịch sử điểm/Đổi mật khẩu + nút Đăng xuất thật.
- **Why:** Hai nhánh cùng tạo mới các file hạ tầng (add/add conflict) với API khác nhau; cần một bản infra thống nhất (chọn của master) và đưa module hồ sơ chạy trên đó. dung viết khi chưa có Login nên phải đấu nối xác thực thật.
- **Impact/Notes for Team:** **Giữ AutoMapper 16.1.1** (bỏ gói `AutoMapper.Extensions.Microsoft.DependencyInjection 12.0.1` của dung — không tương thích v16). Thêm package **`System.Drawing.Common 8.0.26`** (đọc kích thước pixel avatar); kiểm tra pixel bọc trong `OperatingSystem.IsWindowsVersionAtLeast(6, 1)` để tránh cảnh báo CA1416, vẫn chặn dung lượng 2MB ở mọi nền tảng. `ProfileMappingProfile` tự nạp nhờ `AddAutoMapper(cfg => cfg.AddMaps(...assembly))`. Build pass 0 warning/0 error. **Cần smoke-test với DB thật**: đăng nhập rồi vào `/Profile` (xem/sửa hồ sơ, upload avatar, đổi mật khẩu, lịch sử điểm).

### [2026-06-16] Dọn 2 method phim mồ côi (By: vkieu)
- **What changed:** Bỏ `GetFilteredMoviesAsync(genre, ageRating, status)` và `GetAllMovieStatusesAsync()` khỏi `IMovieService` + `MovieService` (không còn nơi gọi sau khi đổi lọc sang trong-tab ở bản trước). Giữ `GetVisibleMoviesAsync` (private) vì `GetAllAgeRatingsAsync` vẫn dùng; giữ `MovieStatus`/`MoviePaging`/`AgeRatingPolicy` (vẫn dùng nhiều nơi).
- **Why:** Code chết — đã xác nhận bằng grep toàn solution (chỉ còn khai báo, 0 caller, không view nào gọi).
- **Impact/Notes for Team:** Nếu sau này cần lọc theo trạng thái tự do, dựng lại từ pattern `GetTabMoviesAsync`. `GetAllMoviesAsync`/`GetMovieByIdAsync` vẫn giữ (scaffolding trang chi tiết). Build pass 0/0.
### [2026-06-14] Phân bố Controller & View theo role-folder (Inter1 taidt) (By: taidt)
- **What changed:** Di chuyển các controller + view nghiệp vụ Inter1 từ root vào đúng thư mục role bằng `git mv` (giữ lịch sử):
  - `Controllers/PointsController.cs`, `RoomTypesController.cs`, `SeatsTypeController.cs` → `Controllers/Manager/` (namespace `Cinema_System.Controllers.Manager`).
  - `Controllers/UsersController.cs` → `Controllers/Admin/` (namespace `Cinema_System.Controllers.Admin`).
  - `Views/{Points,RoomTypes,SeatsType}/*` → `Views/Manager/...`; `Views/Users/*` → `Views/Admin/Users/`.
  - Xóa `.gitkeep` thừa ở các folder Controllers/Views Admin & Manager (đã có file thật).
- **Why:** Đồng bộ với hạ tầng team đã dựng sẵn (`Helpers/SubfolderViewLocationExpander` resolve view ở `/Views/Manager/{controller}/` và `/Views/Admin/{controller}/`, cùng các folder placeholder). Trước đó code nằm ở root, làm folder role rỗng và expander vô nghĩa.
- **Impact/Notes for Team:**
  - Controller subfolder dùng namespace lồng theo folder (`...Controllers.Manager` / `...Controllers.Admin`). Controller public (Home, Movies) giữ ở root với namespace `Cinema_System.Controllers`.
  - Routing KHÔNG đổi: các controller này dùng `[Route("Manager/...")]` / `[Route("Admin/...")]` tường minh + tên controller, không phụ thuộc namespace/folder. Tên class giữ nguyên (`PointsController`, `RoomTypesController`, `SeatsTypeController`, `UsersController`).
  - View của controller trong folder role PHẢI đặt tại `Views/Manager/{Controller}/` hoặc `Views/Admin/{Controller}/` (expander tìm ở đó trước). Quy ước này áp dụng cho mọi controller Manager/Admin về sau (gồm task Staff của Inter2 — nên cân nhắc thêm folder `Controllers/Staff` + `Views/Staff`).
  - Build OK (0 error). Không đụng tới Service/DI/DbContext.

### [2026-06-15] Đổi tên Controller theo tiền tố area (By: taidt)
- **What changed:** Đổi tên 4 controller role-folder sang dạng `<Area><Domain>Controller` (số ít) bằng `git mv` (giữ lịch sử), kèm đổi tên folder View và route cho khớp:
  - `PointsController` → `ManagerPointController` (route giữ `Manager/PointSetting`; view → `Views/Manager/ManagerPoint/`).
  - `RoomTypesController` → `ManagerRoomTypeController` (route `Manager/RoomType`; view → `Views/Manager/ManagerRoomType/`).
  - `SeatsTypeController` → `ManagerSeatTypeController` (route `Manager/SeatType`; view → `Views/Manager/ManagerSeatType/`).
  - `UsersController` → `AdminUserController` (route `Admin/User`; view → `Views/Admin/AdminUser/`).
  - Cập nhật toàn bộ `@Url.Action(..., "<tên-controller-mới>")` trong các view tương ứng.
- **Why:** Tên class tự mô tả area (nhìn là biết Manager/Admin) → dễ quản lý khi số controller tăng. Đổi `[Route("Manager/[controller]")]` sang segment tường minh (`Manager/RoomType`...) để URL không bị lặp `Manager/ManagerRoomType`.
- **Impact/Notes for Team:**
  - Quy ước đặt tên controller role từ nay: `Manager<Domain>Controller`, `Admin<Domain>Controller` (số ít, ví dụ `ManagerShowtimeController`). Controller public (Home, Movies) KHÔNG thêm tiền tố.
  - Tên folder View = tên controller (bỏ hậu tố `Controller`): vd `Views/Manager/ManagerRoomType/`. Khi tham chiếu bằng `Url.Action`/`asp-controller` phải dùng đúng tên mới.
  - URL công khai đã đổi: `/Manager/RoomTypes` → `/Manager/RoomType`, `/Admin/Users` → `/Admin/User` (cập nhật nếu có bookmark/test).
  - Build OK (0 error).

### [2026-06-16] Tổ chức toàn bộ Controller & View theo role (By: vkieu)
- **What changed:** Gom TẤT CẢ controller/view vào folder theo role (mở rộng quy ước Admin/Manager của taidt cho cả public/auth/customer), dùng `git mv` (giữ lịch sử):
  - `Public/` ← Home, Movies. `Auth/` ← Login, Register, ForgotPassword, AuthControllerBase. `Customer/` ← Profile.
  - `Manager/` ← MovieManagement, FoodBeverages (gộp cùng ManagerPoint/RoomType/SeatType). `Admin/` ← AdminUser.
  - View đồng bộ sang `Views/<Role>/<Controller>/`; namespace controller lồng `Cinema_System.Controllers.<Role>`.
  - Mở rộng `SubfolderViewLocationExpander` thêm vị trí `/Views/Public|Auth|Customer/{controller}/`.
  - Sửa tham chiếu `Cinema_System.Controllers.AuthControllerBase` → `...Controllers.Auth.AuthControllerBase` trong `_Layout.cshtml`.
- **Why:** Một quy ước nhất quán cho mọi module, không còn lẫn controller ở root với controller trong folder role.
- **Impact/Notes for Team:**
  - Thêm controller mới: đặt vào `Controllers/<Role>/`, namespace `...Controllers.<Role>`, view ở `Views/<Role>/<Controller>/`. Role mới phải thêm vị trí vào expander.
  - Routing KHÔNG đổi (controller dùng tên + `[Route]` tường minh / routing quy ước). Tên class giữ nguyên.
  - Cập nhật `Rule/code.md` mục 1 (Solution Layout) + mục 8 (quy ước role-folder).
  - Build OK (0/0).

### [2026-06-17] Đồng bộ phông chữ toàn bộ view (By: vkieu)
- **What changed:** Chuẩn hoá **bộ trọng số (weights)** của Google Fonts về một bộ chung cho mọi layout/view: `Plus Jakarta Sans 400;500;600;700;800` + `Be Vietnam Pro 300;400;500;600;700` (đúng bộ `_Layout`/`_ManagerLayout` đang dùng). Sửa link font ở `_AuthLayout` (thêm BVP 300), `_CineStarLayout` (mở rộng cả 2, link dùng `@@` do Razor), và 3 view Profile tự nạp font: `ChangePassword.cshtml`, `Edit.cshtml`, `PointHistory.cshtml`.
- **Why:** Cả app vốn đã dùng chung 2 typeface (PJS heading + BVP body) nhưng mỗi nơi nạp một dải weight khác nhau → cùng độ đậm chữ render lệch nhau (vd trang dùng `_CineStarLayout` thiếu BVP 300/700 nên bị giả đậm/giả nhạt).
- **Impact/Notes for Team:** Khi thêm layout/trang tự nạp font, dùng đúng link chuẩn này. KHÔNG đổi tên token `fontFamily` trong từng layout (`heading`/`head`/`headline-md`...) — chúng là hệ token riêng, nhiều class phụ thuộc; chỉ typeface + weights được đồng bộ. 3 view Profile (Edit/PointHistory/ChangePassword) vẫn tự nạp Tailwind CDN + config trong body dù kế thừa `_Layout` (cấu hình màu trùng `_Layout` nên vô hại) — redundancy này để dọn sau, không thuộc phạm vi lần này. Build pass 0/0.

### [2026-06-17] Đồng bộ validate + nút hiện/ẩn cho Đổi mật khẩu (By: vkieu)
- **What changed:** (1) `ChangePasswordViewModel.NewPassword` đổi từ `StringLength(100, MinimumLength=6)` (không regex) → **`StringLength(72, MinimumLength=6)` + regex `^[!-~]+$`** cho khớp chuẩn dự án (đăng ký/đặt lại mật khẩu). (2) `Views/Customer/Profile/ChangePassword.cshtml`: thêm **nút hiện/ẩn mật khẩu** cho cả 3 ô (icon con mắt **SVG inline** vì trang Profile dùng Tailwind CDN, KHÔNG nạp font Material Symbols như các trang Auth) + script `.toggle-pw`; thêm `minlength/maxlength/pattern[!-~]{6,72}`/`title`/`autocomplete` HTML5 cho ô mật khẩu mới.
- **Why:** Validate đổi mật khẩu lỏng hơn phần còn lại của hệ thống — cho >72 ký tự (BCrypt cắt 72 byte) và không chặn tiếng Việt/khoảng trắng/emoji. Form chưa có hiện/ẩn mật khẩu như form đăng ký.
- **Impact/Notes for Team:** Trang Profile (Customer) tự nạp Tailwind CDN riêng + không có Material Symbols → dùng SVG cho icon, đừng copy `material-symbols-outlined` từ form Auth sang đây. Logic Controller/Service đổi mật khẩu giữ nguyên (verify mật khẩu cũ, chặn trùng cũ, hash BCrypt). Build pass 0/0.

### [2026-06-17] Bỏ bảng Password_Reset_Tokens + thêm trạng thái phim "Special" (By: vkieu)
- **What changed:**
  - **DB — gỡ `Password_Reset_Tokens`:** Xóa `CREATE TABLE [Password_Reset_Tokens]`, FK `FK_PasswordReset_Users`, dòng comment (5) và phần seed/đếm bản ghi trong `SQL/CinemaWebDB_v2.sql` + `SQL/CinemaWebDB_SeedData_Large.sql`. Xóa entity `Domain/Entities/PasswordResetToken.cs`, bỏ navigation `User.PasswordResetTokens`, bỏ `DbSet<PasswordResetToken>` + cấu hình `modelBuilder.Entity<PasswordResetToken>` trong `CinemaWebDbContext`. Bỏ dòng entity trong `Rule/DOMAIN.md`.
  - **Phim — thêm status `Special`:** Thêm `'Special'` vào `CK_Movies_status` (`SQL/CinemaWebDB_v2.sql`). Thêm `MovieStatus.Special = "Special"` và **xóa class `ShowtimeStatus`** (đã chết). Tab "special" và danh sách đặc biệt giờ lọc theo `movie.Status == MovieStatus.Special` (thay vì dò qua `Showtimes.Status` — vốn không khớp vì `CK_Showtimes_status` không cho giá trị "Special"). Đổi tên `GetSpecialShowtimeMoviesAsync`→`GetSpecialMoviesAsync` và VM `HomeViewModel.SpecialShowtimeMovies`→`SpecialMovies` (cập nhật `IMovieService`, `HomeController`, `Views/Public/Home/Index.cshtml`). Thêm option "Suất chiếu đặc biệt" vào form Create/Edit + bộ lọc & badge tím trong `Views/Manager/MovieManagement/Index.cshtml`. Seed: đặt 2 phim sang `Special` để demo.
- **Why:** Forgot-password đã chuyển hẳn sang OTP+Session (xem log 2026-06-09), bảng token không còn ai dùng → gỡ cho gọn. Trạng thái "Special" trước đây suy ra từ suất chiếu nhưng DB không cho phép status suất chiếu "Special" nên danh sách luôn rỗng; nâng "Special" thành trạng thái cấp phim cho đúng và quản lý được.
- **Impact/Notes for Team:** `Special` là một **giá trị status loại trừ** (phim Special sẽ KHÔNG xuất hiện ở tab Đang chiếu/Sắp chiếu). `MovieStatus` hợp lệ: `Now Showing` / `Coming Soon` / `Special` / `Stopped`. `ToggleStatusAsync` vẫn chỉ chuyển Stopped↔Now Showing (gạt 1 phim Special sẽ thành Stopped). **Phải chạy lại script DB** (`CinemaWebDB_v2.sql` + seed) để có constraint & dữ liệu mới. Build pass 0/0.

### [2026-06-21] Staff - Sơ đồ ghế: trang báo "Không tìm thấy phòng" thân thiện (By: dung)
- **What changed:** `RoomManagementController.Seats(Guid roomId)`: khi `GetRoomSeatsAsync` trả `null` (roomId không tồn tại, vd sửa URL tay), thay `return NotFound("...")` bằng **`return View("RoomNotFound", vm)`** — vẫn set `Response.StatusCode = 404` cho đúng chuẩn HTTP nhưng render trong `_StaffLayout`. View mới `Views/Staff/RoomManagement/RoomNotFound.cshtml` (nhận `RoomSeatsViewModel`) vẫn nạp `AllRooms` để cột trái chọn phòng khác + nút "Về danh sách phòng".
- **Why:** `NotFound("text")` nhả ra response 404 plain-text → trình duyệt tự vẽ trang lỗi trần (màn trắng/đen), không qua layout, trải nghiệm xấu.
- **Impact/Notes for Team:** Mẫu xử lý "không tìm thấy" cho các trang Staff/Manager: trả View báo lỗi trong layout + set `Response.StatusCode` thủ công, đừng dùng `NotFound()` nếu muốn giữ giao diện. Cần `using Microsoft.AspNetCore.Http;` (cho `StatusCodes`). Build pass 0/0.

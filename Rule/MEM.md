# MEM.md — Shared Team Memory

> Nhật ký thay đổi code/DB/quyết định kỹ thuật. Append entry mới ở cuối, không xóa lịch sử.

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

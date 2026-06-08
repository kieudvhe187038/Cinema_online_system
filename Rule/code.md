# code.md — Quy Tắc Viết Code (Cinema_System)

> Bổ sung cho `RULE.md`. Tập trung vào quy ước code cụ thể để code nhất quán, đúng kiến trúc N-Tier.
> Tech stack: .NET 8 / ASP.NET Core MVC + EF Core + SQL Server + Tailwind.

---

## 1. Solution Layout (thực tế)

```text
Cinema_System/                  (solution root, chứa Cinema_System.sln)
└── Cinema_System/              (project)
    ├── Application/   Common, DTOs, Interfaces, Mappings, Services, ViewModels
    ├── Domain/        Entities (POCO thuần)
    ├── Infrastructure/ Data (DbContext), Repositories, UnitOfWork
    ├── Controllers/
    ├── Views/         Home, Movies, Shared
    ├── wwwroot/       css, js, images, lib
    ├── Program.cs
    └── appsettings.json
```

**Namespace gốc:** `Cinema_System.<Layer>...` (ví dụ `Cinema_System.Application.Services`).
Dùng **file-scoped namespace** (`namespace Cinema_System.Application.Services;`) — theo code hiện có.

---

## 2. Quy Tắc Theo Tầng (Layer)

### Controllers
- Chỉ nhận request, gọi Service, trả `View`/`Redirect`.
- **KHÔNG** inject `DbContext`, **KHÔNG** chứa LINQ truy vấn DB, **KHÔNG** business logic.
- Inject Service qua interface trong constructor.

### Application/Services
- Chứa toàn bộ business logic, async (`...Async`).
- Trả về **DTO/ViewModel**, không trả `Entity` ra ngoài tầng này.
- ⚠️ **Lưu ý kỹ thuật nợ hiện tại:** `MovieService` đang inject thẳng `CinemaWebDbContext`. Theo `RULE.md`, hướng đúng là Service → `IUnitOfWork`/`IGenericRepository` → DB. Code **mới** phải đi qua UnitOfWork/Repository, không lặp lại pattern truy cập DbContext trực tiếp.

### Domain/Entities
- POCO tuyệt đối: KHÔNG dùng EF Core attribute, AutoMapper, MVC.
- Khai báo `public partial class` (để dành chỗ cho cấu hình mở rộng), navigation property khởi tạo `= new List<>()`.
- Khóa chính dùng `Guid Id`.

### Infrastructure
- `DbContext` (`CinemaWebDbContext`) + cấu hình EF Core đặt ở `Infrastructure/Data`.
- Repository implement `IGenericRepository<T>`; `UnitOfWork` quản lý transaction.
- KHÔNG cho tầng khác tham chiếu ngược vào MVC.

**Luồng bắt buộc:** `Controller → Service → UnitOfWork/Repository → DbContext`. Không nhảy tầng.

---

## 3. Quy Ước Đặt Tên (C#)

| Loại | Quy ước | Ví dụ |
| ---- | ------- | ----- |
| Class / Method / Property | PascalCase | `MovieService`, `GetAllMoviesAsync` |
| Interface | `I` + PascalCase | `IMovieService`, `IUnitOfWork` |
| Field private | `_camelCase` | `_context`, `_unitOfWork` |
| Tham số / biến local | camelCase | `movieId`, `nowShowing` |
| Hằng số | PascalCase | `MaxSeatHold` |
| Method async | hậu tố `Async` | `GetMovieByIdAsync` |
| DTO | hậu tố `DTO` | `MovieDTO` |
| ViewModel | hậu tố `ViewModel` | `HomeViewModel` |

- Mỗi class một file, tên file = tên class.
- Ưu tiên `async/await` end-to-end cho mọi truy cập DB; trả `Task`/`Task<T>`.

---

## 4. Quy Tắc Razor View (.cshtml)

- Chỉ render UI, **không** business logic / truy vấn DB trong view.
- Dùng strongly-typed model: `@model Cinema_System.Application.ViewModels.HomeViewModel`.
- Điều hướng bằng Tag Helpers: `asp-controller`, `asp-action`, `asp-area` (không hardcode URL).
- Layout chung: `Views/Shared/_Layout.cshtml`. Style theo `color.md` + `wwwroot/css/site.css`.
- Tận dụng `RenderSectionAsync("Styles"/"Scripts", required: false)` cho asset riêng từng trang.

---

## 5. EF Core & Truy Vấn

- Truy vấn read-only nên dùng `.AsNoTracking()` khi không cần update.
- Map Entity → DTO ngay trong `.Select(...)` để chỉ kéo cột cần thiết (như `MovieService.MapToDTO`).
- Không gọi `.ToList()`/`.Result` đồng bộ trên truy vấn DB — luôn `await ...Async()`.
- Mọi thay đổi schema phải kèm script trong thư mục `SQL/` và ghi log vào `.claude/MEM.md`.

---

## 6. Bảo Mật & Cấu Hình

- Connection string / secret để trong `appsettings.Development.json` hoặc user-secrets — **KHÔNG commit** (đã có trong `.gitignore`).
- Validate input ở biên (model binding + `ModelState.IsValid`), tránh SQL injection (EF tham số hóa sẵn — không nối chuỗi LINQ thủ công).
- Không log thông tin nhạy cảm (mật khẩu, token) ra `EmailLog`/`AuditLog`.

---

## 7. Sạch Sẽ Trước Khi Commit / PR

- [ ] Không còn `// TODO`, code tạm, hay `Console.WriteLine` debug.
- [ ] Build thành công, không warning vô lý, không lỗi compile.
- [ ] Đúng luồng tầng, không bypass.
- [ ] Commit theo format `<type>(<ten-nguoi>): <description>` (xem `RULE.md`).
- [ ] Đã cập nhật `.claude/MEM.md` nếu đổi DB/feature/bugfix.
- [ ] Không commit `bin/`, `obj/`, `.vs/`, file cấu hình cá nhân.

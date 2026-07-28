# Dữ liệu test — Phim Điện Ảnh Doraemon: Nobita và Lâu Đài Dưới Đáy Biển (Phiên bản mới)

Bộ dữ liệu mẫu để test chức năng **Quản lý phim** (`Manager → Thêm phim / Sửa phim`).
Các trường bám đúng `MovieFormViewModel` và ràng buộc hiện có trong `MovieManagementController`.

> **Lưu ý về độ chính xác:** đây là **dữ liệu test**, không phải nguồn thông tin chính thức.
> Phim gốc là *Nobita và Lâu Đài Dưới Đáy Biển* (のび太の海底鬼岩城, 1983) — bản dưới đây mô phỏng
> một phiên bản làm lại. Các trường **Đạo diễn**, **Thời lượng**, **Ngày khởi chiếu**, **Trailer URL**
> cần kiểm tra lại với nguồn phát hành thật trước khi đưa vào `SQL/CinemaWebDB_SeedData.sql`.

---

## 1. Dữ liệu nhập vào form "Thêm Phim Mới"

| Trường trên form | Giá trị nhập | Ghi chú ràng buộc |
| --- | --- | --- |
| **Tên phim** | `Phim Điện Ảnh Doraemon: Nobita và Lâu Đài Dưới Đáy Biển (Phiên bản mới)` | Bắt buộc, tối đa 255 ký tự (giá trị này 71 ký tự) |
| **Trạng thái** | *(không nhập)* | Hệ thống tự suy ra từ ngày khởi chiếu — xem mục 4 |
| **Giới hạn độ tuổi** | `P` | Chỉ nhận `P` / `C13` / `C16` / `C18` |
| **Thời lượng (phút)** | `96` | Range 1–600 |
| **Ngày khởi chiếu** | `2026-08-14` | Form Thêm chặn ngày quá khứ (hôm nay `2026-07-28`) |
| **Ngôn ngữ** | `Tiếng Nhật` | Tối đa 100 ký tự |
| **Phụ đề** | `Tiếng Việt` | Tối đa 100 ký tự |
| **Đạo diễn** | `Tsutomu Shibayama` | Tối đa 255 ký tự — *tên đạo diễn bản 1983, dùng tạm để test* |
| **Diễn viên** | xem mục 2 | Tối đa 2000 ký tự |
| **Mô tả** | xem mục 2 | Tối đa 3000 ký tự (ô có counter) |
| **Poster (ảnh)** | file ảnh bất kỳ ≤ 2MB | `.jpg .jpeg .png .webp .gif` |
| **Banner (ảnh)** | file ảnh bất kỳ ≤ 2MB | Cùng ràng buộc như poster |
| **Trailer URL** | xem mục 3 | Phải là URL hợp lệ, tối đa 500 ký tự |
| **Thể loại** | `Hoạt Hình`, `Phiêu Lưu`, `Gia Đình` | Tick 3 checkbox |
| **Slug (URL)** | *(để trống)* | Để trống sẽ tự sinh — xem mục 5 |

---

## 2. Nội dung text (copy trực tiếp)

**Tên phim**

```text
Phim Điện Ảnh Doraemon: Nobita và Lâu Đài Dưới Đáy Biển (Phiên bản mới)
```

**Diễn viên (lồng tiếng Nhật)**

```text
Lồng tiếng: Wasabi Mizuta, Megumi Ohara, Yumi Kakazu, Subaru Kimura, Tomokazu Seki
```

**Mô tả** *(486 ký tự — nằm trong giới hạn 3000)*

```text
Nghỉ hè, Nobita cùng Doraemon và nhóm bạn dùng bảo bối biến nước biển thành không khí để mở một chuyến cắm trại dưới đáy đại dương. Chuyến đi tưởng chừng vô tư lại đưa cả nhóm lạc vào vương quốc Mu cổ xưa và tòa lâu đài Quỷ dưới đáy biển, nơi một hệ thống vũ khí tự động còn sót lại từ nền văn minh đã diệt vong vẫn đang âm thầm chờ ngày kích hoạt. Giữa lằn ranh sinh tử, tình bạn và sự hy sinh của những người đồng hành trở thành thứ duy nhất có thể ngăn thảm họa nhấn chìm cả thế giới.
```

---

## 3. Trailer URL

Trường này có `[Url]` validation, chỉ cần đúng định dạng URL là qua được validate.
Nhưng **hãy dán link YouTube thật** nếu muốn kiểm tra phần nhúng trailer ở trang chi tiết phim —
ID video giả sẽ hiển thị "Video unavailable" (đúng lỗi đã từng sửa ở commit `b5daf20`).

```text
https://www.youtube.com/watch?v=<ID_VIDEO_THAT>
```

- Lấy ID thật từ kênh chính thức của Doraemon rồi thay vào `<ID_VIDEO_THAT>`.
- Muốn test riêng validation: dán `khong-phai-url` → phải báo *"Trailer phải là một URL hợp lệ"*.

---

## 4. Test trạng thái tự động (`MovieStatusPolicy`)

Manager **không** chọn trạng thái tay. Đổi ngày khởi chiếu để tạo từng trạng thái:

| Muốn ra trạng thái | Ngày khởi chiếu | Điều kiện thêm | Nơi test |
| --- | --- | --- | --- |
| **Sắp chiếu** (`Coming Soon`) | `2026-08-14` (tương lai) | Chưa xếp suất chiếu nào trước ngày đó | Form Thêm |
| **Chiếu sớm** (`Special`) | `2026-08-14` | Xếp 1 suất chiếu vào `2026-08-05` (trước ngày khởi chiếu) | Thêm phim xong → Quản lý suất chiếu |
| **Đang chiếu** (`Now Showing`) | `2026-07-28` (hôm nay) | — | Form Thêm |
| **Ngừng chiếu** (`Stopped`) | giữ nguyên | Manager bấm ngừng chiếu | Form Sửa / danh sách phim |

Badge trạng thái ở form Thêm cập nhật ngay khi đổi ngày (chỉ hiện `Sắp chiếu` / `Đang chiếu`,
vì phim mới chưa có suất chiếu nên chưa thể ra `Chiếu sớm`).

---

## 5. Slug

Để trống ô Slug → `GenerateSlug()` bỏ dấu tiếng Việt, bỏ ký tự đặc biệt (`:`, `(`, `)`), nối bằng `-`:

```text
phim-dien-anh-doraemon-nobita-va-lau-dai-duoi-day-bien-phien-ban-moi
```

Muốn slug ngắn gọn hơn thì nhập tay:

```text
doraemon-lau-dai-duoi-day-bien
```

**Test trùng slug:** thêm phim lần 2 với đúng slug trên → hệ thống tự nối thêm ticks
(`doraemon-lau-dai-duoi-day-bien-638...`), không được báo lỗi 500.

---

## 6. Bộ case validation (âm tính)

Nhập từng giá trị sai để kiểm tra thông báo lỗi, các trường còn lại giữ như mục 1:

| # | Trường | Giá trị sai | Thông báo mong đợi |
| --- | --- | --- | --- |
| 1 | Tên phim | *(để trống)* | Vui lòng nhập tên phim |
| 2 | Tên phim | chuỗi 256 ký tự | Tên phim tối đa 255 ký tự |
| 3 | Mô tả | chuỗi 3001 ký tự | Mô tả tối đa 3000 ký tự |
| 4 | Diễn viên | chuỗi 2001 ký tự | Diễn viên tối đa 2000 ký tự |
| 5 | Thời lượng | `0` hoặc `601` | Thời lượng phải từ 1 đến 600 phút |
| 6 | Ngày khởi chiếu | *(để trống)* | Vui lòng chọn ngày khởi chiếu |
| 7 | Ngày khởi chiếu | `2026-07-01` (quá khứ) | Ngày khởi chiếu phải là hôm nay hoặc trong tương lai — **chỉ khi Thêm; form Sửa vẫn cho phép** |
| 8 | Giới hạn độ tuổi | sửa DOM thành `C21` rồi submit | Giới hạn độ tuổi không hợp lệ |
| 9 | Trailer URL | `abc` | Trailer phải là một URL hợp lệ |
| 10 | Poster | file `.txt` | Báo lỗi định dạng ảnh |
| 11 | Poster | ảnh > 2MB | Ảnh tối đa 2MB |
| 12 | Poster + Banner | tổng > 10MB | Request bị chặn ở giới hạn upload |

---

## 7. Script SQL chèn nhanh (tuỳ chọn)

Dùng khi muốn có sẵn phim trong DB mà không qua form. Chạy trên DB đã nạp `CinemaWebDB_SeedData.sql`
(cần các `genre_id` seed sẵn). Đặt `@Today` giống quy ước của file seed.

```sql
DECLARE @Today DATE = CAST(GETDATE() AS DATE);
DECLARE @MovieId UNIQUEIDENTIFIER = '00000000-0000-0000-0008-0000000000ff';

INSERT INTO [Movies]
    ([id],[title],[slug],[description],[trailer_url],[poster_url],[banner_url],
     [director],[cast_members],[language],[subtitle],[duration_minutes],
     [release_date],[age_rating],[status],[created_at])
VALUES
(@MovieId,
 N'Phim Điện Ảnh Doraemon: Nobita và Lâu Đài Dưới Đáy Biển (Phiên bản mới)',
 'doraemon-lau-dai-duoi-day-bien',
 N'Nghỉ hè, Nobita cùng Doraemon và nhóm bạn dùng bảo bối biến nước biển thành không khí để mở một chuyến cắm trại dưới đáy đại dương. Chuyến đi tưởng chừng vô tư lại đưa cả nhóm lạc vào vương quốc Mu cổ xưa và tòa lâu đài Quỷ dưới đáy biển, nơi một hệ thống vũ khí tự động còn sót lại từ nền văn minh đã diệt vong vẫn đang âm thầm chờ ngày kích hoạt. Giữa lằn ranh sinh tử, tình bạn và sự hy sinh của những người đồng hành trở thành thứ duy nhất có thể ngăn thảm họa nhấn chìm cả thế giới.',
 NULL,                      -- trailer_url: điền link YouTube thật khi có
 NULL,                      -- poster_url: tên file trần trong wwwroot/images/, vd 'doraemon-lau-dai-duoi-day-bien.webp' (KHÔNG có dấu / đầu)
 NULL,                      -- banner_url: tương tự, vd 'banner_doraemon-lau-dai-duoi-day-bien.webp'
 N'Tsutomu Shibayama',
 N'Lồng tiếng: Wasabi Mizuta, Megumi Ohara, Yumi Kakazu, Subaru Kimura, Tomokazu Seki',
 N'Tiếng Nhật', N'Tiếng Việt', 96,
 DATEADD(DAY, 17, @Today),  -- ngày khởi chiếu ở tương lai => Sắp chiếu
 'P', N'Coming Soon', @Today);

-- Thể loại: Phiêu Lưu, Hoạt Hình, Gia Đình
INSERT INTO [Movie_Genres] ([movie_id],[genre_id]) VALUES
(@MovieId,'00000000-0000-0000-0007-000000000006'),
(@MovieId,'00000000-0000-0000-0007-000000000007'),
(@MovieId,'00000000-0000-0000-0007-00000000000a');
```

**Xoá sau khi test:**

```sql
DELETE FROM [Movie_Genres] WHERE [movie_id] = '00000000-0000-0000-0008-0000000000ff';
DELETE FROM [Movies]       WHERE [id]       = '00000000-0000-0000-0008-0000000000ff';
```

> Phim đã có suất chiếu / vé thì phải xoá các bảng liên quan trước, hoặc dùng chức năng
> **Ngừng chiếu** thay vì xoá.

---

## 8. Checklist kiểm tra sau khi thêm

- [ ] Phim hiện trong danh sách `Manager → Quản lý phim`, đúng badge trạng thái.
- [ ] Poster/banner đã lưu vào `wwwroot/uploads/posters/` và `wwwroot/uploads/banners/`, hiển thị đúng.
- [ ] Trang chi tiết công khai mở được qua slug, hiện đủ mô tả / đạo diễn / diễn viên / thời lượng / độ tuổi.
- [ ] 3 thể loại hiển thị đúng ở trang chi tiết và lọc được ở trang danh sách phim.
- [ ] Trailer nhúng phát được (nếu đã điền link thật).
- [ ] Vào form **Sửa**: mọi giá trị load lại đúng, 3 checkbox thể loại đã tick sẵn.
- [ ] Sửa mà **không** upload ảnh mới → poster/banner cũ giữ nguyên, không bị xoá.
- [ ] Upload ảnh mới khi sửa → file ảnh cũ trong `/uploads/` bị xoá, không để lại file rác.
- [ ] Xếp 1 suất chiếu trước ngày khởi chiếu → trạng thái chuyển thành **Chiếu sớm**.
- [ ] Ghi nhận `Audit Log` cho hành động thêm/sửa phim.

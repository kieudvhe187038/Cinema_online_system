/* =====================================================================================
   Migrations.sql
   Gộp các migration chạy trên CSDL ĐÃ TỒN TẠI (không cần DROP/CREATE lại database).
   Mỗi migration là 1 section đánh số theo ngày, độc lập với các section khác,
   có thể chạy riêng từng phần hoặc chạy trọn file.

   Lưu ý: `CinemaWebDB_v2.sql` (script tạo mới CSDL từ đầu) đã LUÔN tích hợp sẵn mọi
   thay đổi bên dưới — chỉ cần chạy các migration này nếu CSDL của bạn được tạo
   TRƯỚC ngày của migration tương ứng.
   ===================================================================================== */
USE CinemaWebDB;
GO

-- =====================================================================================
-- [2026-06-18] Cho phép khung giờ phụ thu (Price_Time_Configs) QUA ĐÊM
-- =====================================================================================
-- Trước đây: CK_PTime_clock CHECK (end_time > start_time) => không cho phép khung giờ
-- vắt qua nửa đêm (vd 22:00 -> 02:00). Nay nới thành end_time <> start_time:
--   * end_time > start_time : khung giờ trong cùng một ngày (vd 08:00 -> 12:00)
--   * end_time < start_time : khung giờ qua đêm, kết thúc vào ngày hôm sau (vd 22:00 -> 02:00)
--   * end_time = start_time : vẫn cấm (mơ hồ 0 giờ / 24 giờ)
-- =====================================================================================
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PTime_clock')
    ALTER TABLE [Price_Time_Configs] DROP CONSTRAINT [CK_PTime_clock];
GO

ALTER TABLE [Price_Time_Configs]
    ADD CONSTRAINT [CK_PTime_clock] CHECK ([end_time] <> [start_time]);
GO

-- =====================================================================================
-- [2026-07-25] [Seat_Holds].hold_token: phân biệt phiên giữ ghế theo từng tab/máy quầy
-- =====================================================================================
-- Trước đây quyền sở hữu 1 lệnh giữ ghế chỉ khóa theo [user_id]. Ở quầy, nhiều máy/nhiều
-- tab thường dùng CHUNG 1 tài khoản Staff => hold của máy A bị máy B coi là "của chính
-- mình" nên B vẫn chọn được đúng ghế A đang giữ (không hề bị chặn).
-- Thêm [hold_token]: mỗi tab quầy tự sinh 1 GUID riêng (lưu trong sessionStorage), quyền
-- sở hữu hold = (user_id + hold_token). NULL = luồng khách đặt online (mỗi khách 1 tài
-- khoản nên user_id đã đủ định danh) hoặc các dòng cũ có trước migration này.
-- =====================================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('Seat_Holds') AND name = 'hold_token')
    ALTER TABLE [Seat_Holds] ADD [hold_token] UNIQUEIDENTIFIER NULL;
GO

-- =====================================================================================
-- [2026-07-25] Xóa bảng [Chatbot_Logs] (chức năng AI Chatbot đã gỡ khỏi hệ thống)
-- =====================================================================================
-- Tính năng chatbot (widget hỏi đáp gọi Google Gemini) đã bị xóa khỏi mã nguồn trước đó:
-- không còn Entity `ChatbotLog`, `DbSet`, mapping trong DbContext hay Service nào đọc/ghi
-- bảng này. Đây là dự án EF DB-first nên xóa mapping KHÔNG tự drop bảng — bảng vẫn nằm lại
-- trong CSDL cùng dữ liệu cũ. Migration này dọn nốt phần vật lý.
-- Lưu ý: phải drop FOREIGN KEY về [Users] trước rồi mới drop được bảng.
-- =====================================================================================
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ChatbotLogs_Users')
    ALTER TABLE [Chatbot_Logs] DROP CONSTRAINT [FK_ChatbotLogs_Users];
GO

IF OBJECT_ID('Chatbot_Logs', 'U') IS NOT NULL
    DROP TABLE [Chatbot_Logs];
GO

-- =====================================================================================
-- [2026-07-26] Chuẩn hóa [Payments].payment_method về đúng các phương thức hệ thống có
-- =====================================================================================
-- Hệ thống CHỈ sinh ra 5 giá trị: Cash/Transfer (quầy — CounterBookingService),
-- VNPay/VietQR (online — ShowtimeController), Free (đơn 0đ do mã giảm giá/điểm phủ hết tiền).
-- Dữ liệu seed cũ còn Momo/ZaloPay/Credit Card — những cổng KHÔNG hề được tích hợp, làm báo
-- cáo "Phương thức thanh toán" hiện ra các mục không có thật.
-- Quy ước quy đổi: Momo + Credit Card -> VNPay (cổng thanh toán thật của hệ thống),
-- ZaloPay -> VietQR (chuyển khoản quét QR). Toàn bộ các dòng này đều là payment_source='Online'.
-- Chạy remap TRƯỚC rồi mới thêm CHECK constraint, nếu không constraint sẽ bị từ chối.
--
-- BẪY: [Payments] có filtered unique index trên [transaction_ref] nên UPDATE đòi
-- QUOTED_IDENTIFIER ON. SSMS bật sẵn, nhưng chạy bằng `sqlcmd` thì mặc định TẮT và sẽ
-- báo "Msg 1934 ... SET options have incorrect settings: 'QUOTED_IDENTIFIER'".
-- Dòng SET dưới đây xử lý sẵn (hoặc chạy sqlcmd kèm cờ -I).
-- =====================================================================================
SET QUOTED_IDENTIFIER ON;
GO

UPDATE [Payments] SET [payment_method] = N'VNPay'
WHERE [payment_method] IN (N'Momo', N'Credit Card');
GO

UPDATE [Payments] SET [payment_method] = N'VietQR'
WHERE [payment_method] = N'ZaloPay';
GO

-- Kiểm tra còn giá trị lạ nào không trước khi khóa lại (nên trả về 0 dòng).
SELECT [payment_method], COUNT(*) AS so_dong
FROM [Payments]
WHERE [payment_method] NOT IN (N'Cash', N'Transfer', N'VNPay', N'VietQR', N'Free')
GROUP BY [payment_method];
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Payments_method')
    ALTER TABLE [Payments] ADD CONSTRAINT [CK_Payments_method]
        CHECK ([payment_method] IN ('Cash', 'Transfer', 'VNPay', 'VietQR', 'Free'));
GO

-- =====================================================================================
-- [2026-07-27] Gán [Food_Beverages].image_url cho 8 món bắp nước của bộ seed
-- =====================================================================================
-- Bộ seed ban đầu bỏ trống cột này (NULL) nên mọi màn có đồ ăn đều không hiện ảnh:
-- Views/Customer/Showtime/Food.cshtml, Views/Staff/StaffCounter/Index.cshtml,
-- Views/Manager/FoodBeverages/Index.cshtml + Edit.cshtml.
-- Ảnh đã nằm sẵn trong wwwroot/images/foods/ (nguồn & giấy phép: CREDITS.md cùng thư mục).
--
-- LƯU Ý: cột này lưu ĐƯỜNG DẪN ĐẦY ĐỦ có "/" đầu — KHÁC [Movies].[poster_url] (tên file trần).
-- Lý do: các view render thẳng @f.ImageUrl, không đi qua Application/Common/MediaUrl.cs.
--
-- Idempotent: khớp theo [name], và chỉ ghi đè khi ảnh đang trống hoặc vẫn đang trỏ đúng
-- đường dẫn seed => KHÔNG đụng tới ảnh do Manager tự upload (/uploads/foods/...).
-- =====================================================================================
UPDATE fb
SET fb.[image_url] = v.[url]
FROM [Food_Beverages] fb
JOIN (VALUES
    (N'Bắp rang bơ (vừa)',     '/images/foods/bap-rang-bo-vua.jpg'),
    (N'Bắp rang bơ (lớn)',     '/images/foods/bap-rang-bo-lon.jpg'),
    (N'Coca-Cola (vừa)',       '/images/foods/coca-cola-vua.jpg'),
    (N'Coca-Cola (lớn)',       '/images/foods/coca-cola-lon.jpg'),
    (N'Combo Đôi',             '/images/foods/combo-doi.jpg'),
    (N'Combo Gia Đình',        '/images/foods/combo-gia-dinh.jpg'),
    (N'Nước suối Aquafina',    '/images/foods/nuoc-suoi.jpg'),
    (N'Khoai tây lắc phô mai', '/images/foods/khoai-tay-lac-pho-mai.jpg')
) AS v([name], [url]) ON v.[name] = fb.[name]
WHERE fb.[image_url] IS NULL
   OR fb.[image_url] LIKE '/images/foods/%';
GO

-- Kiểm tra: cột thieu_anh nên bằng 0.
SELECT COUNT(*) AS so_mon,
       SUM(CASE WHEN [image_url] IS NULL THEN 1 ELSE 0 END) AS thieu_anh
FROM [Food_Beverages];
GO

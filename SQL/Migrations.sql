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

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

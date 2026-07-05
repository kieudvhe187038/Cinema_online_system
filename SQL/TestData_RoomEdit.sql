/* =====================================================================================
   TestData_RoomEdit.sql
   Dữ liệu test cho chức năng SỬA PHÒNG + sơ đồ ghế (module Quản lý Phòng chiếu).
   Tạo 3 phòng test phủ 3 kịch bản:
     1) "Test - Suất tương lai"  → có suất chiếu SẮP TỚI  ⇒ Sửa bị KHÓA sơ đồ ghế (chỉ đổi tên/loại/trạng thái).
     2) "Test - Vé quá khứ"      → có vé ở suất ĐÃ CHIẾU   ⇒ đổi sơ đồ ghế sẽ LƯU TRỮ phòng cũ + TẠO PHÒNG MỚI.
     3) "Test - Sửa tự do"       → KHÔNG có suất/vé        ⇒ sửa sơ đồ ghế thoải mái tại chỗ.

   - Mốc thời gian dùng GETDATE() nên luôn đúng dù chạy lúc nào.
   - Chạy LẠI nhiều lần được: phần đầu tự xóa sạch dữ liệu test cũ theo quan hệ FK.
   - Phụ thuộc dữ liệu seed (CinemaWebDB_SeedData_Large.sql): rạp, loại phòng, loại ghế, phim, user.
   ===================================================================================== */
SET NOCOUNT ON;

/* ---- Khóa ngoại tham chiếu tới dữ liệu seed ---- */
DECLARE @cinema     UNIQUEIDENTIFIER = '00000000-0000-0000-0003-000000000001'; -- CinemaWeb Hà Nội - Hoàn Kiếm
DECLARE @roomType   UNIQUEIDENTIFIER = '00000000-0000-0000-0004-000000000001'; -- 2D
DECLARE @seatStd    UNIQUEIDENTIFIER = '00000000-0000-0000-0006-000000000001'; -- Standard (span 1)
DECLARE @seatCouple UNIQUEIDENTIFIER = '00000000-0000-0000-0006-000000000003'; -- Couple   (span 2)
DECLARE @movie      UNIQUEIDENTIFIER = '00000000-0000-0000-0008-000000000001'; -- Bí Mật Phố Cổ
DECLARE @user       UNIQUEIDENTIFIER = '00000000-0000-0000-0002-000000000001'; -- admin (làm người đặt vé test)

/* ---- GUID cố định của dữ liệu test (prefix aaaaaaaa- để dễ nhận biết & dọn) ---- */
DECLARE @roomFuture UNIQUEIDENTIFIER = 'aaaaaaaa-0000-0000-0005-000000000001';
DECLARE @roomPast   UNIQUEIDENTIFIER = 'aaaaaaaa-0000-0000-0005-000000000002';
DECLARE @roomFree   UNIQUEIDENTIFIER = 'aaaaaaaa-0000-0000-0005-000000000003';

DECLARE @testRooms TABLE (id UNIQUEIDENTIFIER);
INSERT INTO @testRooms VALUES (@roomFuture), (@roomPast), (@roomFree);

/* =====================================================================================
   0) DỌN DỮ LIỆU TEST CŨ (theo thứ tự FK: con trước, cha sau)
   ===================================================================================== */
DELETE FROM [Tickets]
 WHERE [showtime_id] IN (SELECT id FROM [Showtimes] WHERE [room_id] IN (SELECT id FROM @testRooms))
    OR [seat_id]     IN (SELECT id FROM [Seats]     WHERE [room_id] IN (SELECT id FROM @testRooms));

DELETE FROM [Booking_Foods]
 WHERE [booking_id] IN (SELECT id FROM [Bookings]
        WHERE [showtime_id] IN (SELECT id FROM [Showtimes] WHERE [room_id] IN (SELECT id FROM @testRooms)));

DELETE FROM [Payments]
 WHERE [booking_id] IN (SELECT id FROM [Bookings]
        WHERE [showtime_id] IN (SELECT id FROM [Showtimes] WHERE [room_id] IN (SELECT id FROM @testRooms)));

DELETE FROM [Bookings]
 WHERE [showtime_id] IN (SELECT id FROM [Showtimes] WHERE [room_id] IN (SELECT id FROM @testRooms));

DELETE FROM [Seat_Holds]
 WHERE [showtime_id] IN (SELECT id FROM [Showtimes] WHERE [room_id] IN (SELECT id FROM @testRooms))
    OR [seat_id]     IN (SELECT id FROM [Seats]     WHERE [room_id] IN (SELECT id FROM @testRooms));

DELETE FROM [Showtimes] WHERE [room_id] IN (SELECT id FROM @testRooms);
DELETE FROM [Seats]     WHERE [room_id] IN (SELECT id FROM @testRooms);
DELETE FROM [Rooms]     WHERE [id]      IN (SELECT id FROM @testRooms);

/* =====================================================================================
   1) PHÒNG "Test - Suất tương lai"  (3 hàng x 4 cột, 12 ghế thường)
      → có 2 suất chiếu SẮP TỚI ⇒ mở Sửa sẽ thấy sơ đồ ghế KHÓA (chỉ sửa tên/loại/trạng thái).
   ===================================================================================== */
INSERT INTO [Rooms] ([id],[cinema_id],[name],[room_type_id],[total_seats],[total_columns],[total_row],[status])
VALUES (@roomFuture, @cinema, N'Test - Suất tương lai', @roomType, 12, 4, 3, N'Active');

INSERT INTO [Seats] ([id],[room_id],[seat_type_id],[row_number],[seat_number],[status]) VALUES
('aaaaaaaa-0000-0000-000e-000000000001', @roomFuture, @seatStd, 1, 1, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000002', @roomFuture, @seatStd, 1, 2, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000003', @roomFuture, @seatStd, 1, 3, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000004', @roomFuture, @seatStd, 1, 4, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000005', @roomFuture, @seatStd, 2, 1, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000006', @roomFuture, @seatStd, 2, 2, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000007', @roomFuture, @seatStd, 2, 3, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000008', @roomFuture, @seatStd, 2, 4, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000009', @roomFuture, @seatStd, 3, 1, N'Available'),
('aaaaaaaa-0000-0000-000e-00000000000a', @roomFuture, @seatStd, 3, 2, N'Available'),
('aaaaaaaa-0000-0000-000e-00000000000b', @roomFuture, @seatStd, 3, 3, N'Available'),
('aaaaaaaa-0000-0000-000e-00000000000c', @roomFuture, @seatStd, 3, 4, N'Available');

-- 2 suất chiếu trong tương lai (sau hôm nay 5 và 7 ngày)
INSERT INTO [Showtimes] ([id],[movie_id],[room_id],[start_time],[end_time],[status]) VALUES
('aaaaaaaa-0000-0000-0009-000000000001', @movie, @roomFuture, DATEADD(DAY, 5, GETDATE()), DATEADD(MINUTE, 110, DATEADD(DAY, 5, GETDATE())), N'Scheduled'),
('aaaaaaaa-0000-0000-0009-000000000002', @movie, @roomFuture, DATEADD(DAY, 7, GETDATE()), DATEADD(MINUTE, 110, DATEADD(DAY, 7, GETDATE())), N'Scheduled');

-- (Tùy chọn) 1 vé cho suất tương lai để có cả "vé đặt trong tương lai"
INSERT INTO [Bookings] ([id],[user_id],[showtime_id],[total_amount],[discount_amount],[vat_amount],[final_amount],[payment_status],[booking_type],[created_at])
VALUES ('aaaaaaaa-0000-0000-000d-000000000001', @user, 'aaaaaaaa-0000-0000-0009-000000000001', 200000.00, 0.00, 16000.00, 216000.00, N'Paid', N'Online', GETDATE());

INSERT INTO [Tickets] ([id],[booking_id],[showtime_id],[seat_id],[price_at_booking],[status]) VALUES
('aaaaaaaa-0000-0000-000f-000000000001', 'aaaaaaaa-0000-0000-000d-000000000001', 'aaaaaaaa-0000-0000-0009-000000000001', 'aaaaaaaa-0000-0000-000e-000000000001', 100000.00, N'Booked'),
('aaaaaaaa-0000-0000-000f-000000000002', 'aaaaaaaa-0000-0000-000d-000000000001', 'aaaaaaaa-0000-0000-0009-000000000001', 'aaaaaaaa-0000-0000-000e-000000000002', 100000.00, N'Booked');

/* =====================================================================================
   2) PHÒNG "Test - Vé quá khứ"  (2 hàng x 3 cột, 6 ghế thường)
      → có suất chiếu ĐÃ CHIẾU (quá khứ) + vé ⇒ đổi sơ đồ ghế khi Sửa sẽ LƯU TRỮ phòng cũ + TẠO PHÒNG MỚI.
        (chỉ đổi tên/loại/trạng thái thì vẫn cập nhật tại chỗ, KHÔNG tạo phòng mới.)
   ===================================================================================== */
INSERT INTO [Rooms] ([id],[cinema_id],[name],[room_type_id],[total_seats],[total_columns],[total_row],[status])
VALUES (@roomPast, @cinema, N'Test - Vé quá khứ', @roomType, 6, 3, 2, N'Active');

INSERT INTO [Seats] ([id],[room_id],[seat_type_id],[row_number],[seat_number],[status]) VALUES
('aaaaaaaa-0000-0000-000e-000000000101', @roomPast, @seatStd, 1, 1, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000102', @roomPast, @seatStd, 1, 2, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000103', @roomPast, @seatStd, 1, 3, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000104', @roomPast, @seatStd, 2, 1, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000105', @roomPast, @seatStd, 2, 2, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000106', @roomPast, @seatStd, 2, 3, N'Available');

-- 1 suất chiếu đã kết thúc (10 ngày trước)
INSERT INTO [Showtimes] ([id],[movie_id],[room_id],[start_time],[end_time],[status]) VALUES
('aaaaaaaa-0000-0000-0009-000000000010', @movie, @roomPast, DATEADD(DAY, -10, GETDATE()), DATEADD(MINUTE, 110, DATEADD(DAY, -10, GETDATE())), N'Completed');

-- 1 đơn + 2 vé cho suất quá khứ → phòng "đã từng bán vé"
INSERT INTO [Bookings] ([id],[user_id],[showtime_id],[total_amount],[discount_amount],[vat_amount],[final_amount],[payment_status],[booking_type],[created_at])
VALUES ('aaaaaaaa-0000-0000-000d-000000000010', @user, 'aaaaaaaa-0000-0000-0009-000000000010', 200000.00, 0.00, 16000.00, 216000.00, N'Paid', N'Online', DATEADD(DAY, -11, GETDATE()));

INSERT INTO [Tickets] ([id],[booking_id],[showtime_id],[seat_id],[price_at_booking],[status]) VALUES
('aaaaaaaa-0000-0000-000f-000000000010', 'aaaaaaaa-0000-0000-000d-000000000010', 'aaaaaaaa-0000-0000-0009-000000000010', 'aaaaaaaa-0000-0000-000e-000000000101', 100000.00, N'Checked-in'),
('aaaaaaaa-0000-0000-000f-000000000011', 'aaaaaaaa-0000-0000-000d-000000000010', 'aaaaaaaa-0000-0000-0009-000000000010', 'aaaaaaaa-0000-0000-000e-000000000102', 100000.00, N'Checked-in');

/* =====================================================================================
   3) PHÒNG "Test - Sửa tự do"  (3 hàng x 4 cột; có 1 ghế ĐÔI để thử vẽ ghế đôi)
      → KHÔNG có suất chiếu/vé ⇒ sửa sơ đồ ghế thoải mái, lưu tại chỗ.
   ===================================================================================== */
INSERT INTO [Rooms] ([id],[cinema_id],[name],[room_type_id],[total_seats],[total_columns],[total_row],[status])
VALUES (@roomFree, @cinema, N'Test - Sửa tự do', @roomType, 11, 4, 3, N'Active');

INSERT INTO [Seats] ([id],[room_id],[seat_type_id],[row_number],[seat_number],[status]) VALUES
('aaaaaaaa-0000-0000-000e-000000000201', @roomFree, @seatStd,    1, 1, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000202', @roomFree, @seatStd,    1, 2, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000203', @roomFree, @seatStd,    1, 3, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000204', @roomFree, @seatStd,    1, 4, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000205', @roomFree, @seatStd,    2, 1, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000206', @roomFree, @seatStd,    2, 2, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000207', @roomFree, @seatStd,    2, 3, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000208', @roomFree, @seatStd,    2, 4, N'Available'),
('aaaaaaaa-0000-0000-000e-000000000209', @roomFree, @seatCouple, 3, 1, N'Available'),  -- ghế đôi chiếm cột 1-2
('aaaaaaaa-0000-0000-000e-00000000020a', @roomFree, @seatStd,    3, 3, N'Available'),
('aaaaaaaa-0000-0000-000e-00000000020b', @roomFree, @seatStd,    3, 4, N'Available');

PRINT N'Đã tạo 3 phòng test: "Test - Suất tương lai" (khóa ghế), "Test - Vé quá khứ" (versioning), "Test - Sửa tự do" (sửa tại chỗ).';
GO

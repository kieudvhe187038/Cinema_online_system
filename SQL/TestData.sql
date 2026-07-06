/* =====================================================================================
   TestData.sql
   Gộp các script dữ liệu DEV/TEST (KHÔNG dùng cho production) thành 1 file, gồm 3 phần
   độc lập — chạy trọn file hoặc chỉ chạy riêng từng phần (A/B/C) đều được:

     A) Reset mật khẩu dev cho admin/manager1/staff1@cinemaweb.vn -> "Admin@123"
        (seed gốc CinemaWebDB_SeedData_Large.sql dùng password_hash mẫu nên không login được)
     B) Seed dữ liệu cho trang Đặt vé tại quầy (Counter Booking) — idempotent,
        tự tạo cinema/phòng/ghế/giá/VAT/nhân viên nếu CSDL còn thiếu.
     C) Seed 3 phòng test cho chức năng Sửa phòng + sơ đồ ghế (Room Edit) — idempotent,
        tự dọn dữ liệu test cũ (theo GUID prefix aaaaaaaa-) rồi tạo lại từ đầu.
        Phụ thuộc dữ liệu seed chính (CinemaWebDB_SeedData_Large.sql): rạp, loại phòng,
        loại ghế, phim, user — phải chạy seed chính trước phần C.

   Yêu cầu chung: đã chạy CinemaWebDB_v2.sql (schema) trước đó.
   ===================================================================================== */
USE CinemaWebDB;
GO

-- #######################################################################################
-- A) DEV: RESET MẬT KHẨU (Admin@123) CHO admin/manager1/staff1
--    Mật khẩu chung sau khi chạy: Admin@123 (hash BCrypt cost=10 ứng với chuỗi này).
--    CHỈ DÙNG CHO MÔI TRƯỜNG DEV. Không chạy trên production.
-- #######################################################################################
DECLARE @devHash VARCHAR(255) = '$2a$10$spiC4CGfKOTsfXpOkVTK1OJtsH0PeuKkrzu.td9jClj.6DTiCbBRi';

UPDATE [Users]
SET [password_hash] = @devHash,
    [updated_at]    = GETDATE()
WHERE [email] IN (
    'admin@cinemaweb.vn',     -- ADMIN
    'manager1@cinemaweb.vn',  -- MANAGER
    'staff1@cinemaweb.vn'     -- STAFF
);
GO

PRINT N'[A] Da dat mat khau "Admin@123" cho admin/manager1/staff1@cinemaweb.vn';
GO

-- #######################################################################################
-- B) SEED: Đặt vé tại quầy (#39 Counter Ticket Booking)
--    Idempotent: chạy nhiều lần không tạo trùng.
--    Mục tiêu: có >= 1 phim với suất chiếu TƯƠNG LAI (status='Scheduled'),
--    phòng + ghế + cấu hình giá Active + VAT + nhân viên Staff active.
-- #######################################################################################
SET NOCOUNT ON;
DECLARE @now datetime = GETDATE();

/* ---------- B1. Role + User Staff (active) ---------- */
DECLARE @staffRoleId uniqueidentifier =
    (SELECT TOP 1 id FROM Roles WHERE name = 'Staff');
IF @staffRoleId IS NULL
BEGIN
    SET @staffRoleId = NEWID();
    INSERT INTO Roles (id, name, description) VALUES (@staffRoleId, 'Staff', N'Nhân viên quầy');
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE role_id = @staffRoleId AND status = 'Active')
BEGIN
    INSERT INTO Users (id, full_name, email, phone, password_hash, role_id, status, reward_points, created_at)
    VALUES (NEWID(), N'Nhân viên Quầy 01', 'staff01@cinema.local', '0900000001',
            'SEED_NO_LOGIN', @staffRoleId, 'Active', 0, @now);
END

/* ---------- B2. Cinema ---------- */
DECLARE @cinemaId uniqueidentifier = (SELECT TOP 1 id FROM Cinemas);
IF @cinemaId IS NULL
BEGIN
    SET @cinemaId = NEWID();
    INSERT INTO Cinemas (id, name, address, hotline, status, created_at)
    VALUES (@cinemaId, N'Cinema Trung Tâm', N'123 Đường Lê Lợi', '19001234', 'Active', @now);
END

/* ---------- B3. RoomType + phụ thu loại phòng ---------- */
DECLARE @roomTypeId uniqueidentifier = (SELECT TOP 1 id FROM Room_Types);
IF @roomTypeId IS NULL
BEGIN
    SET @roomTypeId = NEWID();
    INSERT INTO Room_Types (id, name, description) VALUES (@roomTypeId, N'2D', N'Phòng chiếu 2D tiêu chuẩn');
END
IF NOT EXISTS (SELECT 1 FROM Price_Room_Type_Configs WHERE room_type_id = @roomTypeId AND status = 'Active')
    INSERT INTO Price_Room_Type_Configs (id, room_type_id, type_surcharge, effective_from, effective_to, status)
    VALUES (NEWID(), @roomTypeId, 0, @now, NULL, 'Active');

/* ---------- B4. SeatType (Thường + VIP) + phụ thu loại ghế ---------- */
DECLARE @stStandard uniqueidentifier = (SELECT TOP 1 id FROM Seat_Types WHERE name = N'Thường');
IF @stStandard IS NULL
BEGIN
    SET @stStandard = NEWID();
    INSERT INTO Seat_Types (id, name, capacity, column_span) VALUES (@stStandard, N'Thường', 1, 1);
END
DECLARE @stVip uniqueidentifier = (SELECT TOP 1 id FROM Seat_Types WHERE name = N'VIP');
IF @stVip IS NULL
BEGIN
    SET @stVip = NEWID();
    INSERT INTO Seat_Types (id, name, capacity, column_span) VALUES (@stVip, N'VIP', 1, 1);
END
IF NOT EXISTS (SELECT 1 FROM Price_Seat_Configs WHERE seat_type_id = @stStandard AND status = 'Active')
    INSERT INTO Price_Seat_Configs (id, seat_type_id, seat_surcharge, effective_from, effective_to, status)
    VALUES (NEWID(), @stStandard, 0, @now, NULL, 'Active');
IF NOT EXISTS (SELECT 1 FROM Price_Seat_Configs WHERE seat_type_id = @stVip AND status = 'Active')
    INSERT INTO Price_Seat_Configs (id, seat_type_id, seat_surcharge, effective_from, effective_to, status)
    VALUES (NEWID(), @stVip, 30000, @now, NULL, 'Active');

/* ---------- B5. Room (5 hàng x 8 ghế = 40) + Seats ---------- */
DECLARE @rows int = 5, @cols int = 8;
DECLARE @roomId uniqueidentifier = (SELECT TOP 1 id FROM Rooms WHERE cinema_id = @cinemaId AND name = N'Phòng 1');
IF @roomId IS NULL
BEGIN
    SET @roomId = NEWID();
    INSERT INTO Rooms (id, cinema_id, name, room_type_id, total_row, total_columns, total_seats, status)
    VALUES (@roomId, @cinemaId, N'Phòng 1', @roomTypeId, @rows, @cols, @rows * @cols, 'Active');
END

-- Tạo ghế còn thiếu (hàng cuối = VIP). UK_Seats_Room_Row_Number chống trùng.
;WITH r AS (SELECT TOP (@rows) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS rn FROM sys.all_objects),
      c AS (SELECT TOP (@cols) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS cn FROM sys.all_objects)
INSERT INTO Seats (id, room_id, [row_number], seat_number, seat_type_id, status)
SELECT NEWID(), @roomId, r.rn, c.cn,
       CASE WHEN r.rn = @rows THEN @stVip ELSE @stStandard END, 'Available'
FROM r CROSS JOIN c
WHERE NOT EXISTS (SELECT 1 FROM Seats s WHERE s.room_id = @roomId AND s.[row_number] = r.rn AND s.seat_number = c.cn);

/* ---------- B6. Movie + giá cơ bản (Active) ---------- */
DECLARE @movieId uniqueidentifier = (SELECT TOP 1 id FROM Movies WHERE slug = 'seed-counter-demo');
IF @movieId IS NULL
BEGIN
    SET @movieId = NEWID();
    INSERT INTO Movies (id, title, slug, description, duration_minutes, age_rating, language, status, created_at)
    VALUES (@movieId, N'Phim Demo Quầy', 'seed-counter-demo', N'Phim seed để test đặt vé tại quầy',
            120, 'P', N'Tiếng Việt', 'Showing', @now);
END
IF NOT EXISTS (SELECT 1 FROM Price_Base_Configs WHERE movie_id = @movieId AND status = 'Active')
    INSERT INTO Price_Base_Configs (id, movie_id, base_price, effective_from, effective_to, status)
    VALUES (NEWID(), @movieId, 70000, @now, NULL, 'Active');

/* ---------- B7. VAT active ---------- */
IF NOT EXISTS (SELECT 1 FROM VAT WHERE status = 'Active')
    INSERT INTO VAT (id, vat_rate, description, status, created_at)
    VALUES (NEWID(), 8.00, N'VAT 8%', 'Active', @now);

/* ---------- B8. Đồ ăn (tùy chọn) ---------- */
IF NOT EXISTS (SELECT 1 FROM Food_Beverages WHERE name = N'Bắp rang (vừa)')
    INSERT INTO Food_Beverages (id, name, price, stock_status) VALUES (NEWID(), N'Bắp rang (vừa)', 45000, 'In Stock');
IF NOT EXISTS (SELECT 1 FROM Food_Beverages WHERE name = N'Nước ngọt (lớn)')
    INSERT INTO Food_Beverages (id, name, price, stock_status) VALUES (NEWID(), N'Nước ngọt (lớn)', 30000, 'In Stock');

/* ---------- B9. Suất chiếu TƯƠNG LAI (status='Scheduled') ---------- */
-- 3 suất: +2h, +5h hôm nay, và 19:00 ngày mai. Chống trùng theo (movie, room, start_time).
DECLARE @dur int = 120;
DECLARE @slots TABLE (start_time datetime);
INSERT INTO @slots (start_time) VALUES
    (DATEADD(HOUR, 2, @now)),
    (DATEADD(HOUR, 5, @now)),
    (DATEADD(HOUR, 19, CAST(CAST(DATEADD(DAY, 1, @now) AS date) AS datetime)));

INSERT INTO Showtimes (id, movie_id, room_id, start_time, end_time, status)
SELECT NEWID(), @movieId, @roomId, s.start_time, DATEADD(MINUTE, @dur, s.start_time), 'Scheduled'
FROM @slots s
WHERE NOT EXISTS (
    SELECT 1 FROM Showtimes x
    WHERE x.movie_id = @movieId AND x.room_id = @roomId AND x.start_time = s.start_time);

/* ---------- Kết quả phần B ---------- */
SELECT
    (SELECT COUNT(*) FROM Movies)                                                          AS movies,
    (SELECT COUNT(*) FROM Showtimes WHERE start_time >= @now AND status = 'Scheduled')     AS future_scheduled,
    (SELECT COUNT(*) FROM Seats WHERE room_id = @roomId)                                   AS seats_in_room,
    (SELECT COUNT(*) FROM Users WHERE role_id = @staffRoleId AND status = 'Active')        AS active_staff,
    (SELECT COUNT(*) FROM VAT WHERE status = 'Active')                                     AS active_vat;
GO

-- #######################################################################################
-- C) SEED: Test SỬA PHÒNG + sơ đồ ghế (module Quản lý Phòng chiếu)
--    Tạo 3 phòng test phủ 3 kịch bản:
--      1) "Test - Suất tương lai"  → có suất chiếu SẮP TỚI  ⇒ Sửa bị KHÓA sơ đồ ghế (chỉ đổi tên/loại/trạng thái).
--      2) "Test - Vé quá khứ"      → có vé ở suất ĐÃ CHIẾU   ⇒ đổi sơ đồ ghế sẽ LƯU TRỮ phòng cũ + TẠO PHÒNG MỚI.
--      3) "Test - Sửa tự do"       → KHÔNG có suất/vé        ⇒ sửa sơ đồ ghế thoải mái tại chỗ.
--    - Mốc thời gian dùng GETDATE() nên luôn đúng dù chạy lúc nào.
--    - Chạy LẠI nhiều lần được: phần đầu tự xóa sạch dữ liệu test cũ theo quan hệ FK.
--    - Phụ thuộc dữ liệu seed (CinemaWebDB_SeedData_Large.sql): rạp, loại phòng, loại ghế, phim, user.
-- #######################################################################################
SET NOCOUNT ON;

/* ---- Khóa ngoại tham chiếu tới dữ liệu seed ---- */
DECLARE @cinema     UNIQUEIDENTIFIER = '00000000-0000-0000-0003-000000000001'; -- CinemaWeb Hà Nội (rạp duy nhất)
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

/* ---- C0) DỌN DỮ LIỆU TEST CŨ (theo thứ tự FK: con trước, cha sau) ---- */
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

/* ---- C1) PHÒNG "Test - Suất tương lai" (3 hàng x 4 cột, 12 ghế thường) ---- */
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

/* ---- C2) PHÒNG "Test - Vé quá khứ" (2 hàng x 3 cột, 6 ghế thường) ---- */
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

/* ---- C3) PHÒNG "Test - Sửa tự do" (3 hàng x 4 cột; có 1 ghế ĐÔI để thử vẽ ghế đôi) ---- */
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

PRINT N'[C] Da tao 3 phong test: "Test - Suat tuong lai" (khoa ghe), "Test - Ve qua khu" (versioning), "Test - Sua tu do" (sua tai cho).';
GO

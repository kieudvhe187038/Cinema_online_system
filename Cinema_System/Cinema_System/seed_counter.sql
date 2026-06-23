/* ============================================================
   Seed dữ liệu cho trang Đặt vé tại quầy (#39 Counter Ticket Booking)
   Idempotent: chạy nhiều lần không tạo trùng.
   Mục tiêu: có >= 1 phim với suất chiếu TƯƠNG LAI (status='Scheduled'),
   phòng + ghế + cấu hình giá Active + VAT + nhân viên Staff active.
   Chạy:  sqlcmd -S Localhost -U sa -P 123 -d CinemaWebDB -C -i seed_counter.sql
   ============================================================ */
SET NOCOUNT ON;
DECLARE @now datetime = GETDATE();

/* ---------- 1. Role + User Staff (active) ---------- */
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

/* ---------- 2. Cinema ---------- */
DECLARE @cinemaId uniqueidentifier = (SELECT TOP 1 id FROM Cinemas);
IF @cinemaId IS NULL
BEGIN
    SET @cinemaId = NEWID();
    INSERT INTO Cinemas (id, name, address, hotline, status, created_at)
    VALUES (@cinemaId, N'Cinema Trung Tâm', N'123 Đường Lê Lợi', '19001234', 'Active', @now);
END

/* ---------- 3. RoomType + phụ thu loại phòng ---------- */
DECLARE @roomTypeId uniqueidentifier = (SELECT TOP 1 id FROM Room_Types);
IF @roomTypeId IS NULL
BEGIN
    SET @roomTypeId = NEWID();
    INSERT INTO Room_Types (id, name, description) VALUES (@roomTypeId, N'2D', N'Phòng chiếu 2D tiêu chuẩn');
END
IF NOT EXISTS (SELECT 1 FROM Price_Room_Type_Configs WHERE room_type_id = @roomTypeId AND status = 'Active')
    INSERT INTO Price_Room_Type_Configs (id, room_type_id, type_surcharge, effective_from, effective_to, status)
    VALUES (NEWID(), @roomTypeId, 0, @now, NULL, 'Active');

/* ---------- 4. SeatType (Thường + VIP) + phụ thu loại ghế ---------- */
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

/* ---------- 5. Room (5 hàng x 8 ghế = 40) + Seats ---------- */
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

/* ---------- 6. Movie + giá cơ bản (Active) ---------- */
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

/* ---------- 7. VAT active ---------- */
IF NOT EXISTS (SELECT 1 FROM VAT WHERE status = 'Active')
    INSERT INTO VAT (id, vat_rate, description, status, created_at)
    VALUES (NEWID(), 8.00, N'VAT 8%', 'Active', @now);

/* ---------- 8. Đồ ăn (tùy chọn) ---------- */
IF NOT EXISTS (SELECT 1 FROM Food_Beverages WHERE name = N'Bắp rang (vừa)')
    INSERT INTO Food_Beverages (id, name, price, stock_status) VALUES (NEWID(), N'Bắp rang (vừa)', 45000, 'In Stock');
IF NOT EXISTS (SELECT 1 FROM Food_Beverages WHERE name = N'Nước ngọt (lớn)')
    INSERT INTO Food_Beverages (id, name, price, stock_status) VALUES (NEWID(), N'Nước ngọt (lớn)', 30000, 'In Stock');

/* ---------- 9. Suất chiếu TƯƠNG LAI (status='Scheduled') ---------- */
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

/* ---------- Kết quả ---------- */
SELECT
    (SELECT COUNT(*) FROM Movies)                                                          AS movies,
    (SELECT COUNT(*) FROM Showtimes WHERE start_time >= @now AND status = 'Scheduled')     AS future_scheduled,
    (SELECT COUNT(*) FROM Seats WHERE room_id = @roomId)                                   AS seats_in_room,
    (SELECT COUNT(*) FROM Users WHERE role_id = @staffRoleId AND status = 'Active')        AS active_staff,
    (SELECT COUNT(*) FROM VAT WHERE status = 'Active')                                     AS active_vat;

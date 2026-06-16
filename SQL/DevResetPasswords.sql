/*
 * DevResetPasswords.sql
 * ----------------------------------------------------------------------------
 * Dữ liệu seed gốc (CinemaWebDB_SeedData_Large.sql) dùng password_hash MẪU
 * dạng '$2a$10$samplehash...' nên KHÔNG đăng nhập được.
 *
 * Script này đặt lại mật khẩu cho một vài tài khoản để TEST chức năng login.
 * Mật khẩu chung sau khi chạy: Admin@123
 * (hash BCrypt cost=10 bên dưới ứng với chuỗi "Admin@123").
 *
 * CHỈ DÙNG CHO MÔI TRƯỜNG DEV. Không chạy trên production.
 * ----------------------------------------------------------------------------
 */

USE CinemaWebDB;
GO

DECLARE @hash VARCHAR(255) = '$2a$10$spiC4CGfKOTsfXpOkVTK1OJtsH0PeuKkrzu.td9jClj.6DTiCbBRi';

UPDATE [Users]
SET [password_hash] = @hash,
    [updated_at]    = GETDATE()
WHERE [email] IN (
    'admin@cinemaweb.vn',     -- ADMIN
    'manager1@cinemaweb.vn',  -- MANAGER
    'staff1@cinemaweb.vn'     -- STAFF
);
GO

PRINT 'Da dat mat khau "Admin@123" cho admin/manager1/staff1@cinemaweb.vn';
GO

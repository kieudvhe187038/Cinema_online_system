# 🎬 Cinema Web System (Advanced Version)

Hệ thống quản lý và đặt vé xem phim trực tuyến toàn diện, tích hợp các tính năng nâng cao như khóa ghế thời gian thực (Realtime Seat Locking), cấu hình giá vé linh hoạt, AI Chatbot tích hợp và hệ thống lưu vết thông minh.

## 📌 Tổng Quan Dự Án

Dự án này cung cấp giải pháp chuyển đổi số toàn diện cho các rạp chiếu phim, tối ưu hóa trải nghiệm khách hàng từ khâu tra cứu lịch chiếu, đặt vé, chọn bắp nước đến tích điểm thành viên và nhận vé điện tử qua email. Đồng thời, hệ thống cung cấp công cụ quản trị mạnh mẽ (Audit Logs, Price Configuration) cho bộ phận vận hành.

---

## 🛠 Thiết Kế Cơ Sở Dữ Liệu (Database Architecture)

Cấu trúc cơ sở dữ liệu được thiết kế theo chuẩn DBML, hỗ trợ import trực tiếp tại [dbdiagram.io](https://dbdiagram.io/).

### 🗺 Sơ đồ mối quan hệ tổng quan

Hệ thống dữ liệu được chia thành 7 nhóm thực thể chính có mối quan hệ chặt chẽ với nhau:

[Roles] ──< [Users] ── Silber ──< [Bookings] ──< [Tickets] ── [Ticket_Scans]
│                            │
[Seat_Holds] (< Showtimes)           ├──< [Booking_Foods] ──> [Food_Beverages]
│                            │
[Seats] ──> [Rooms]             └──> [Payments]

---

## 🗂 Chi Tiết Các Nhóm Bảng Trong Hệ Thống

### 1. Nhóm Quản Lý Người Dùng, Vai Trò & Bảo Mật
* `Roles`: Định nghĩa nhóm quyền hạn trong hệ thống (`Admin`, `Manager`, `Staff`, `Customer`, `Guest`).
* `Users`: Lưu trữ thông tin tài khoản, điểm tích lũy thành viên (`reward_points`) và trạng thái hoạt động.
* `Password_Reset_Tokens`: Cơ chế mã hóa mã OTP/Token phục vụ tính năng quên mật khẩu an toàn.

### 2. Nhóm Quản Lý Rạp & Sơ Đồ Ghế Ngồi
* `Rooms` & `Room_Types`: Quản lý phòng chiếu theo định dạng (`2D`, `3D`, `IMAX`, `ScreenX`).
* `Seats` & `Seat_Types`: Định vị tọa độ ghế (Hàng, Số) và phân loại phân khúc (`Regular`, `VIP`, `Couple`).
* `Seat_Holds` *(Nâng cao)*: Cơ chế giữ ghế tạm thời trong 5-10 phút khi khách hàng đang tiến hành thanh toán, ngăn chặn tình trạng trùng ghế trực thời gian thực.

### 3. Nhóm Quản Lý Phim & Lịch Chiếu
* `Movies` & `Genres`: Quản lý thông tin phim, trạng thái phát hành (`Now Showing`, `Coming Soon`, `Stopped`) và bảng trung gian `Movie_Genres`.
* `Showtimes`: Điều phối lịch chiếu cụ thể theo khung giờ và phòng chiếu.

### 4. Dịch Vụ Bắp Nước, Khuyến Mãi & Tích Điểm
* `Food_Beverages`: Menu đồ ăn kèm (F&B) kèm trạng thái kho hàng.
* `Promotions`: Hệ thống mã giảm giá linh hoạt, hỗ trợ giảm theo % hoặc số tiền cố định, áp dụng riêng cho Vé, F&B hoặc Toàn bộ đơn hàng.
* `Reward_Point_History`: Lưu vết chi tiết lịch sử tích điểm (`Earned`), đổi quà (`Redeemed`), hoặc hoàn điểm (`Refund_Rollback`).

### 5. Nhóm Đặt Vé, Giao Dịch, Hoàn Tiền & Email
* `Bookings`: Hóa đơn tổng lưu trữ thông tin tiền gốc, thuế VAT, giảm giá và số tiền cuối cùng phải trả (`final_amount`).
* `Tickets` & `Ticket_Scans`: Mã QR riêng biệt cho từng vé và phân hệ quét vé kiểm tra lối vào (`Checked-in`).
* `Booking_Foods`: Chi tiết số lượng bắp nước được đặt kèm trong hóa đơn.
* `Payments`: Cổng ghi nhận thanh toán đa nền tảng (`VNPay`, `MoMo`, `Cash`...) và xử lý tiền thừa tại quầy.
* `Email_Logs` *(Nâng cao)*: Hàng đợi gửi thư điện tử (Email Queue) tự động gửi vé kèm mã QR, có cơ chế lưu vết lỗi (`Failed`, `Sent`).

### 6. Nhóm Chăm Sóc Khách Hàng & Hệ Thống Logs
* `Reviews`: Đánh giá, chấm điểm phim từ người dùng đã qua kiểm duyệt (`Approved`).
* `Chatbot_Logs`: Lưu trữ lịch sử tương tác của khách hàng với AI Chatbot nhằm nhận diện ý định (`intent_detected`) để cải tiến dịch vụ.
* `Audit_Logs`: Nhật ký tối quan trọng lưu lại vết hành động của Admin/Staff (lưu dữ liệu cũ và mới trước/sau khi cập nhật) nhằm đảm bảo tính minh bạch dữ liệu.

### 7. Nhóm Cấu Hình Giá Vé Linh Hoạt (Price Configuration)
Hệ thống áp dụng công thức tính giá vé tự động động:
$$\text{Total Price} = \text{Base Price} + \text{Seat Surcharge} + \text{Type Surcharge} + \text{Time Surcharge}$$

* `Price_Base_Configs`: Giá gốc mặc định hoặc giá riêng cho từng bộ phim bom tấn.
* `Price_Seat_Configs`: Phụ thu theo loại ghế (ví dụ: VIP +20k, Couple +50k).
* `Price_Room_Type_Configs`: Phụ thu theo phòng chiếu (ví dụ: IMAX +50k).
* `Price_Time_Configs`: Phụ thu theo khung giờ cao điểm, ngày cuối tuần (Thứ 7, CN) hoặc các ngày lễ tết cụ thể (`specific_date`) dựa trên mức độ ưu tiên (`priority`).
* `VAT`: Quản lý cấu hình thuế suất áp dụng linh hoạt trên đơn hàng.

---

## 🚀 Hướng Dẫn Sử Dụng Trên dbdiagram.io

Để xem trực quan biểu đồ ERD (Entity-Relationship Diagram) và xuất ra file SQL (MySQL, PostgreSQL, SQL Server):

1. Truy cập trang web [dbdiagram.io](https://dbdiagram.io/).
2. Tạo một diagram mới.
3. Copy toàn bộ nội dung mã cấu hình DBML trong file mã nguồn cơ sở dữ liệu của bạn và dán vào khung chỉnh sửa bên trái.
4. Hệ thống sẽ tự động vẽ sơ đồ quan hệ thực thể trực quan ở khung bên phải.

## 🔒 Quy Tắc Bảo Mật & Ràng Buộc Dữ Liệu
* Mật khẩu người dùng bắt buộc phải được băm (`password_hash`) trước khi lưu trữ.
* Mã QR của giao dịch (`Bookings.qr_code`) và vé (`Tickets.qr_code`) là duy nhất (`unique`).
* Tất cả các hành động chỉnh sửa cấu hình hệ thống từ phía quản trị viên bắt buộc phải kích hoạt Trigger để ghi nhận vào `Audit_Logs`.
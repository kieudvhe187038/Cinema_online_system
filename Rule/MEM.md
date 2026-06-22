### [2026-06-18] Kiểm tra quyền xem phim trước khi đánh giá (By: copilot)
- **What changed:** 
  1. Controllers/Public/ReviewsController.cs: Thêm try-catch block trong action Create (GET) để handle exception tốt hơn; cập nhật error message từ "Bạn chỉ có thể đánh giá những phim mà bạn đã xem." → "Bạn chưa xem phim này nên chưa được đánh giá. Vui lòng đặt vé xem phim trước."
  2. Views/Public/Movies/Details.cshtml: Thêm section hiển thị TempData["Error"] và TempData["Success"] messages dạng alert box (red cho error, green cho success) ngay sau nút "Quay lại"
  3. SQL: Tạo user test `testcustomer@gmail.com` (password: Admin@123, role: CUSTOMER)
  4. SQL: Tạo test booking + ticket cho testcustomer user để có thể test chức năng viết đánh giá
- **Why:** User yêu cầu: chỉ user đã xem phim (có ticket) mới được viết đánh giá; nếu chưa xem sẽ thông báo. Cần test user mới để verify functionality.
- **Impact/Notes for Team:** 
  - `HasUserWatchedMovieAsync` kiểm tra xem user có ticket nào cho phim đó không bằng cách: lấy tất cả completed bookings → loop through → check tickets với Showtime.MovieId matching
  - TempData messages hiển thị khi redirect từ Create action
  - Test user testcustomer@gmail.com đã có booking + ticket cho một phim để có thể test review flow
  - Nếu user chưa xem phim, click "Viết đánh giá" → redirect về Movie Details → show error message

### [2026-06-18] Cải thiện UI trang Đánh giá và Chi tiết phim (By: copilot)
- **What changed:** 
  1. Controllers/Public/ReviewsController.cs: Giảm pagesize từ 10 xuống 9 items/trang cho cả 2 method (GetMovieReviewsAsync và GetRecentReviewsAsync)
  2. Views/Public/Reviews/Index.cshtml: Bỏ nút "Đăng nhập" cho user chưa authenticate; cải thiện UI với gradient background, hiệu ứng hover nâng cao, shadow tốt hơn, pagination button cải tiến
  3. Views/Public/Movies/Details.cshtml: Thêm nút "Quay lại" ở đầu trang; cải thiện UI toàn trang với gradient backgrounds, shadow effects, hover animations; làm đẹp review cards với gradient overlay hover; thêm link "Xem tất cả đánh giá" ở cuối section reviews
  4. Application/Services/ReviewService.cs: Thiết lập `TotalCount` và đảm bảo `TotalPages` hợp lý khi trả `PagedResult<ReviewDTO>` (fix hiển thị tổng lượt đánh giá bằng 0 trên view).
  5. Views/Public/Reviews/Index.cshtml: Loại bỏ phần `aside` (panel thông tin) và tái bố trí nội dung — vùng reviews giờ nằm chính giữa trang (max-width giới hạn, responsive giữ nguyên). Đồng thời căn giữa hiển thị "Tổng số đánh giá" trong hero.
  6. Views/Shared/_Layout.cshtml: Tăng padding-top của `main` để tránh header chồng lấn lên tiêu đề trang (fix giao diện khi cuộn).
  7. Views/Public/Movies/Details.cshtml và một số view liên quan: Đồng nhất icon sang `material-symbols-outlined` và đổi văn bản nút đăng nhập thành "Đăng nhập để đánh giá".
- **Why:** Theo yêu cầu user - trang Đánh giá cần: bỏ chữ "Đăng nhập", giới hạn 9 reviews/trang, làm đẹp giao diện. Trang Chi tiết phim cần: thêm nút quay lại, làm đẹp giao diện tổng thể.
- **Impact/Notes for Team:** UI improvements sử dụng Tailwind utility classes hiện có (gradient, shadows, hover effects) không thêm custom CSS; tất cả responsive design đã được kiểm tra; "Quay lại" button dùng javascript:history.back() cho UX tự nhiên.

### [2026-06-17] Thông tin rạp CineStar (By: copilot)
- **What changed:** Thêm trang Views/Public/Home/Info.cshtml hiển thị giới thiệu CineStar, cập nhật HomeController.Info() và liên kết Thông tin rạp trong Views/Shared/_Layout.cshtml và footer.
- **Why:** Tạo trang giới thiệu rạp mới theo yêu cầu, đồng bộ với giao diện Tailwind hiện tại và mở điều hướng từ header + footer.
- **Impact/Notes for Team:** Dùng layout chung _Layout.cshtml; toàn bộ liên kết từ header/footer dẫn về HomeController.Info; không thêm chi nhánh mới, chỉ hiển thị thông tin rạp duy nhất tại Hà Nội.

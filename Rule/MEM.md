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

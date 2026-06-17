### [2026-06-17] Thông tin rạp CineStar (By: copilot)
- **What changed:** Thêm trang Views/Public/Home/Info.cshtml hiển thị giới thiệu CineStar, cập nhật HomeController.Info() và liên kết Thông tin rạp trong Views/Shared/_Layout.cshtml và footer.
- **Why:** Tạo trang giới thiệu rạp mới theo yêu cầu, đồng bộ với giao diện Tailwind hiện tại và mở điều hướng từ header + footer.
- **Impact/Notes for Team:** Dùng layout chung _Layout.cshtml; toàn bộ liên kết từ header/footer dẫn về HomeController.Info; không thêm chi nhánh mới, chỉ hiển thị thông tin rạp duy nhất tại Hà Nội.

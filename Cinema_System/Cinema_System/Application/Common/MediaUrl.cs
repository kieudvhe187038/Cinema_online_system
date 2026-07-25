namespace Cinema_System.Application.Common
{
    /// <summary>
    /// Chuẩn hoá đường dẫn ảnh phim để hiển thị trên view.
    /// DB seed lưu tên file trần (vd "movie1.jpg", "banner_movie1.png"); ảnh upload lưu sẵn đường dẫn tuyệt đối.
    /// - Poster nằm ở wwwroot/images/  -> "/images/{file}"
    /// - Banner phim nằm ở wwwroot/banner/ (tên chứa "banner") -> "/banner/{file}"
    /// Giá trị đã có prefix "/" hoặc "http" thì giữ nguyên; rỗng thì trả về null để view tự dùng ảnh placeholder.
    /// </summary>
    public static class MediaUrl
    {
        public static string? Poster(string? url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            if (url.StartsWith("/") || url.StartsWith("http")) return url;
            return $"/images/{url}";
        }

        public static string? Banner(string? url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            if (url.StartsWith("/") || url.StartsWith("http")) return url;
            return url.Contains("banner") ? $"/banner/{url}" : $"/images/{url}";
        }
    }
}

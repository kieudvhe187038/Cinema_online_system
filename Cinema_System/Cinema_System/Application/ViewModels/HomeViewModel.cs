using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

public class HomeViewModel
{
    // Phim lên banner: phim đang chiếu đạt ngưỡng đánh giá cao (xem BannerHighlight).
    // Đã kèm AverageRating/ReviewCount, khác với NowShowingMovies bên dưới.
    public IEnumerable<MovieDTO> BannerMovies { get; set; } = new List<MovieDTO>();

    public IEnumerable<MovieDTO> NowShowingMovies { get; set; } = new List<MovieDTO>();
    public IEnumerable<MovieDTO> ComingSoonMovies { get; set; } = new List<MovieDTO>();
    public IEnumerable<MovieDTO> SpecialMovies { get; set; } = new List<MovieDTO>();
}

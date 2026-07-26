using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

public class MovieProfile : Profile
{
    // Cấu hình AutoMapper giữa entity Movie/Genre và các DTO/ViewModel.
    public MovieProfile()
    {
        CreateMap<Movie, MovieDTO>()
            .ForMember(d => d.GenreNames, o => o.MapFrom(s => s.Genres.Select(g => g.Name).ToList()))
            // Còn suất chiếu BÁN ĐƯỢC trong phạm vi đặt trước -> mở nút mua vé (phim sắp chiếu có lịch vẫn
            // đặt trước được). Điều kiện phải khớp bộ lọc của lịch chiếu để nút không dẫn tới popup rỗng.
            .ForMember(d => d.HasUpcomingShowtimes,
                o => o.MapFrom(s => s.Showtimes.Any(st =>
                    st.Status == ShowtimeStatus.Scheduled &&
                    st.StartTime > DateTime.Now &&
                    st.StartTime < DateTime.Now.AddDays(ShowtimeBrowsing.AdvanceDays))));

        CreateMap<Movie, MovieFormViewModel>()
            .ForMember(d => d.SelectedGenreIds, o => o.MapFrom(s => s.Genres.Select(g => g.Id).ToList()))
            // Gán sau khi map: cần truy vấn riêng.
            .ForMember(d => d.AvailableGenres, o => o.Ignore())
            .ForMember(d => d.FirstShowtimeStart, o => o.Ignore());

        CreateMap<Genre, GenreDTO>();
    }
}

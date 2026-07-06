using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

public class ShowtimeProfile : Profile
{
    // Chỉ map các mảnh "phẳng" của luồng đặt vé; phần có tính toán (giá ghế, VAT, điểm) vẫn dựng tay trong service.
    public ShowtimeProfile()
    {
        // Dòng đồ ăn trong đơn: lấy Name/Price từ entity, Quantity gán sau khi map.
        CreateMap<FoodBeverage, FoodLineItem>()
            .ForMember(d => d.FbId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Quantity, o => o.Ignore());

        CreateMap<Showtime, ShowtimeDTO>()
            .ForMember(d => d.MovieTitle, o => o.MapFrom(s => s.Movie.Title))
            .ForMember(d => d.MoviePosterUrl, o => o.MapFrom(s => s.Movie.PosterUrl))
            .ForMember(d => d.DurationMinutes, o => o.MapFrom(s => s.Movie.DurationMinutes))
            .ForMember(d => d.AgeRating, o => o.MapFrom(s => s.Movie.AgeRating))
            .ForMember(d => d.RoomName, o => o.MapFrom(s => s.Room.Name))
            .ForMember(d => d.RoomTypeName, o => o.MapFrom(s => s.Room.RoomType != null ? s.Room.RoomType.Name : null))
            .ForMember(d => d.CinemaName, o => o.MapFrom(s => s.Room.Cinema != null ? s.Room.Cinema.Name : string.Empty));

        // Ghế trong sơ đồ chọn ghế: RowLabel/Price/State phụ thuộc bối cảnh suất chiếu (giá, ghế đã đặt/giữ) -> gán sau khi map.
        CreateMap<Seat, SeatDTO>()
            .ForMember(d => d.SeatTypeName, o => o.MapFrom(s => s.SeatType != null ? s.SeatType.Name : string.Empty))
            .ForMember(d => d.RowLabel, o => o.Ignore())
            .ForMember(d => d.Price, o => o.Ignore())
            .ForMember(d => d.State, o => o.Ignore());
    }
}

using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

/// <summary>
/// Map cho 4 loại cấu hình giá. Chỉ map chiều ĐỌC (Entity → DTO hiển thị, Entity → Form sửa);
/// chiều GHI (Form → Entity) vẫn map tay trong <see cref="Services.PriceService"/> để kiểm soát
/// theo từng loại và tránh ghi đè ngoài ý muốn.
/// </summary>
public class PriceProfile : Profile
{
    public PriceProfile()
    {
        // -------- Entity → DTO (danh sách hiển thị) --------
        CreateMap<PriceBaseConfig, PriceConfigDTO>()
            .ForMember(d => d.Kind, o => o.MapFrom(_ => PriceKind.Base))
            .ForMember(d => d.Amount, o => o.MapFrom(s => s.BasePrice))
            .ForMember(d => d.IsGlobalBase, o => o.MapFrom(s => s.MovieId == null))
            .ForMember(d => d.TargetName, o => o.MapFrom(s => s.Movie != null ? s.Movie.Title : "Áp dụng chung (mọi phim)"))
            .ForMember(d => d.DisplayStatus, o => o.MapFrom((s, _) => PriceDisplayStatus.Resolve(s.EffectiveFrom, s.EffectiveTo, DateTime.Now, s.Status)));

        CreateMap<PriceRoomTypeConfig, PriceConfigDTO>()
            .ForMember(d => d.Kind, o => o.MapFrom(_ => PriceKind.Room))
            .ForMember(d => d.Amount, o => o.MapFrom(s => s.TypeSurcharge))
            .ForMember(d => d.TargetName, o => o.MapFrom(s => s.RoomType != null ? s.RoomType.Name : "—"))
            .ForMember(d => d.DisplayStatus, o => o.MapFrom((s, _) => PriceDisplayStatus.Resolve(s.EffectiveFrom, s.EffectiveTo, DateTime.Now, s.Status)));

        CreateMap<PriceSeatConfig, PriceConfigDTO>()
            .ForMember(d => d.Kind, o => o.MapFrom(_ => PriceKind.Seat))
            .ForMember(d => d.Amount, o => o.MapFrom(s => s.SeatSurcharge))
            .ForMember(d => d.TargetName, o => o.MapFrom(s => s.SeatType != null ? s.SeatType.Name : "—"))
            .ForMember(d => d.DisplayStatus, o => o.MapFrom((s, _) => PriceDisplayStatus.Resolve(s.EffectiveFrom, s.EffectiveTo, DateTime.Now, s.Status)));

        CreateMap<PriceTimeConfig, PriceConfigDTO>()
            .ForMember(d => d.Kind, o => o.MapFrom(_ => PriceKind.Time))
            .ForMember(d => d.Amount, o => o.MapFrom(s => s.TimeSurcharge))
            .ForMember(d => d.TargetName, o => o.MapFrom((s, _) => DescribeTime(s)))
            .ForMember(d => d.DisplayStatus, o => o.MapFrom((s, _) => PriceDisplayStatus.Resolve(s.EffectiveFrom, s.EffectiveTo, DateTime.Now, s.Status)));
        // TimeCondition/DayOfWeek/SpecificDate/StartTime/EndTime/RuleGroup/Priority tự map theo tên.

        // -------- Entity → Form ViewModel (đổ vào form Sửa) --------
        CreateMap<PriceBaseConfig, PriceConfigFormViewModel>()
            .ForMember(d => d.Kind, o => o.MapFrom(_ => PriceKind.Base));

        CreateMap<PriceRoomTypeConfig, PriceConfigFormViewModel>()
            .ForMember(d => d.Kind, o => o.MapFrom(_ => PriceKind.Room));

        CreateMap<PriceSeatConfig, PriceConfigFormViewModel>()
            .ForMember(d => d.Kind, o => o.MapFrom(_ => PriceKind.Seat));

        CreateMap<PriceTimeConfig, PriceConfigFormViewModel>()
            .ForMember(d => d.Kind, o => o.MapFrom(_ => PriceKind.Time));
    }

    // Mô tả khung giờ/điều kiện cho cột "Điều kiện áp dụng" (end < start = qua đêm).
    private static string DescribeTime(PriceTimeConfig e) => e.TimeCondition switch
    {
        PriceTimeCondition.DayOfWeek => PriceDayOfWeek.Label(e.DayOfWeek),
        PriceTimeCondition.SpecificDate => e.SpecificDate?.ToString("dd/MM/yyyy") ?? "Ngày cụ thể",
        PriceTimeCondition.TimeRange => e.EndTime < e.StartTime
            ? $"{e.StartTime:HH\\:mm} – {e.EndTime:HH\\:mm} (hôm sau)"
            : $"{e.StartTime:HH\\:mm} – {e.EndTime:HH\\:mm}",
        _ => PriceTimeCondition.Label(e.TimeCondition)
    };
}

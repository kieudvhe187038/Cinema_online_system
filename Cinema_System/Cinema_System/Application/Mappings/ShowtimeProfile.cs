using AutoMapper;
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
    }
}

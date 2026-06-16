using Cinema_System.Application.Common;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface IPointConfigService
{
    Task<PointRateViewModel> GetRateAsync();

    Task<Result> UpdateRateAsync(PointRateViewModel model);
}

using System.Globalization;
using Cinema_System.Application.Common;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

public class PointConfigService : IPointConfigService
{
    private const string RateKey = "reward_point_rate";
    private const string DefaultDescription = "Tỉ lệ tích điểm trên số tiền";

    private readonly IUnitOfWork _unitOfWork;

    public PointConfigService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PointRateViewModel> GetRateAsync()
    {
        var config = await _unitOfWork.SystemConfigs
            .FirstOrDefaultAsync(c => c.ConfigKey == RateKey);

        return new PointRateViewModel
        {
            Rate = ParseRate(config?.ConfigValue),
            Description = config?.Description ?? DefaultDescription,
            UpdatedAt = config?.UpdatedAt
        };
    }

    public async Task<Result> UpdateRateAsync(PointRateViewModel model)
    {
        if (model.Rate < 0 || model.Rate > 1)
            return Result.Failure("Tỉ lệ phải nằm trong khoảng 0 đến 1.");

        var config = await _unitOfWork.SystemConfigs
            .FirstOrDefaultAsync(c => c.ConfigKey == RateKey);

        var rateText = model.Rate.ToString(CultureInfo.InvariantCulture);

        if (config is null)
        {
            config = new SystemConfig
            {
                ConfigKey = RateKey,
                ConfigValue = rateText,
                Description = DefaultDescription,
                UpdatedAt = DateTime.Now
            };
            await _unitOfWork.SystemConfigs.AddAsync(config);
        }
        else
        {
            config.ConfigValue = rateText;
            config.UpdatedAt = DateTime.Now;
            _unitOfWork.SystemConfigs.Update(config);
        }

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    private static decimal ParseRate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0m;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate)
            ? rate
            : 0m;
    }
}

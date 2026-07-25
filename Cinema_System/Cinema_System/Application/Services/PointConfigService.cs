using System.Globalization;
using Cinema_System.Application.Common;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

/// <summary>
/// Quản lý chính sách điểm thưởng trong SystemConfig — nguồn DUY NHẤT cho toàn hệ thống:
/// tỉ lệ tích điểm (reward_point_rate) và giá trị quy đổi khi tiêu điểm (point_value_vnd).
/// </summary>
public class PointConfigService : IPointConfigService
{
    private const string RateKey = "reward_point_rate";
    private const string PointValueKey = "point_value_vnd";
    private const string RateDescription = "Tỉ lệ tích điểm trên số tiền";
    private const string PointValueDescription = "Giá trị quy đổi 1 điểm khi giảm giá (VND)";

    // Dùng khi chưa có cấu hình trong DB (giữ đúng giá trị đã hardcode trước đây để không đổi hành vi).
    private const int DefaultPointValueVnd = 100;

    private readonly IUnitOfWork _unitOfWork;

    public PointConfigService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PointRateViewModel> GetRateAsync()
    {
        var rateConfig = await _unitOfWork.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == RateKey);
        var valueConfig = await _unitOfWork.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == PointValueKey);

        return new PointRateViewModel
        {
            Rate = ParseDecimal(rateConfig?.ConfigValue),
            PointValueVnd = ParseInt(valueConfig?.ConfigValue, DefaultPointValueVnd),
            Description = rateConfig?.Description ?? RateDescription,
            // Lấy mốc cập nhật gần nhất giữa hai khóa để màn hình hiển thị đúng lần sửa cuối.
            UpdatedAt = Latest(rateConfig?.UpdatedAt, valueConfig?.UpdatedAt)
        };
    }

    public async Task<PointPolicy> GetPolicyAsync()
    {
        var vm = await GetRateAsync();
        return new PointPolicy(vm.Rate, vm.PointValueVnd);
    }

    public async Task<Result> UpdateRateAsync(PointRateViewModel model)
    {
        if (model.Rate < 0 || model.Rate > 1)
            return Result.Failure("Tỉ lệ phải nằm trong khoảng 0 đến 1.");

        if (model.PointValueVnd < 1)
            return Result.Failure("Giá trị quy đổi của 1 điểm phải từ 1đ trở lên.");

        await UpsertAsync(RateKey, model.Rate.ToString(CultureInfo.InvariantCulture), RateDescription);
        await UpsertAsync(PointValueKey, model.PointValueVnd.ToString(CultureInfo.InvariantCulture), PointValueDescription);

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    private async Task UpsertAsync(string key, string value, string description)
    {
        var config = await _unitOfWork.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == key);

        if (config is null)
        {
            await _unitOfWork.SystemConfigs.AddAsync(new SystemConfig
            {
                ConfigKey = key,
                ConfigValue = value,
                Description = description,
                UpdatedAt = DateTime.Now
            });
            return;
        }

        config.ConfigValue = value;
        config.UpdatedAt = DateTime.Now;
        _unitOfWork.SystemConfigs.Update(config);
    }

    private static DateTime? Latest(DateTime? first, DateTime? second)
    {
        if (first is null) return second;
        if (second is null) return first;
        return first > second ? first : second;
    }

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0m;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate)
            ? rate
            : 0m;
    }

    private static int ParseInt(string? value, int fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;

        return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : fallback;
    }
}

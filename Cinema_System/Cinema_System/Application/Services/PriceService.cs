using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cinema_System.Application.Services;

/// <summary>
/// Nghiệp vụ quản lý 4 nhóm cấu hình giá theo mô hình "dòng thời gian":
/// với mỗi đối tượng, các mức giá ĐANG ÁP DỤNG (status = Active) xếp theo thời điểm bắt đầu;
/// mỗi mức tự kết thúc khi mức kế tiếp bắt đầu. Người dùng KHÔNG nhập effective_to/trạng thái.
/// "Xóa" là soft-delete: chỉ đặt ngày kết thúc + status = Inactive (giữ lại trong DB để lưu vết),
/// và mức liền trước được nối lại. Không cho Sửa/Ngừng mức đã hết hạn hoặc đã ngừng.
/// Chiều ĐỌC map bằng AutoMapper (PriceProfile); chiều GHI map tay theo từng loại.
/// </summary>
public class PriceService : IPriceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PriceService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PriceManagementViewModel> GetManagementAsync(string? tab, int page, int pageSize)
    {
        var bases = await _unitOfWork.PriceBaseConfigs.GetAllAsync(includeProperties: new[] { "Movie" });
        var rooms = await _unitOfWork.PriceRoomTypeConfigs.GetAllAsync(includeProperties: new[] { "RoomType" });
        var seats = await _unitOfWork.PriceSeatConfigs.GetAllAsync(includeProperties: new[] { "SeatType" });
        var times = await _unitOfWork.PriceTimeConfigs.GetAllAsync();

        // Sắp xếp: trạng thái MỞ trước, TẮT sau. Riêng giá cơ bản: mức "Áp dụng chung" đang mở lên đầu.
        var baseList = _mapper.Map<List<PriceConfigDTO>>(bases)
            .OrderByDescending(d => d.IsGlobalBase && IsOpen(d.DisplayStatus))
            .ThenBy(d => StatusRank(d.DisplayStatus)).ThenBy(d => d.TargetName).ThenBy(d => d.EffectiveFrom).ToList();
        var roomList = _mapper.Map<List<PriceConfigDTO>>(rooms)
            .OrderBy(d => StatusRank(d.DisplayStatus)).ThenBy(d => d.TargetName).ThenBy(d => d.EffectiveFrom).ToList();
        var seatList = _mapper.Map<List<PriceConfigDTO>>(seats)
            .OrderBy(d => StatusRank(d.DisplayStatus)).ThenBy(d => d.TargetName).ThenBy(d => d.EffectiveFrom).ToList();
        var timeList = _mapper.Map<List<PriceConfigDTO>>(times)
            .OrderBy(d => StatusRank(d.DisplayStatus)).ThenBy(d => d.RuleGroup).ThenBy(d => d.TargetName).ThenBy(d => d.EffectiveFrom).ToList();

        var active = PriceKind.AllowedValues.Contains(tab ?? "") ? tab! : PriceKind.Base;
        IReadOnlyList<PriceConfigDTO> source = active switch
        {
            PriceKind.Room => roomList,
            PriceKind.Seat => seatList,
            PriceKind.Time => timeList,
            _ => baseList
        };
        var paged = PagedResult<PriceConfigDTO>.Create(source, page, pageSize);
        var empty = (IReadOnlyList<PriceConfigDTO>)Array.Empty<PriceConfigDTO>();

        return new PriceManagementViewModel
        {
            ActiveTab = active,
            // Chỉ tab đang xem mới nạp dữ liệu (đã phân trang); các tab khác để rỗng.
            BaseConfigs = active == PriceKind.Base ? paged.Items : empty,
            RoomConfigs = active == PriceKind.Room ? paged.Items : empty,
            SeatConfigs = active == PriceKind.Seat ? paged.Items : empty,
            TimeConfigs = active == PriceKind.Time ? paged.Items : empty,
            BaseCount = baseList.Count,
            RoomCount = roomList.Count,
            SeatCount = seatList.Count,
            TimeCount = timeList.Count,
            CurrentPage = paged.CurrentPage,
            TotalPages = paged.TotalPages,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };
    }

    // Thứ tự hiển thị theo trạng thái: mở (Active → Scheduled) trước, tắt (Expired → Cancelled) sau.
    private static int StatusRank(string display) => display switch
    {
        PriceDisplayStatus.Active => 0,
        PriceDisplayStatus.Scheduled => 1,
        PriceDisplayStatus.Expired => 2,
        PriceDisplayStatus.Cancelled => 3,
        _ => 4
    };

    private static bool IsOpen(string display) =>
        display is PriceDisplayStatus.Active or PriceDisplayStatus.Scheduled;

    public async Task<PriceConfigFormViewModel> BuildCreateFormAsync(string kind)
    {
        var model = new PriceConfigFormViewModel
        {
            Kind = PriceKind.AllowedValues.Contains(kind) ? kind : PriceKind.Base,
            EffectiveFrom = DateTime.Now
        };
        await PopulateOptionsAsync(model);
        return model;
    }

    // Chỉ trả form khi cấu hình còn được phép sửa (Sắp áp dụng / Đang áp dụng). Hết hạn/đã ngừng → null.
    public async Task<PriceConfigFormViewModel?> GetForEditAsync(string kind, Guid id)
    {
        if (!PriceKind.AllowedValues.Contains(kind)) return null;
        var now = DateTime.Now;

        PriceConfigFormViewModel? model = null;
        switch (kind)
        {
            case PriceKind.Base:
                var b = await _unitOfWork.PriceBaseConfigs.GetByIdAsync(id);
                if (b is null || !CanModify(b.EffectiveFrom, b.EffectiveTo, b.Status, now)) return null;
                model = _mapper.Map<PriceConfigFormViewModel>(b);
                break;
            case PriceKind.Room:
                var r = await _unitOfWork.PriceRoomTypeConfigs.GetByIdAsync(id);
                if (r is null || !CanModify(r.EffectiveFrom, r.EffectiveTo, r.Status, now)) return null;
                model = _mapper.Map<PriceConfigFormViewModel>(r);
                break;
            case PriceKind.Seat:
                var s = await _unitOfWork.PriceSeatConfigs.GetByIdAsync(id);
                if (s is null || !CanModify(s.EffectiveFrom, s.EffectiveTo, s.Status, now)) return null;
                model = _mapper.Map<PriceConfigFormViewModel>(s);
                break;
            case PriceKind.Time:
                var t = await _unitOfWork.PriceTimeConfigs.GetByIdAsync(id);
                if (t is null || !CanModify(t.EffectiveFrom, t.EffectiveTo, t.Status, now)) return null;
                model = _mapper.Map<PriceConfigFormViewModel>(t);
                break;
        }

        if (model is null) return null;
        await PopulateOptionsAsync(model);
        return model;
    }

    public async Task PopulateOptionsAsync(PriceConfigFormViewModel model)
    {
        switch (model.Kind)
        {
            case PriceKind.Base:
                var movies = await _unitOfWork.Movies.GetAllAsync(orderBy: q => q.OrderBy(m => m.Title));
                model.MovieOptions = movies.Select(m => new SelectListItem(m.Title, m.Id.ToString())).ToList();
                break;
            case PriceKind.Room:
                var roomTypes = await _unitOfWork.RoomTypes.GetAllAsync(orderBy: q => q.OrderBy(r => r.Name));
                model.RoomTypeOptions = roomTypes.Select(r => new SelectListItem(r.Name, r.Id.ToString())).ToList();
                break;
            case PriceKind.Seat:
                var seatTypes = await _unitOfWork.SeatTypes.GetAllAsync(orderBy: q => q.OrderBy(s => s.Name));
                model.SeatTypeOptions = seatTypes.Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToList();
                break;
        }
    }

    public async Task<Result> CreateAsync(PriceConfigFormViewModel model)
    {
        switch (model.Kind)
        {
            case PriceKind.Base:
                {
                    var all = (await _unitOfWork.PriceBaseConfigs.GetAllAsync()).ToList();
                    if (HasSameStart(all.Where(x => x.MovieId == model.MovieId && x.Status == PriceConfigStatus.Active), x => x.EffectiveFrom, model.EffectiveFrom, null))
                        return DuplicateStart();
                    await _unitOfWork.PriceBaseConfigs.AddAsync(new PriceBaseConfig
                    {
                        Id = Guid.NewGuid(), MovieId = model.MovieId, BasePrice = model.BasePrice,
                        EffectiveFrom = model.EffectiveFrom, Status = PriceConfigStatus.Active
                    });
                    await _unitOfWork.SaveChangesAsync();
                    await RechainBaseAsync();
                    break;
                }
            case PriceKind.Room:
                {
                    var all = (await _unitOfWork.PriceRoomTypeConfigs.GetAllAsync()).ToList();
                    if (HasSameStart(all.Where(x => x.RoomTypeId == model.RoomTypeId && x.Status == PriceConfigStatus.Active), x => x.EffectiveFrom, model.EffectiveFrom, null))
                        return DuplicateStart();
                    await _unitOfWork.PriceRoomTypeConfigs.AddAsync(new PriceRoomTypeConfig
                    {
                        Id = Guid.NewGuid(), RoomTypeId = model.RoomTypeId!.Value, TypeSurcharge = model.TypeSurcharge,
                        EffectiveFrom = model.EffectiveFrom, Status = PriceConfigStatus.Active
                    });
                    await _unitOfWork.SaveChangesAsync();
                    await RechainRoomAsync();
                    break;
                }
            case PriceKind.Seat:
                {
                    var all = (await _unitOfWork.PriceSeatConfigs.GetAllAsync()).ToList();
                    if (HasSameStart(all.Where(x => x.SeatTypeId == model.SeatTypeId && x.Status == PriceConfigStatus.Active), x => x.EffectiveFrom, model.EffectiveFrom, null))
                        return DuplicateStart();
                    await _unitOfWork.PriceSeatConfigs.AddAsync(new PriceSeatConfig
                    {
                        Id = Guid.NewGuid(), SeatTypeId = model.SeatTypeId!.Value, SeatSurcharge = model.SeatSurcharge,
                        EffectiveFrom = model.EffectiveFrom, Status = PriceConfigStatus.Active
                    });
                    await _unitOfWork.SaveChangesAsync();
                    await RechainSeatAsync();
                    break;
                }
            case PriceKind.Time:
                {
                    var entity = BuildTimeEntity(new PriceTimeConfig { Id = Guid.NewGuid() }, model);
                    entity.Status = PriceConfigStatus.Active;
                    var all = (await _unitOfWork.PriceTimeConfigs.GetAllAsync()).ToList();
                    if (HasSameStart(all.Where(x => x.Status == PriceConfigStatus.Active && TimeKey(x) == TimeKey(entity)), x => x.EffectiveFrom, model.EffectiveFrom, null))
                        return DuplicateStart();
                    await _unitOfWork.PriceTimeConfigs.AddAsync(entity);
                    await _unitOfWork.SaveChangesAsync();
                    await RechainTimeAsync();
                    break;
                }
            default:
                return Result.Failure("Loại cấu hình giá không hợp lệ.");
        }

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateAsync(PriceConfigFormViewModel model)
    {
        var now = DateTime.Now;
        switch (model.Kind)
        {
            case PriceKind.Base:
                {
                    var entity = await _unitOfWork.PriceBaseConfigs.GetByIdAsync(model.Id);
                    if (entity is null) return NotFound();
                    if (!CanModify(entity.EffectiveFrom, entity.EffectiveTo, entity.Status, now)) return NotEditable();
                    if (IsBackdated(model.EffectiveFrom, entity.EffectiveFrom)) return PastStart();
                    var all = (await _unitOfWork.PriceBaseConfigs.GetAllAsync()).ToList();
                    if (HasSameStart(all.Where(x => x.MovieId == model.MovieId && x.Status == PriceConfigStatus.Active), x => x.EffectiveFrom, model.EffectiveFrom, model.Id))
                        return DuplicateStart();
                    entity.MovieId = model.MovieId;
                    entity.BasePrice = model.BasePrice;
                    entity.EffectiveFrom = model.EffectiveFrom;
                    _unitOfWork.PriceBaseConfigs.Update(entity);
                    await _unitOfWork.SaveChangesAsync();
                    await RechainBaseAsync();
                    break;
                }
            case PriceKind.Room:
                {
                    var entity = await _unitOfWork.PriceRoomTypeConfigs.GetByIdAsync(model.Id);
                    if (entity is null) return NotFound();
                    if (!CanModify(entity.EffectiveFrom, entity.EffectiveTo, entity.Status, now)) return NotEditable();
                    if (IsBackdated(model.EffectiveFrom, entity.EffectiveFrom)) return PastStart();
                    var all = (await _unitOfWork.PriceRoomTypeConfigs.GetAllAsync()).ToList();
                    if (HasSameStart(all.Where(x => x.RoomTypeId == model.RoomTypeId && x.Status == PriceConfigStatus.Active), x => x.EffectiveFrom, model.EffectiveFrom, model.Id))
                        return DuplicateStart();
                    entity.RoomTypeId = model.RoomTypeId!.Value;
                    entity.TypeSurcharge = model.TypeSurcharge;
                    entity.EffectiveFrom = model.EffectiveFrom;
                    _unitOfWork.PriceRoomTypeConfigs.Update(entity);
                    await _unitOfWork.SaveChangesAsync();
                    await RechainRoomAsync();
                    break;
                }
            case PriceKind.Seat:
                {
                    var entity = await _unitOfWork.PriceSeatConfigs.GetByIdAsync(model.Id);
                    if (entity is null) return NotFound();
                    if (!CanModify(entity.EffectiveFrom, entity.EffectiveTo, entity.Status, now)) return NotEditable();
                    if (IsBackdated(model.EffectiveFrom, entity.EffectiveFrom)) return PastStart();
                    var all = (await _unitOfWork.PriceSeatConfigs.GetAllAsync()).ToList();
                    if (HasSameStart(all.Where(x => x.SeatTypeId == model.SeatTypeId && x.Status == PriceConfigStatus.Active), x => x.EffectiveFrom, model.EffectiveFrom, model.Id))
                        return DuplicateStart();
                    entity.SeatTypeId = model.SeatTypeId!.Value;
                    entity.SeatSurcharge = model.SeatSurcharge;
                    entity.EffectiveFrom = model.EffectiveFrom;
                    _unitOfWork.PriceSeatConfigs.Update(entity);
                    await _unitOfWork.SaveChangesAsync();
                    await RechainSeatAsync();
                    break;
                }
            case PriceKind.Time:
                {
                    var entity = await _unitOfWork.PriceTimeConfigs.GetByIdAsync(model.Id);
                    if (entity is null) return NotFound();
                    if (!CanModify(entity.EffectiveFrom, entity.EffectiveTo, entity.Status, now)) return NotEditable();
                    if (IsBackdated(model.EffectiveFrom, entity.EffectiveFrom)) return PastStart();
                    BuildTimeEntity(entity, model);
                    var all = (await _unitOfWork.PriceTimeConfigs.GetAllAsync()).ToList();
                    if (HasSameStart(all.Where(x => x.Status == PriceConfigStatus.Active && TimeKey(x) == TimeKey(entity)), x => x.EffectiveFrom, model.EffectiveFrom, model.Id))
                        return DuplicateStart();
                    _unitOfWork.PriceTimeConfigs.Update(entity);
                    await _unitOfWork.SaveChangesAsync();
                    await RechainTimeAsync();
                    break;
                }
            default:
                return Result.Failure("Loại cấu hình giá không hợp lệ.");
        }

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    // "Xóa" = soft-delete: đặt ngày kết thúc + status Inactive (KHÔNG xóa khỏi DB), rồi nối lại dòng thời gian.
    public async Task<Result> DeleteAsync(string kind, Guid id)
    {
        var now = DateTime.Now;
        switch (kind)
        {
            case PriceKind.Base:
                {
                    var e = await _unitOfWork.PriceBaseConfigs.GetByIdAsync(id);
                    if (e is null) return NotFound();
                    if (!CanModify(e.EffectiveFrom, e.EffectiveTo, e.Status, now)) return NotEndable();
                    e.Status = PriceConfigStatus.Inactive;
                    e.EffectiveTo = EndDate(e.EffectiveFrom, now);
                    _unitOfWork.PriceBaseConfigs.Update(e);
                    await _unitOfWork.SaveChangesAsync();
                    await RechainBaseAsync();
                    break;
                }
            case PriceKind.Room:
                {
                    var e = await _unitOfWork.PriceRoomTypeConfigs.GetByIdAsync(id);
                    if (e is null) return NotFound();
                    if (!CanModify(e.EffectiveFrom, e.EffectiveTo, e.Status, now)) return NotEndable();
                    e.Status = PriceConfigStatus.Inactive;
                    e.EffectiveTo = EndDate(e.EffectiveFrom, now);
                    _unitOfWork.PriceRoomTypeConfigs.Update(e);
                    await _unitOfWork.SaveChangesAsync();
                    await RechainRoomAsync();
                    break;
                }
            case PriceKind.Seat:
                {
                    var e = await _unitOfWork.PriceSeatConfigs.GetByIdAsync(id);
                    if (e is null) return NotFound();
                    if (!CanModify(e.EffectiveFrom, e.EffectiveTo, e.Status, now)) return NotEndable();
                    e.Status = PriceConfigStatus.Inactive;
                    e.EffectiveTo = EndDate(e.EffectiveFrom, now);
                    _unitOfWork.PriceSeatConfigs.Update(e);
                    await _unitOfWork.SaveChangesAsync();
                    await RechainSeatAsync();
                    break;
                }
            case PriceKind.Time:
                {
                    var e = await _unitOfWork.PriceTimeConfigs.GetByIdAsync(id);
                    if (e is null) return NotFound();
                    if (!CanModify(e.EffectiveFrom, e.EffectiveTo, e.Status, now)) return NotEndable();
                    e.Status = PriceConfigStatus.Inactive;
                    e.EffectiveTo = EndDate(e.EffectiveFrom, now);
                    _unitOfWork.PriceTimeConfigs.Update(e);
                    await _unitOfWork.SaveChangesAsync();
                    await RechainTimeAsync();
                    break;
                }
            default:
                return Result.Failure("Loại cấu hình giá không hợp lệ.");
        }

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    // ---------------------- Nối lại dòng thời gian (rechain) ----------------------
    // Chỉ tính trên các mức ĐANG ÁP DỤNG (Active). Mỗi nhóm đối tượng: sắp theo effective_from,
    // đặt effective_to = effective_from của mức sau (mức cuối = null). Các mức đã ngừng (Inactive) bị loại.

    private async Task RechainBaseAsync()
    {
        _unitOfWork.ClearTracking();
        var all = (await _unitOfWork.PriceBaseConfigs.GetAllAsync()).Where(x => x.Status == PriceConfigStatus.Active);
        foreach (var grp in all.GroupBy(x => x.MovieId))
        {
            var ordered = grp.OrderBy(x => x.EffectiveFrom).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].EffectiveTo = i < ordered.Count - 1 ? ordered[i + 1].EffectiveFrom : null;
                _unitOfWork.PriceBaseConfigs.Update(ordered[i]);
            }
        }
    }

    private async Task RechainRoomAsync()
    {
        _unitOfWork.ClearTracking();
        var all = (await _unitOfWork.PriceRoomTypeConfigs.GetAllAsync()).Where(x => x.Status == PriceConfigStatus.Active);
        foreach (var grp in all.GroupBy(x => x.RoomTypeId))
        {
            var ordered = grp.OrderBy(x => x.EffectiveFrom).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].EffectiveTo = i < ordered.Count - 1 ? ordered[i + 1].EffectiveFrom : null;
                _unitOfWork.PriceRoomTypeConfigs.Update(ordered[i]);
            }
        }
    }

    private async Task RechainSeatAsync()
    {
        _unitOfWork.ClearTracking();
        var all = (await _unitOfWork.PriceSeatConfigs.GetAllAsync()).Where(x => x.Status == PriceConfigStatus.Active);
        foreach (var grp in all.GroupBy(x => x.SeatTypeId))
        {
            var ordered = grp.OrderBy(x => x.EffectiveFrom).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].EffectiveTo = i < ordered.Count - 1 ? ordered[i + 1].EffectiveFrom : null;
                _unitOfWork.PriceSeatConfigs.Update(ordered[i]);
            }
        }
    }

    private async Task RechainTimeAsync()
    {
        _unitOfWork.ClearTracking();
        var all = (await _unitOfWork.PriceTimeConfigs.GetAllAsync()).Where(x => x.Status == PriceConfigStatus.Active);
        foreach (var grp in all.GroupBy(TimeKey))
        {
            var ordered = grp.OrderBy(x => x.EffectiveFrom).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].EffectiveTo = i < ordered.Count - 1 ? ordered[i + 1].EffectiveFrom : null;
                _unitOfWork.PriceTimeConfigs.Update(ordered[i]);
            }
        }
    }

    // ---------------------- Helpers ----------------------

    private static Result NotFound() => Result.Failure("Không tìm thấy cấu hình giá.");
    private static Result NotEditable() => Result.Failure("Không thể sửa cấu hình giá đã hết hạn hoặc đã ngừng áp dụng.");
    private static Result NotEndable() => Result.Failure("Cấu hình này đã hết hạn hoặc đã ngừng, không cần thao tác.");

    private static Result DuplicateStart() =>
        Result.Failure("Đã có cấu hình giá cho đối tượng này bắt đầu đúng thời điểm đó. Vui lòng chọn thời điểm áp dụng khác.");

    private static Result PastStart() => Result.Failure("Thời điểm áp dụng không được ở quá khứ.");

    // Khi Sửa: chỉ chặn nếu ĐỔI thời điểm áp dụng sang một mốc quá khứ mới (grace 1 phút).
    // Giữ nguyên mốc cũ (kể cả đã ở quá khứ với mức đang chạy) thì vẫn cho lưu.
    private static bool IsBackdated(DateTime newFrom, DateTime originalFrom)
        => newFrom != originalFrom && newFrom < DateTime.Now.AddMinutes(-1);

    private static bool CanModify(DateTime from, DateTime? to, string? status, DateTime now)
        => PriceDisplayStatus.CanModify(PriceDisplayStatus.Resolve(from, to, now, status));

    // Ngày kết thúc khi ngừng: nếu đã bắt đầu → kết thúc ngay bây giờ; nếu còn ở tương lai → để trống (hủy trước khi áp dụng).
    private static DateTime? EndDate(DateTime from, DateTime now) => now > from ? now : null;

    // Có cấu hình khác (khác id) cùng đối tượng bắt đầu đúng thời điểm này không?
    private static bool HasSameStart<T>(IEnumerable<T> group, Func<T, DateTime> startOf, DateTime start, Guid? excludeId)
        where T : class
    {
        return group.Any(x => startOf(x) == start && IdOf(x) != (excludeId ?? Guid.Empty));
    }

    private static Guid IdOf(object e) => e switch
    {
        PriceBaseConfig b => b.Id,
        PriceRoomTypeConfig r => r.Id,
        PriceSeatConfig s => s.Id,
        PriceTimeConfig t => t.Id,
        _ => Guid.Empty
    };

    // Khóa nhóm cho phụ thu theo giờ: cùng điều kiện => cùng một dòng thời gian (đổi mức theo thời gian).
    private static string TimeKey(PriceTimeConfig t) =>
        $"{t.RuleGroup}|{t.TimeCondition}|{t.DayOfWeek}|{t.SpecificDate}|{t.StartTime}|{t.EndTime}";

    private static PriceTimeConfig BuildTimeEntity(PriceTimeConfig entity, PriceConfigFormViewModel model)
    {
        entity.RuleGroup = model.RuleGroup;
        entity.TimeSurcharge = model.TimeSurcharge;
        entity.Priority = model.Priority;
        entity.EffectiveFrom = model.EffectiveFrom;

        if (model.RuleGroup == PriceRuleGroup.Time)
        {
            entity.TimeCondition = PriceTimeCondition.TimeRange;
            entity.StartTime = model.StartTime;
            entity.EndTime = model.EndTime;
            entity.DayOfWeek = null;
            entity.SpecificDate = null;
        }
        else // DAY
        {
            entity.TimeCondition = model.TimeCondition ?? PriceTimeCondition.DayOfWeek;
            entity.DayOfWeek = model.TimeCondition == PriceTimeCondition.DayOfWeek ? model.DayOfWeek : null;
            entity.SpecificDate = model.TimeCondition == PriceTimeCondition.SpecificDate ? model.SpecificDate : null;
            entity.StartTime = null;
            entity.EndTime = null;
        }
        return entity;
    }
}

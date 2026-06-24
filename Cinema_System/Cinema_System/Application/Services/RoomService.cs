using System.Text.Json;
using System.Text.RegularExpressions;
using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

public class RoomService : IRoomService
{
    // Web defaults: serialize ra camelCase + deserialize không phân biệt hoa thường
    // → khớp với field name JS (row/startColumn/seatTypeId).
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RoomService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<RoomDTO>> GetAllAsync()
    {
        var rooms = await _unitOfWork.Rooms.GetAllAsync(
            includeProperties: new[] { nameof(Room.RoomType) });

        var result = new List<RoomDTO>();
        foreach (var room in rooms)
        {
            var dto = _mapper.Map<RoomDTO>(room);
            dto.SeatCount = await _unitOfWork.Seats.CountAsync(s => s.RoomId == room.Id);
            dto.HasShowtimes = await _unitOfWork.Showtimes.ExistsAsync(s => s.RoomId == room.Id);
            dto.HasBookedTickets = await HasBookedTicketsAsync(room.Id);
            result.Add(dto);
        }

        // Sắp xếp: phòng đang dùng/bảo trì lên trước, phòng lưu trữ (Ngừng dùng) xuống cuối;
        // trong mỗi nhóm theo tên dạng số tự nhiên (Phòng 2 trước Phòng 10).
        return result
            .OrderBy(d => StatusRank(d.Status))
            .ThenBy(d => NaturalKey(d.Name), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int StatusRank(string? status) => status switch
    {
        RoomStatus.Active => 0,
        RoomStatus.Maintenance => 1,
        RoomStatus.Inactive => 2,
        _ => 3
    };

    /// <summary>Chuẩn hóa tên để sắp số tự nhiên: pad các cụm chữ số về 10 ký tự.</summary>
    private static string NaturalKey(string name) =>
        Regex.Replace(name, @"\d+", m => m.Value.PadLeft(10, '0'));

    public async Task<RoomFormViewModel> BuildCreateFormAsync()
    {
        var model = new RoomFormViewModel { SeatsJson = "[]" };
        await PopulateOptionsAsync(model);
        return model;
    }

    public async Task<RoomFormViewModel?> GetForEditAsync(Guid id)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(id);
        if (room is null) return null;

        var model = _mapper.Map<RoomFormViewModel>(room);
        var hasFuture = await HasFutureShowtimeAsync(id);
        model.Locked = hasFuture;
        model.WillCreateNewVersion = !hasFuture && await HasBookedTicketsAsync(id);
        model.SeatsJson = await BuildSeatsJsonAsync(id);
        await PopulateOptionsAsync(model);
        return model;
    }

    public async Task PopulateOptionsAsync(RoomFormViewModel model)
    {
        var roomTypes = await _unitOfWork.RoomTypes.GetAllAsync(orderBy: q => q.OrderBy(t => t.Name));
        model.RoomTypeOptions = roomTypes.Select(t => (t.Id, t.Name)).ToList();

        var seatTypes = await _unitOfWork.SeatTypes.GetAllAsync(
            orderBy: q => q.OrderBy(t => t.ColumnSpan).ThenBy(t => t.Name));
        model.SeatTypes = seatTypes.Select(t => _mapper.Map<SeatTypeOptionDTO>(t)).ToList();
    }

    public async Task<Result> CreateAsync(RoomFormViewModel model)
    {
        var cinema = await GetSingleCinemaAsync();
        if (cinema is null)
            return Result.Failure("Chưa có rạp nào trong hệ thống. Vui lòng tạo rạp trước.");

        var roomTypeId = model.RoomTypeId!.Value;
        if (!await _unitOfWork.RoomTypes.ExistsAsync(t => t.Id == roomTypeId))
            return Result.Failure("Loại phòng không tồn tại.");

        var name = model.Name.Trim();
        var nameTaken = await _unitOfWork.Rooms.ExistsAsync(
            r => r.CinemaId == cinema.Id && r.Name == name);
        if (nameTaken)
            return Result.Failure("Tên phòng đã tồn tại trong rạp.");

        var seatsResult = await BuildSeatsAsync(model.TotalRow, model.TotalColumns, model.SeatsJson);
        if (!seatsResult.Succeeded)
            return Result.Failure(seatsResult.Error!);
        var drafts = seatsResult.Data!;

        var roomId = Guid.NewGuid();
        var room = new Room
        {
            Id = roomId,
            CinemaId = cinema.Id,
            Name = name,
            RoomTypeId = roomTypeId,
            Status = model.Status,
            TotalRow = model.TotalRow,
            TotalColumns = model.TotalColumns,
            TotalSeats = drafts.Count > 0 ? drafts.Count : null
        };

        await _unitOfWork.Rooms.AddAsync(room);
        foreach (var d in drafts)
            await _unitOfWork.Seats.AddAsync(ToSeat(roomId, d));

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<string>> UpdateAsync(RoomFormViewModel model)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(model.Id);
        if (room is null)
            return Result<string>.Failure("Không tìm thấy phòng.");

        var roomTypeId = model.RoomTypeId!.Value;
        if (!await _unitOfWork.RoomTypes.ExistsAsync(t => t.Id == roomTypeId))
            return Result<string>.Failure("Loại phòng không tồn tại.");

        var name = model.Name.Trim();
        var nameTaken = await _unitOfWork.Rooms.ExistsAsync(
            r => r.CinemaId == room.CinemaId && r.Name == name && r.Id != room.Id);
        if (nameTaken)
            return Result<string>.Failure("Tên phòng đã tồn tại trong rạp.");

        // CÓ suất chiếu/vé trong TƯƠNG LAI → chỉ cho sửa thông tin, GIỮ NGUYÊN sơ đồ ghế & kích thước.
        if (await HasFutureShowtimeAsync(room.Id))
        {
            room.Name = name;
            room.RoomTypeId = roomTypeId;
            room.Status = model.Status;
            _unitOfWork.Rooms.Update(room);
            await _unitOfWork.SaveChangesAsync();
            return Result<string>.Success(
                "Phòng có suất chiếu sắp tới nên chỉ cập nhật tên/loại/trạng thái; sơ đồ ghế giữ nguyên.");
        }

        var seatsResult = await BuildSeatsAsync(model.TotalRow, model.TotalColumns, model.SeatsJson);
        if (!seatsResult.Succeeded)
            return Result<string>.Failure(seatsResult.Error!);
        var drafts = seatsResult.Data!;

        // CHỈ có vé ở suất chiếu QUÁ KHỨ.
        if (await HasBookedTicketsAsync(room.Id))
        {
            var current = await _unitOfWork.Seats.GetAllAsync(s => s.RoomId == room.Id);
            var dimsSame = room.TotalRow == model.TotalRow && room.TotalColumns == model.TotalColumns;
            var seatsSame = SeatLayoutEquals(current, drafts);

            // Chỉ đổi tên/loại/trạng thái, KHÔNG đụng sơ đồ ghế/kích thước → cập nhật tại chỗ.
            if (dimsSame && seatsSame)
            {
                room.Name = name;
                room.RoomTypeId = roomTypeId;
                room.Status = model.Status;
                _unitOfWork.Rooms.Update(room);
                await _unitOfWork.SaveChangesAsync();
                return Result<string>.Success("Cập nhật phòng chiếu thành công.");
            }

            // Có thay đổi sơ đồ ghế/kích thước → lưu trữ phòng cũ + tạo phòng MỚI (giữ lịch sử vé cũ).
            return await VersionRoomAsync(room, name, roomTypeId, model, drafts);
        }

        // KHÔNG có vé nào → sửa tại chỗ, thay toàn bộ ghế.
        room.Name = name;
        room.RoomTypeId = roomTypeId;
        room.Status = model.Status;
        room.TotalRow = model.TotalRow;
        room.TotalColumns = model.TotalColumns;
        room.TotalSeats = drafts.Count > 0 ? drafts.Count : null;
        _unitOfWork.Rooms.Update(room);

        var existing = await _unitOfWork.Seats.GetAllAsync(s => s.RoomId == room.Id);
        foreach (var seat in existing)
            _unitOfWork.Seats.Remove(seat);
        foreach (var d in drafts)
            await _unitOfWork.Seats.AddAsync(ToSeat(room.Id, d));

        await _unitOfWork.SaveChangesAsync();
        return Result<string>.Success("Cập nhật phòng chiếu thành công.");
    }

    /// <summary>
    /// Lưu trữ phòng cũ (đổi tên kèm mã ngày-giờ + ngừng dùng, GIỮ ghế cũ cho lịch sử vé) rồi
    /// tạo phòng mới mang cấu hình vừa sửa. Tách 2 lần SaveChanges để tránh đụng unique tên phòng.
    /// </summary>
    private async Task<Result<string>> VersionRoomAsync(
        Room oldRoom, string newName, Guid roomTypeId, RoomFormViewModel model, List<SeatInputDTO> drafts)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var archivedName = BuildArchivedName(oldRoom.Name, stamp);

        oldRoom.Name = archivedName;
        oldRoom.Status = RoomStatus.Inactive;
        _unitOfWork.Rooms.Update(oldRoom);
        await _unitOfWork.SaveChangesAsync();

        var newId = Guid.NewGuid();
        var newRoom = new Room
        {
            Id = newId,
            CinemaId = oldRoom.CinemaId,
            Name = newName,
            RoomTypeId = roomTypeId,
            Status = model.Status,
            TotalRow = model.TotalRow,
            TotalColumns = model.TotalColumns,
            TotalSeats = drafts.Count > 0 ? drafts.Count : null
        };

        await _unitOfWork.Rooms.AddAsync(newRoom);
        foreach (var d in drafts)
            await _unitOfWork.Seats.AddAsync(ToSeat(newId, d));

        await _unitOfWork.SaveChangesAsync();
        return Result<string>.Success(
            $"Phòng cũ đã có vé ở suất chiếu trước đây nên được lưu trữ thành \"{archivedName}\" (Ngừng dùng); " +
            "đã tạo phòng mới với cấu hình bạn vừa chỉnh.");
    }

    /// <summary>Ghép tên lưu trữ "{tên} (lưu trữ {stamp})", cắt bớt để vừa NVARCHAR(100).</summary>
    private static string BuildArchivedName(string currentName, string stamp)
    {
        const int max = 100;
        var suffix = $" (lưu trữ {stamp})";
        var keep = Math.Max(0, max - suffix.Length);
        var baseName = currentName.Length > keep ? currentName[..keep] : currentName;
        return baseName + suffix;
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(id);
        if (room is null)
            return Result.Failure("Không tìm thấy phòng.");

        if (await _unitOfWork.Showtimes.ExistsAsync(s => s.RoomId == id))
            return Result.Failure("Không thể xóa: phòng đang có suất chiếu.");
        if (await HasBookedTicketsAsync(id))
            return Result.Failure("Không thể xóa: phòng đã có vé được đặt.");

        var seats = await _unitOfWork.Seats.GetAllAsync(s => s.RoomId == id);
        foreach (var seat in seats)
            _unitOfWork.Seats.Remove(seat);

        _unitOfWork.Rooms.Remove(room);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    // ---- helpers ----

    /// <summary>Parse + validate sơ đồ ghế từ JSON theo kích thước phòng và span loại ghế.</summary>
    private async Task<Result<List<SeatInputDTO>>> BuildSeatsAsync(int rows, int cols, string? seatsJson)
    {
        List<SeatInputDTO> items;
        try
        {
            items = string.IsNullOrWhiteSpace(seatsJson)
                ? new List<SeatInputDTO>()
                : JsonSerializer.Deserialize<List<SeatInputDTO>>(seatsJson, JsonOpts) ?? new();
        }
        catch (JsonException)
        {
            return Result<List<SeatInputDTO>>.Failure("Dữ liệu sơ đồ ghế không hợp lệ.");
        }

        var seatTypes = await _unitOfWork.SeatTypes.GetAllAsync();
        var spanById = seatTypes.ToDictionary(t => t.Id, t => t.ColumnSpan);

        var occupied = new bool[rows + 1, cols + 1];
        foreach (var item in items)
        {
            if (!spanById.TryGetValue(item.SeatTypeId, out var span))
                return Result<List<SeatInputDTO>>.Failure("Có ghế dùng loại ghế không tồn tại.");

            if (item.Row < 1 || item.Row > rows)
                return Result<List<SeatInputDTO>>.Failure("Có ghế nằm ngoài phạm vi số hàng của phòng.");
            if (item.StartColumn < 1 || item.StartColumn + span - 1 > cols)
                return Result<List<SeatInputDTO>>.Failure("Có ghế nằm ngoài phạm vi số cột của phòng.");

            for (int c = item.StartColumn; c <= item.StartColumn + span - 1; c++)
            {
                if (occupied[item.Row, c])
                    return Result<List<SeatInputDTO>>.Failure("Có ghế bị chồng lấn lên nhau. Vui lòng kiểm tra lại sơ đồ.");
                occupied[item.Row, c] = true;
            }
        }

        return Result<List<SeatInputDTO>>.Success(items);
    }

    /// <summary>So khớp sơ đồ ghế hiện có (DB) với sơ đồ mới (drafts) — coi như tập (hàng, cột bắt đầu, loại ghế).</summary>
    private static bool SeatLayoutEquals(IEnumerable<Seat> existing, List<SeatInputDTO> drafts)
    {
        var a = existing.Select(s => (s.RowNumber, s.SeatNumber, s.SeatTypeId)).ToHashSet();
        var b = drafts.Select(d => (d.Row, d.StartColumn, d.SeatTypeId)).ToHashSet();
        return a.Count == b.Count && a.SetEquals(b);
    }

    private static Seat ToSeat(Guid roomId, SeatInputDTO d) => new()
    {
        Id = Guid.NewGuid(),
        RoomId = roomId,
        SeatTypeId = d.SeatTypeId,
        RowNumber = d.Row,
        SeatNumber = d.StartColumn,
        Status = SeatStatus.Available
    };

    private async Task<string> BuildSeatsJsonAsync(Guid roomId)
    {
        var seats = await _unitOfWork.Seats.GetAllAsync(s => s.RoomId == roomId);
        var items = seats.Select(s => new SeatInputDTO
        {
            Row = s.RowNumber,
            StartColumn = s.SeatNumber,
            SeatTypeId = s.SeatTypeId
        });
        // Dùng JsonOpts (camelCase) để khớp field JS khi trình thiết kế vẽ lại ghế cũ.
        return JsonSerializer.Serialize(items, JsonOpts);
    }

    private async Task<Cinema?> GetSingleCinemaAsync()
    {
        // Dự án chỉ vận hành 1 rạp → lấy rạp đầu tiên (theo tên) làm rạp mặc định.
        var cinemas = await _unitOfWork.Cinemas.GetAllAsync(orderBy: q => q.OrderBy(c => c.Name));
        return cinemas.FirstOrDefault();
    }

    private async Task<bool> HasBookedTicketsAsync(Guid roomId)
    {
        var seatIds = (await _unitOfWork.Seats.GetAllAsync(s => s.RoomId == roomId))
            .Select(s => s.Id).ToHashSet();
        if (seatIds.Count == 0) return false;

        return await _unitOfWork.Tickets.ExistsAsync(t => seatIds.Contains(t.SeatId));
    }

    /// <summary>Phòng có suất chiếu đang/sẽ diễn ra (chưa kết thúc) → đang được cam kết cho khách.</summary>
    private async Task<bool> HasFutureShowtimeAsync(Guid roomId)
    {
        var now = DateTime.Now;
        return await _unitOfWork.Showtimes.ExistsAsync(s => s.RoomId == roomId && s.EndTime > now);
    }
}

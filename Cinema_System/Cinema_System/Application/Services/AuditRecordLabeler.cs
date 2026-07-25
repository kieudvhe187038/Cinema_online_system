using System.Text.Json;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

/// <summary>
/// Dịch (bảng + record_id) của nhật ký thành nhãn dễ đọc — vd "Phim: Avengers" thay vì một GUID.
/// Tra theo LÔ: mỗi bảng chỉ một truy vấn cho cả trang danh sách.
/// Bản ghi đã bị xóa không tra được thì lấy tên từ chính JSON đã lưu trong log.
/// </summary>
internal sealed class AuditRecordLabeler
{
    // Khóa JSON thường mang tên gợi nhớ của bản ghi, ưu tiên từ trái sang phải.
    private static readonly string[] NameKeys =
    {
        "title", "full_name", "name", "code", "config_key", "email", "qr_code"
    };

    private readonly IUnitOfWork _unitOfWork;

    public AuditRecordLabeler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>Nhãn của từng dòng log, khóa theo Id của dòng log (thiếu khóa = không xác định được).</summary>
    public async Task<Dictionary<Guid, string>> ResolveAsync(IReadOnlyCollection<AuditLog> logs)
    {
        var labels = new Dictionary<Guid, string>();

        var groups = logs
            .Where(log => log.RecordId.HasValue && !string.IsNullOrWhiteSpace(log.TableName))
            .GroupBy(log => log.TableName!, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var ids = group.Select(log => log.RecordId!.Value).Distinct().ToList();
            var names = await LookupAsync(group.Key, ids);

            foreach (var log in group)
            {
                if (names.TryGetValue(log.RecordId!.Value, out var name) && !string.IsNullOrWhiteSpace(name))
                    labels[log.Id] = name;
            }
        }

        // Bản ghi đã xóa (hoặc bảng chưa có quy tắc đặt tên) -> lấy tạm tên trong JSON của log.
        foreach (var log in logs.Where(log => !labels.ContainsKey(log.Id)))
        {
            var fromSnapshot = NameFromSnapshot(log.NewValue) ?? NameFromSnapshot(log.OldValue);
            if (fromSnapshot != null) labels[log.Id] = fromSnapshot;
        }

        return labels;
    }

    // Tra tên hiển thị theo từng bảng. Bảng không liệt kê ở đây sẽ dùng tên lấy từ JSON.
    private async Task<Dictionary<Guid, string>> LookupAsync(string tableName, List<Guid> ids)
    {
        switch (tableName.ToLowerInvariant())
        {
            case "movies":
                return (await _unitOfWork.Movies.GetAllAsync(m => ids.Contains(m.Id)))
                    .ToDictionary(m => m.Id, m => $"Phim: {m.Title}");

            case "users":
                return (await _unitOfWork.Users.GetAllAsync(u => ids.Contains(u.Id)))
                    .ToDictionary(u => u.Id, u => $"Người dùng: {u.FullName} ({u.Email})");

            case "showtimes":
                return (await _unitOfWork.Showtimes.GetAllAsync(
                        predicate: s => ids.Contains(s.Id),
                        includeProperties: new[] { "Movie", "Room" }))
                    .ToDictionary(s => s.Id,
                        s => $"Suất chiếu: {s.Movie.Title} — {s.Room.Name} — {s.StartTime:HH:mm dd/MM/yyyy}");

            case "rooms":
                return (await _unitOfWork.Rooms.GetAllAsync(r => ids.Contains(r.Id)))
                    .ToDictionary(r => r.Id, r => $"Phòng chiếu: {r.Name}");

            case "room_types":
                return (await _unitOfWork.RoomTypes.GetAllAsync(rt => ids.Contains(rt.Id)))
                    .ToDictionary(rt => rt.Id, rt => $"Loại phòng: {rt.Name}");

            case "seat_types":
                return (await _unitOfWork.SeatTypes.GetAllAsync(st => ids.Contains(st.Id)))
                    .ToDictionary(st => st.Id, st => $"Loại ghế: {st.Name}");

            case "seats":
                return (await _unitOfWork.Seats.GetAllAsync(
                        predicate: s => ids.Contains(s.Id),
                        includeProperties: new[] { "Room" }))
                    .ToDictionary(s => s.Id,
                        s => $"Ghế: hàng {s.RowNumber} số {s.SeatNumber} (phòng {s.Room.Name})");

            case "genres":
                return (await _unitOfWork.Genres.GetAllAsync(g => ids.Contains(g.Id)))
                    .ToDictionary(g => g.Id, g => $"Thể loại: {g.Name}");

            case "food_beverages":
                return (await _unitOfWork.FoodBeverages.GetAllAsync(f => ids.Contains(f.Id)))
                    .ToDictionary(f => f.Id, f => $"Món: {f.Name}");

            case "promotions":
                return (await _unitOfWork.Promotions.GetAllAsync(p => ids.Contains(p.Id)))
                    .ToDictionary(p => p.Id, p => $"Khuyến mãi: {p.Code}");

            case "cinemas":
                return (await _unitOfWork.Cinemas.GetAllAsync(c => ids.Contains(c.Id)))
                    .ToDictionary(c => c.Id, c => $"Rạp: {c.Name}");

            // SystemConfig khóa chính là config_key (chuỗi) nên log không có record_id —
            // nhãn lấy từ khóa "config_key" trong JSON qua NameFromSnapshot.

            case "bookings":
                return (await _unitOfWork.Bookings.GetAllAsync(
                        predicate: b => ids.Contains(b.Id),
                        includeProperties: new[] { "Showtime.Movie" }))
                    .ToDictionary(b => b.Id,
                        b => $"Đơn vé: {b.QrCode ?? b.Id.ToString()[..8]} — {b.Showtime.Movie.Title}");

            case "roles":
                return (await _unitOfWork.Roles.GetAllAsync(r => ids.Contains(r.Id)))
                    .ToDictionary(r => r.Id, r => $"Vai trò: {r.Name}");

            case "vat":
                return (await _unitOfWork.Vats.GetAllAsync(v => ids.Contains(v.Id)))
                    .ToDictionary(v => v.Id, v => $"VAT: {v.VatRate:P0}{(string.IsNullOrWhiteSpace(v.Description) ? "" : $" — {v.Description}")}");

            case "price_base_configs":
                return (await _unitOfWork.PriceBaseConfigs.GetAllAsync(
                        predicate: p => ids.Contains(p.Id),
                        includeProperties: new[] { "Movie" }))
                    .ToDictionary(p => p.Id,
                        p => $"Giá cơ bản: {(p.Movie != null ? p.Movie.Title : "áp dụng chung")}");

            case "price_room_type_configs":
                return (await _unitOfWork.PriceRoomTypeConfigs.GetAllAsync(
                        predicate: p => ids.Contains(p.Id),
                        includeProperties: new[] { "RoomType" }))
                    .ToDictionary(p => p.Id, p => $"Phụ thu loại phòng: {p.RoomType.Name}");

            case "price_seat_configs":
                return (await _unitOfWork.PriceSeatConfigs.GetAllAsync(
                        predicate: p => ids.Contains(p.Id),
                        includeProperties: new[] { "SeatType" }))
                    .ToDictionary(p => p.Id, p => $"Phụ thu loại ghế: {p.SeatType.Name}");

            case "price_time_configs":
                return (await _unitOfWork.PriceTimeConfigs.GetAllAsync(p => ids.Contains(p.Id)))
                    .ToDictionary(p => p.Id, p => $"Phụ thu khung giờ: {p.TimeCondition}");

            case "reviews":
                return (await _unitOfWork.Reviews.GetAllAsync(
                        predicate: r => ids.Contains(r.Id),
                        includeProperties: new[] { "Movie", "User" }))
                    .ToDictionary(r => r.Id, r => $"Đánh giá: {r.Movie.Title} — {r.User.FullName} ({r.Rating}★)");

            case "showtimeincidents":
                return (await _unitOfWork.ShowtimeIncidents.GetAllAsync(
                        predicate: i => ids.Contains(i.Id),
                        includeProperties: new[] { "Showtime.Movie" }))
                    .ToDictionary(i => i.Id,
                        i => $"Sự cố suất chiếu: {(i.Showtime != null ? i.Showtime.Movie.Title : "—")}");

            default:
                return new Dictionary<Guid, string>();
        }
    }

    // Lấy tên gợi nhớ từ JSON old_value/new_value (dùng khi bản ghi gốc đã bị xóa).
    private static string? NameFromSnapshot(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;

            foreach (var key in NameKeys)
            {
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (!string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase)) continue;
                    if (property.Value.ValueKind != JsonValueKind.String) continue;

                    var value = property.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(value)) return value;
                }
            }
        }
        catch (JsonException)
        {
            // JSON hỏng thì coi như không có nhãn.
        }

        return null;
    }
}

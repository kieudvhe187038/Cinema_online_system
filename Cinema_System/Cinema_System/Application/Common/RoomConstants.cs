namespace Cinema_System.Application.Common;

/// <summary>
/// Hằng số cho module Quản lý Phòng chiếu. Giá trị khớp CHECK constraint
/// <c>CK_Rooms_status</c> và <c>CK_Seats_status</c> trong DB.
/// </summary>
public static class RoomStatus
{
    public const string Active = "Active";
    public const string Maintenance = "Maintenance";
    public const string Inactive = "Inactive";

    public static readonly IReadOnlySet<string> AllowedValues =
        new HashSet<string>(StringComparer.Ordinal) { Active, Maintenance, Inactive };

    public static string Label(string? status) => status switch
    {
        Active => "Đang hoạt động",
        Maintenance => "Bảo trì",
        Inactive => "Ngừng dùng",
        _ => status ?? "—"
    };
}

public static class SeatStatus
{
    public const string Available = "Available";
    public const string Broken = "Broken";
    public const string Reserved = "Reserved";
}

public static class RoomPolicy
{
    /// <summary>Số hàng / số cột tối đa cho phép khi dựng sơ đồ ghế.</summary>
    public const int MaxRows = 40;
    public const int MaxColumns = 40;
}

public static class RoomPaging
{
    public const int DefaultPageSize = 8;
}

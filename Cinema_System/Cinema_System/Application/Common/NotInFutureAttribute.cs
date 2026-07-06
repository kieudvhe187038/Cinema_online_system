using System.ComponentModel.DataAnnotations;

namespace Cinema_System.Application.Common;

/// <summary>
/// Chặn ngày nằm trong tương lai (so với hôm nay). Dùng cho các cột có CHECK constraint
/// kiểu <c>value &lt;= GETDATE()</c> ở DB (vd Users.date_of_birth) — validate phía server
/// để báo lỗi thân thiện thay vì để DbUpdateException ném ra lỗi 500.
/// Hỗ trợ <see cref="DateOnly"/>, <see cref="DateTime"/> và phiên bản nullable của chúng.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotInFutureAttribute : ValidationAttribute
{
    public NotInFutureAttribute()
        : base("Ngày không được nằm trong tương lai.") { }

    public override bool IsValid(object? value)
    {
        if (value is null) return true; // để [Required] lo phần bắt buộc

        var today = DateTime.Today;

        return value switch
        {
            DateOnly d => d <= DateOnly.FromDateTime(today),
            DateTime dt => dt.Date <= today,
            _ => true
        };
    }
}

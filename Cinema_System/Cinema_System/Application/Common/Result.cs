namespace Cinema_System.Application.Common;

/// <summary>
/// Kết quả trả về chuẩn cho tầng Application/Service.
/// Giúp Controller phân biệt thành công/thất bại mà không cần ném exception.
/// </summary>
public class Result
{
    /// <summary>True nếu thao tác thành công.</summary>
    public bool Succeeded { get; protected set; }

    /// <summary>Thông báo lỗi khi thất bại; null khi thành công.</summary>
    public string? Error { get; protected set; }

    /// <summary>Tạo kết quả thành công.</summary>
    public static Result Success() => new() { Succeeded = true };

    /// <summary>Tạo kết quả thất bại kèm thông báo lỗi.</summary>
    public static Result Failure(string error) => new() { Succeeded = false, Error = error };
}

/// <summary>Kết quả nghiệp vụ có kèm dữ liệu trả về khi thành công.</summary>
public class Result<T> : Result
{
    /// <summary>Dữ liệu trả về khi thành công; mặc định khi thất bại.</summary>
    public T? Data { get; private set; }

    /// <summary>Tạo kết quả thành công kèm dữ liệu.</summary>
    public static Result<T> Success(T data) => new() { Succeeded = true, Data = data };

    /// <summary>Tạo kết quả thất bại kèm thông báo lỗi.</summary>
    public static new Result<T> Failure(string error) => new() { Succeeded = false, Error = error };
}

namespace Cinema_System.Application.Common;

/// <summary>
/// Kết quả trả về chuẩn cho tầng Application/Service.
/// Giúp Controller phân biệt thành công/thất bại mà không cần ném exception.
/// </summary>
public class Result<T>
{
    public bool Succeeded { get; private set; }

    public string? Error { get; private set; }

    public T? Data { get; private set; }

    private Result(bool succeeded, T? data, string? error)
    {
        Succeeded = succeeded;
        Data = data;
        Error = error;
    }

    public static Result<T> Success(T data) => new(true, data, null);

    public static Result<T> Failure(string error) => new(false, default, error);
}

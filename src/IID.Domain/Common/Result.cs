namespace IID.Domain.Common;

public enum ErrorKind
{
    None = 0,
    ValidationFailed = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    Internal = 6
}

public readonly record struct Result<T>(bool IsSuccess, T? Value, ErrorKind ErrorKind, string? Message)
{
    public static Result<T> Success(T value) => new(true, value, ErrorKind.None, null);
    public static Result<T> Failure(ErrorKind kind, string message) => new(false, default, kind, message);

    public static implicit operator Result<T>(T value) => Success(value);
}

public readonly record struct Result(bool IsSuccess, ErrorKind ErrorKind, string? Message)
{
    public static Result Success() => new(true, ErrorKind.None, null);
    public static Result Failure(ErrorKind kind, string message) => new(false, kind, message);
}

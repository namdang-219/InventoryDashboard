namespace IID.Application.Common.Results;

public static class ResultExtensions
{
    public static int ToStatusCode(this ErrorKind kind) => kind switch
    {
        ErrorKind.ValidationFailed => 422,
        ErrorKind.NotFound => 404,
        ErrorKind.Conflict => 409,
        ErrorKind.Unauthorized => 401,
        ErrorKind.Forbidden => 403,
        ErrorKind.Internal => 500,
        _ => 200
    };
}

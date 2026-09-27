namespace IID.Application.Common.Results;

public static class ResultMapper
{
    public static int ToStatus(ErrorKind k) => k switch
    {
        ErrorKind.ValidationFailed => 422,
        ErrorKind.NotFound => 404,
        ErrorKind.Conflict => 409,
        ErrorKind.Unauthorized => 401,
        ErrorKind.Forbidden => 403,
        ErrorKind.Internal => 500,
        _ => 500
    };
}

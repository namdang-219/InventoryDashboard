namespace IID.Application.Common.Models;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int Limit, int Total)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / Limit);
}

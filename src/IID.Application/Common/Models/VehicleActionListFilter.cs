namespace IID.Application.Common.Models;

/// <summary>
/// Parameters for querying vehicle actions with pagination.
/// </summary>
public sealed record VehicleActionListFilter(
    Guid? VehicleId = null,
    int Page = 1,
    int Limit = 20,
    string? ActionType = null,
    string? Search = null);

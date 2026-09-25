namespace IID.Application.Vehicles.Commands.MarkVehicleSold;

/// <summary>
/// Marks a vehicle as sold. <see cref="RowVersionBase64"/> is the Base64-encoded
/// rowVersion returned by the previous GET; if it doesn't match the current DB
/// row, the handler returns <see cref="ErrorKind.Conflict"/> (HTTP 409).
/// </summary>
public sealed record MarkVehicleSoldCommand(
    Guid VehicleId,
    decimal SoldPrice,
    string? SoldPriceCurrency = null,
    DateTimeOffset? SoldAtUtc = null,
    string? RowVersionBase64 = null,
    byte[]? RowVersion = null) : MediatR.IRequest<Result<Guid>>;

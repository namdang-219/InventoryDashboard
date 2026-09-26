using FastEndpoints;
using IID.Application.Vehicles.Commands.MarkVehicleSold;
using MediatR;

namespace IID.Api.Endpoints.Vehicles;

/// <summary>
/// POST /api/v1/vehicles/{id}/mark-sold — transitions a vehicle to Sold.
/// Requires the <c>rowVersion</c> returned by the previous GET for optimistic
/// concurrency. Returns 409 Conflict if another writer modified the row.
/// </summary>
public sealed class MarkVehicleSoldEndpoint(ISender sender) : Endpoint<MarkVehicleSoldRequest, object>
{
    public override void Configure()
    {
        Post("/api/v1/vehicles/{id}/mark-sold");
        Roles("Sales", "Saler");
        Description(x => x.WithTags("Vehicles"));
        Summary(s =>
        {
            s.Summary = "Mark a vehicle as sold (Saler only)";
            s.Description = "Mark a vehicle as sold (Saler only). Optimistic concurrency via rowVersion.";
            s.Response(200, "Vehicle marked as sold.");
            s.Response(403, "Forbidden. Only Salers can mark vehicles as sold.");
            s.Response(404, "Vehicle not found.");
            s.Response(409, "Concurrency conflict or already sold.");
            s.Response(422, "Validation failed (e.g. missing rowVersion).");
        });
    }

    public override async Task HandleAsync(MarkVehicleSoldRequest req, CancellationToken ct)
    {
        var cmd = new MarkVehicleSoldCommand(
            Route<Guid>("id"),
            req.SoldPrice,
            req.SoldPriceCurrency,
            req.SoldAtUtc,
            req.RowVersion);

        var result = await sender.Send(cmd, ct);
        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to mark vehicle as sold.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        await Send.OkAsync(new { data = new { id = result.Value } }, ct);
    }
}

public sealed class MarkVehicleSoldRequest
{
    public decimal SoldPrice { get; set; }
    public string? SoldPriceCurrency { get; set; }
    public DateTimeOffset? SoldAtUtc { get; set; }

    /// <summary>
    /// Base64-encoded rowVersion from the previous GET response.
    /// Required for optimistic concurrency control.
    /// </summary>
    public string? RowVersion { get; set; }
}

using IID.Domain.Common;

namespace IID.Domain.Activities;

public sealed class InventoryActivity : Entity
{
    public ActivityType Type { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string? Make { get; private set; }
    public Guid? VehicleId { get; private set; }
    public DateTimeOffset OccurredOnUtc { get; private set; }

    private InventoryActivity() { }

    public static InventoryActivity Create(
        ActivityType type,
        string message,
        string? make = null,
        Guid? vehicleId = null)
    {
        return new InventoryActivity
        {
            Id = Guid.NewGuid(),
            Type = type,
            Message = message,
            Make = make,
            VehicleId = vehicleId,
            OccurredOnUtc = DateTimeOffset.UtcNow
        };
    }
}

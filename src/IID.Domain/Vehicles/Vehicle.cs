using IID.Domain.Common;
using IID.Domain.Vehicles.Events;

namespace IID.Domain.Vehicles;

/// <summary>
/// Vehicle aggregate root. Authoritative for the inventory state of a single unit.
/// Invariants are checked at construction time only; mutations go through method calls.
/// </summary>
public sealed class Vehicle : AggregateRoot
{
    public Guid DealershipId { get; private set; }
    public Dealerships.Dealership? Dealership { get; private set; }
    public Vin Vin { get; private set; }
    public string? StockNumber { get; private set; }
    public string Make { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public string Color { get; private set; } = string.Empty;
    public int Mileage { get; private set; }
    public FuelType FuelType { get; private set; }
    public Money PurchasePrice { get; private set; } = null!;
    public Money AskingPrice { get; private set; } = null!;
    public Money? SoldPrice { get; private set; }
    public DateTimeOffset? SoldAt { get; private set; }
    public VehicleStatus Status { get; private set; }
    public DateTimeOffset DateAddedToInventory { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public string? CreatedByUserId { get; private set; }
    public string? UpdatedByUserId { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public byte[] RowVersion { get; set; } = [];

    private Vehicle() { } // EF Core

    public static Vehicle Create(
        Vin vin,
        string make,
        string model,
        int year,
        string color,
        int mileage,
        FuelType fuelType,
        Money purchasePrice,
        Money askingPrice,
        VehicleStatus status,
        DateTimeOffset dateAddedToInventory,
        DateTimeOffset nowUtc,
        string? createdByUserId = null,
        string? stockNumber = null,
        Guid? dealershipId = null)
    {
        Validate(make, model, year, color, mileage, dateAddedToInventory, nowUtc);

        var normalizedVin = vin.Value;
        var v = new Vehicle
        {
            Id = Guid.NewGuid(),
            DealershipId = dealershipId ?? Guid.Empty,
            Vin = vin,
            StockNumber = string.IsNullOrWhiteSpace(stockNumber) ? GenerateStockNumber(normalizedVin) : stockNumber.Trim().ToUpperInvariant(),
            Make = make.Trim(),
            Model = model.Trim(),
            Year = year,
            Color = color.Trim(),
            Mileage = mileage,
            FuelType = fuelType,
            PurchasePrice = purchasePrice,
            AskingPrice = askingPrice,
            Status = status,
            DateAddedToInventory = dateAddedToInventory,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
            CreatedByUserId = createdByUserId,
            UpdatedByUserId = createdByUserId
        };

        v.RaiseDomainEvent(new VehicleAdded(v.Id, v.Make, v.Model, v.Status));
        return v;
    }

    public void TransferDealership(Guid newDealershipId, DateTimeOffset nowUtc, string? updatedByUserId = null)
    {
        EnsureNotSold();
        if (newDealershipId == Guid.Empty)
            throw new ArgumentException("Dealership ID cannot be empty.", nameof(newDealershipId));
        if (DealershipId == newDealershipId)
            throw new InvalidOperationException("Vehicle is already assigned to this dealership.");

        DealershipId = newDealershipId;
        UpdatedAt = nowUtc;
        if (!string.IsNullOrWhiteSpace(updatedByUserId))
            UpdatedByUserId = updatedByUserId;
        RaiseDomainEvent(new VehicleUpdated(Id, Make));
    }

    public void Update(
        string make,
        string model,
        int year,
        string color,
        int mileage,
        FuelType fuelType,
        Money purchasePrice,
        Money askingPrice,
        VehicleStatus status,
        DateTimeOffset nowUtc,
        string updatedByUserId)
    {
        EnsureNotSold();
        Validate(make, model, year, color, mileage, DateAddedToInventory, nowUtc);

        var previous = Status;
        Make = make.Trim();
        Model = model.Trim();
        Year = year;
        Color = color.Trim();
        Mileage = mileage;
        FuelType = fuelType;
        PurchasePrice = purchasePrice;
        AskingPrice = askingPrice;
        Status = status;
        UpdatedAt = nowUtc;
        UpdatedByUserId = updatedByUserId;

        RaiseDomainEvent(new VehicleUpdated(Id, Make));
        if (previous != status)
            RaiseDomainEvent(new VehicleStatusChanged(Id, previous, status, Make));
    }

    /// <summary>
    /// Transitions to Sold and raises domain event. Idempotent: returns silently if already sold.
    /// </summary>
    public void MarkSold(Money soldPrice, DateTimeOffset soldAtUtc, string updatedByUserId)
    {
        if (Status == VehicleStatus.Sold) return;
        EnsureNotWholesale();

        var previous = Status;
        Status = VehicleStatus.Sold;
        SoldPrice = soldPrice;
        SoldAt = soldAtUtc;
        UpdatedAt = soldAtUtc;
        UpdatedByUserId = updatedByUserId;

        RaiseDomainEvent(new VehicleStatusChanged(Id, previous, Status, Make));
        RaiseDomainEvent(new VehicleSold(Id, Make, soldAtUtc, DaysInInventory(soldAtUtc)));
    }

    public void SoftDelete(DateTimeOffset nowUtc, string deletedByUserId)
    {
        if (DeletedAt.HasValue) return;
        DeletedAt = nowUtc;
        UpdatedAt = nowUtc;
        UpdatedByUserId = deletedByUserId;
        RaiseDomainEvent(new VehicleRemoved(Id, Make));
    }

    // ── Domain logic / computed ───────────────────────────────────────────────

    /// <summary>Profit margin from purchase to sale (positive = gain).</summary>
    public Money? GrossProfit =>
        SoldPrice is null ? null : Money.Of(SoldPrice.Amount - PurchasePrice.Amount, SoldPrice.Currency);

    /// <summary>Markup on top of purchase price; null if sold price not set.</summary>
    public decimal? GrossMarginPercent =>
        SoldPrice is null || PurchasePrice.Amount == 0
            ? null
            : Math.Round(((SoldPrice.Amount - PurchasePrice.Amount) / PurchasePrice.Amount) * 100m, 2);

    public int DaysInInventory(DateTimeOffset? asOfUtc = null)
    {
        var asOf = asOfUtc ?? DateTimeOffset.UtcNow;
        var end = Status == VehicleStatus.Sold && SoldAt.HasValue ? SoldAt.Value : asOf;
        return Math.Max(0, (int)(end.Date - DateAddedToInventory.Date).TotalDays);
    }

    public bool IsAging(int thresholdDays = 90) => DaysInInventory() > thresholdDays;

    public AgingSeverity GetAgingSeverity(DateTimeOffset? asOfUtc = null)
    {
        if (Status == VehicleStatus.Sold) return AgingSeverity.None;
        return DaysInInventory(asOfUtc) switch
        {
            >= 90 => AgingSeverity.Critical,
            >= 60 => AgingSeverity.High,
            >= 30 => AgingSeverity.Warning,
            _ => AgingSeverity.None
        };
    }

    /// <summary>
    /// Demand heuristic 0–100. EV/Hybrid & newer vehicles score higher; aging stock lower.
    /// </summary>
    public int GetDemandScore(DateTimeOffset? asOfUtc = null)
    {
        if (Status == VehicleStatus.Sold) return 0;

        var asOf = asOfUtc ?? DateTimeOffset.UtcNow;
        var score = 50;

        var yearDelta = asOf.Year - Year;
        score += yearDelta switch
        {
            <= 0 => 20,
            1 => 15,
            2 => 10,
            3 => 5,
            _ => -5
        };

        score += FuelType switch
        {
            FuelType.Electric => 15,
            FuelType.PluginHybrid => 12,
            FuelType.Hybrid => 10,
            FuelType.Petrol => 0,
            FuelType.Diesel => -5,
            _ => 0
        };

        var age = DaysInInventory(asOf);
        score -= age switch
        {
            >= 120 => 35,
            >= 90 => 25,
            >= 60 => 15,
            >= 30 => 8,
            _ => 0
        };

        return Math.Clamp(score, 0, 100);
    }

    public DemandLevel GetDemandLevel(DateTimeOffset? asOfUtc = null)
        => GetDemandScore(asOfUtc) switch
        {
            >= 70 => DemandLevel.High,
            >= 40 => DemandLevel.Medium,
            _ => DemandLevel.Low
        };

    // ── Private helpers ───────────────────────────────────────────────────────

    private void EnsureNotSold()
    {
        if (Status == VehicleStatus.Sold)
            throw new InvalidOperationException("Sold vehicles cannot be modified.");
    }

    private void EnsureNotWholesale()
    {
        if (Status == VehicleStatus.Wholesale)
            throw new InvalidOperationException("Wholesale vehicles cannot be sold via retail flow.");
    }

    private static string GenerateStockNumber(string vin) =>
        vin.Length >= 6 ? $"STK-{vin[^6..]}" : $"STK-{vin}";

    private static void Validate(string make, string model, int year, string color, int mileage, DateTimeOffset dateAdded, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(make) || make.Length > 50)
            throw new ArgumentException("Make must be 1–50 chars.", nameof(make));
        if (string.IsNullOrWhiteSpace(model) || model.Length > 50)
            throw new ArgumentException("Model must be 1–50 chars.", nameof(model));
        if (year < 1980 || year > nowUtc.Year + 1)
            throw new ArgumentOutOfRangeException(nameof(year), "Year out of plausible range.");
        if (string.IsNullOrWhiteSpace(color) || color.Length > 30)
            throw new ArgumentException("Color must be 1–30 chars.", nameof(color));
        if (mileage < 0)
            throw new ArgumentOutOfRangeException(nameof(mileage), "Mileage must be non-negative.");
        if (dateAdded > nowUtc)
            throw new ArgumentException("DateAddedToInventory cannot be in the future.", nameof(dateAdded));
    }
}

public enum AgingSeverity
{
    None = 0,
    Warning = 1,
    High = 2,
    Critical = 3
}

public enum DemandLevel
{
    Low = 0,
    Medium = 1,
    High = 2
}

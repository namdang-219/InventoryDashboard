using IID.Domain.Common;

namespace IID.Domain.Dealerships;

public sealed class Dealership : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Dealership() { } // EF Core

    public static Dealership Create(
        string name,
        string code,
        string city,
        string state,
        string phone,
        DateTimeOffset nowUtc,
        Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));

        return new Dealership
        {
            Id = id ?? Guid.NewGuid(),
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            City = city.Trim(),
            State = state.Trim().ToUpperInvariant(),
            Phone = phone.Trim(),
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };
    }

    public void Update(string name, string code, string city, string state, string phone, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        City = city.Trim();
        State = state.Trim().ToUpperInvariant();
        Phone = phone.Trim();
        UpdatedAt = nowUtc;
    }
}

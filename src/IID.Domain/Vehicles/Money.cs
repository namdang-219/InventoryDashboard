namespace IID.Domain.Vehicles;

/// <summary>
/// Money value object: non-negative amount + ISO 4217 currency code.
/// Modeled as a record so it has value-based equality in accordance with DDD.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";

    private Money() { }

    public static Money Of(decimal amount, string currency = "USD", bool allowNegative = false)
    {
        if (!allowNegative && amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code.", nameof(currency));
        return new Money { Amount = amount, Currency = currency.ToUpperInvariant() };
    }

    public static Money Zero(string currency = "USD") => Of(0m, currency);
}

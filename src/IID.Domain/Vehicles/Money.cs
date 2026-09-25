namespace IID.Domain.Vehicles;

/// <summary>
/// Money value object: non-negative amount + ISO 4217 currency code.
/// Modeled as a class so EF Core 10 can map it as an owned entity.
/// </summary>
public sealed class Money
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";

    private Money() { }

    public static Money Of(decimal amount, string currency = "USD")
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code.", nameof(currency));
        return new Money { Amount = amount, Currency = currency.ToUpperInvariant() };
    }

    public static Money Zero(string currency = "USD") => Of(0m, currency);
}

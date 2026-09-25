using System.Text.RegularExpressions;

namespace IID.Domain.Vehicles;

/// <summary>
/// VIN value object (ISO 3779). 17 chars; charset A-HJ-NPR-Z0-9.
/// </summary>
public readonly partial record struct Vin
{
    [GeneratedRegex("^[A-HJ-NPR-Z0-9]{17}$", RegexOptions.Compiled)]
    private static partial Regex VinRegex();

    public string Value { get; }

    private Vin(string value) => Value = value;

    public static Vin Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("VIN must not be empty.", nameof(value));
        if (!VinRegex().IsMatch(value))
            throw new ArgumentException("VIN must be 17 chars, charset [A-HJ-NPR-Z0-9].", nameof(value));
        return new Vin(value.ToUpperInvariant());
    }

    public override string ToString() => Value;
}

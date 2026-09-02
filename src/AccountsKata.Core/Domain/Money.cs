using System.Globalization;

namespace AccountsKata.Core.Domain;

/// <summary>Open set of currencies: adding one only requires a new rate line in the statement file.</summary>
public readonly record struct Currency(string Code)
{
    public static readonly Currency Euro = new("EUR");

    public static Currency Parse(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Currency code is required.", nameof(code));
        }

        return new Currency(code.Trim().ToUpperInvariant());
    }

    public override string ToString() => Code;
}

public readonly record struct Money(decimal Amount, Currency Currency)
{
    public static Money Euros(decimal amount) => new(amount, Currency.Euro);

    /// <summary>Rounding happens on display only, never while accumulating.</summary>
    public string Format(int decimals = 2) =>
        $"{Math.Round(Amount, decimals, MidpointRounding.AwayFromZero).ToString($"F{decimals}", CultureInfo.InvariantCulture)} {Currency}";

    public override string ToString() => Format();
}

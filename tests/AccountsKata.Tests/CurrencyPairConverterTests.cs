using AccountsKata.Core.Domain;

namespace AccountsKata.Tests;

public class CurrencyPairConverterTests
{
    private static readonly Currency Eur = Currency.Euro;
    private static readonly Currency Jpy = Currency.Parse("JPY");
    private static readonly Currency Usd = Currency.Parse("USD");
    private static readonly Currency Gbp = Currency.Parse("GBP");

    /// <summary>Today's shape: everything quoted against a single currency.</summary>
    private static readonly CurrencyPairConverter QuotedAgainstEuro = new(
    [
        new(Jpy, Eur, 0.482m),
        new(Usd, Eur, 1.445m),
    ]);

    [Fact]
    public void Leaves_an_amount_already_in_the_target_currency_untouched() =>
        Assert.Equal(Money.Euros(-504.61m), QuotedAgainstEuro.Convert(Money.Euros(-504.61m), Eur));

    [Fact]
    public void Uses_a_declared_pair_directly() =>
        Assert.Equal(-196.95002m, QuotedAgainstEuro.Convert(new Money(-408.61m, Jpy), Eur).Amount);

    [Fact]
    public void Derives_the_inverse_of_a_declared_pair() =>
        Assert.Equal("1000.00 USD", QuotedAgainstEuro.Convert(Money.Euros(1445m), Usd).Format());

    [Fact]
    public void Chains_two_pairs_when_none_is_declared() =>
        Assert.Equal("2997.93 JPY", QuotedAgainstEuro.Convert(new Money(1000m, Usd), Jpy).Format());

    [Fact]
    public void Fails_when_no_chain_connects_the_two_currencies() =>
        Assert.Throws<KeyNotFoundException>(() => QuotedAgainstEuro.Convert(new Money(10m, Gbp), Eur));

    [Fact]
    public void Rejects_a_non_positive_rate() =>
        Assert.Throws<ArgumentException>(() => new CurrencyPairConverter([new(Jpy, Eur, 0m)]));

    /// <summary>Tomorrow's shape: arbitrary pairs, no common quote currency. Same class, no change.</summary>
    [Fact]
    public void Supports_a_full_pair_matrix_without_a_common_quote_currency()
    {
        var converter = new CurrencyPairConverter(
        [
            new(Usd, Jpy, 150m),
            new(Jpy, Gbp, 0.005m),
        ]);

        Assert.Equal("15000.00 JPY", converter.Convert(new Money(100m, Usd), Jpy).Format());
        Assert.Equal("75.00 GBP", converter.Convert(new Money(100m, Usd), Gbp).Format());
        Assert.Equal("100.00 USD", converter.Convert(new Money(75m, Gbp), Usd).Format());
    }

    [Fact]
    public void Prefers_a_declared_pair_over_a_derived_chain()
    {
        var converter = new CurrencyPairConverter(
        [
            new(Usd, Eur, 1.445m),
            new(Eur, Jpy, 2m),
            new(Usd, Jpy, 3m),
        ]);

        Assert.Equal("300.00 JPY", converter.Convert(new Money(100m, Usd), Jpy).Format());
    }
}

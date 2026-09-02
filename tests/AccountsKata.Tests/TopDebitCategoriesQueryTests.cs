using AccountsKata.Core.Domain;
using AccountsKata.Core.Features;

namespace AccountsKata.Tests;

public class TopDebitCategoriesQueryTests
{
    private static readonly Currency Usd = Currency.Parse("USD");

    private static readonly ICurrencyConverter Converter = new CurrencyPairConverter([new(Usd, Currency.Euro, 2m)]);

    private static readonly Transaction[] History =
    [
        new(new DateOnly(2022, 1, 1), Money.Euros(-100m), "Habitation"),
        new(new DateOnly(2022, 2, 1), new Money(-100m, Usd), "Habitation"),
        new(new DateOnly(2022, 3, 1), Money.Euros(-250m), "Loisir"),
        new(new DateOnly(2022, 4, 1), Money.Euros(-10m), "Sante"),
        new(new DateOnly(2022, 5, 1), Money.Euros(9999m), "Salaire"),
    ];

    private static TopDebitCategoriesQuery Query() =>
        new(new AccountStatement(new DateOnly(2022, 6, 1), Money.Euros(0m), History), Converter);

    [Fact]
    public void Ranks_categories_by_converted_debit_total()
    {
        var top = Query().Top(2);

        Assert.Equal(["Habitation", "Loisir"], top.Select(t => t.Category));
        Assert.Equal(-300m, top[0].Total.Amount);
        Assert.Equal(2, top[0].TransactionCount);
    }

    [Fact]
    public void Ignores_credits() =>
        Assert.DoesNotContain("Salaire", Query().Top(10).Select(t => t.Category));

    [Fact]
    public void Can_be_restricted_to_a_period()
    {
        var top = Query().Top(1, new DateRange(new DateOnly(2022, 3, 1), new DateOnly(2022, 4, 1)));

        Assert.Equal("Loisir", top[0].Category);
    }

    [Fact]
    public void Rejects_a_non_positive_count() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Query().Top(0));

    [Fact]
    public void Can_express_totals_in_another_currency()
    {
        var top = Query().Top(1, period: null, currency: Usd);

        Assert.Equal("-150.00 USD", top[0].Total.Format());
    }
}

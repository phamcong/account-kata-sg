using AccountsKata.Core.Domain;
using AccountsKata.Core.Features;

namespace AccountsKata.Tests;

public class AccountBalanceQueryTests
{
    private static readonly Currency Usd = Currency.Parse("USD");

    private static readonly ICurrencyConverter Converter = new CurrencyPairConverter([new(Usd, Currency.Euro, 2m)]);

    private static AccountBalanceQuery Build(params Transaction[] transactions) =>
        new(new AccountStatement(new DateOnly(2022, 1, 10), Money.Euros(100m), transactions), Converter);

    private static Transaction Eur(int day, decimal amount) =>
        new(new DateOnly(2022, 1, day), Money.Euros(amount), "Divers");

    [Fact]
    public void Returns_the_anchor_balance_at_the_statement_date() =>
        Assert.Equal(Money.Euros(100m), Build(Eur(5, -30m), Eur(8, 50m)).At(new DateOnly(2022, 1, 10)));

    [Fact]
    public void Rewinds_the_history_for_an_earlier_date() =>
        Assert.Equal(Money.Euros(80m), Build(Eur(5, -30m), Eur(8, 50m)).At(new DateOnly(2022, 1, 4)));

    [Fact]
    public void Includes_the_transactions_of_the_requested_day() =>
        Assert.Equal(Money.Euros(50m), Build(Eur(5, -30m), Eur(8, 50m)).At(new DateOnly(2022, 1, 5)));

    [Fact]
    public void Converts_foreign_transactions_before_replaying_them()
    {
        var usd = new Transaction(new DateOnly(2022, 1, 5), new Money(-30m, Usd), "Divers");

        Assert.Equal(Money.Euros(160m), Build(usd).At(new DateOnly(2022, 1, 4)));
    }

    [Fact]
    public void Handles_an_empty_history() =>
        Assert.Equal(Money.Euros(100m), Build().At(new DateOnly(2022, 1, 1)));

    [Fact]
    public void Can_express_the_result_in_another_currency() =>
        Assert.Equal("40.00 USD", Build(Eur(5, -30m), Eur(8, 50m)).At(new DateOnly(2022, 1, 4), Usd).Format());

    /// <summary>The account currency comes from the anchor, not from a hard-coded euro.</summary>
    [Fact]
    public void Works_on_an_account_denominated_in_another_currency()
    {
        var statement = new AccountStatement(
            new DateOnly(2022, 1, 10),
            new Money(100m, Usd),
            [new(new DateOnly(2022, 1, 5), Money.Euros(-40m), "Divers")]);

        var query = new AccountBalanceQuery(statement, Converter);

        Assert.Equal("120.00 USD", query.At(new DateOnly(2022, 1, 4)).Format());
        Assert.Equal("240.00 EUR", query.At(new DateOnly(2022, 1, 4), Currency.Euro).Format());
    }
}

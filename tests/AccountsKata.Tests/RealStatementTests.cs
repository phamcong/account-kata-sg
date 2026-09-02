using AccountsKata.Core.Domain;
using AccountsKata.Core.Features;
using AccountsKata.Core.Parsing;

namespace AccountsKata.Tests;

/// <summary>End-to-end checks against the 10 000 line statement provided with the kata.</summary>
public class RealStatementTests
{
    private static readonly CsvFileStatementSource Source =
        new(Path.Combine(AppContext.BaseDirectory, "account_20230228.csv"));

    private static readonly AccountStatement Statement = Source.LoadStatement();

    private static readonly ICurrencyConverter Converter = new CurrencyPairConverter(Source.LoadRates());

    [Fact]
    public void Loads_the_whole_history()
    {
        Assert.Equal(10_000, Statement.Transactions.Count);
        Assert.Equal(new DateOnly(2023, 2, 28), Statement.StatementDate);
        Assert.Equal(Money.Euros(8300.00m), Statement.StatementBalance);
    }

    [Fact]
    public void Balance_at_the_statement_date_is_the_announced_one() =>
        Assert.Equal("8300.00 EUR", new AccountBalanceQuery(Statement, Converter).At(new DateOnly(2023, 2, 28)).Format());

    [Fact]
    public void Balance_can_be_displayed_in_another_currency() =>
        Assert.Equal(
            "5743.94 USD",
            new AccountBalanceQuery(Statement, Converter).At(new DateOnly(2023, 2, 28), Currency.Parse("USD")).Format());

    [Fact]
    public void Balance_is_unchanged_after_the_last_transaction()
    {
        var balance = new AccountBalanceQuery(Statement, Converter);
        var lastTransactionDate = Statement.Transactions.Max(t => t.Date);

        Assert.Equal(balance.At(new DateOnly(2023, 2, 28)), balance.At(lastTransactionDate));
    }

    [Fact]
    public void Top_three_debit_categories_match_the_expected_answer()
    {
        var top = new TopDebitCategoriesQuery(Statement, Converter).Top(3);

        Assert.Equal(["Alimentation", "Sante", "Habitation"], top.Select(t => t.Category));
        Assert.Equal("-567392.63 EUR", top[0].Total.Format());
        Assert.Equal("-554428.06 EUR", top[1].Total.Format());
        Assert.Equal("-552477.08 EUR", top[2].Total.Format());
    }
}

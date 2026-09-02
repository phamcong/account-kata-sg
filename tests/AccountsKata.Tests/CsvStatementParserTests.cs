using AccountsKata.Core.Domain;
using AccountsKata.Core.Parsing;

namespace AccountsKata.Tests;

public class CsvStatementParserTests
{
    private static readonly string[] Sample =
    [
        "Compte au 28/02/2023 : 8300.00 EUR",
        "JPY/EUR : 0.482",
        "USD/EUR : 1.445",
        "Date;Montant;Devise;Categorie",
        "06/10/2022;-504.61;EUR;Loisir",
        "15/10/2022;-408.61;JPY;Transport",
    ];

    [Fact]
    public void Reads_the_balance_anchor()
    {
        var statement = CsvStatementParser.Parse(Sample).Statement;

        Assert.Equal(new DateOnly(2023, 2, 28), statement.StatementDate);
        Assert.Equal(Money.Euros(8300.00m), statement.StatementBalance);
        Assert.Equal(Currency.Euro, statement.AccountCurrency);
    }

    [Fact]
    public void Reads_rate_lines_literally_as_currency_pairs()
    {
        var rates = CsvStatementParser.Parse(Sample).Rates;

        Assert.Equal(
            [
                new ExchangeRate(Currency.Parse("JPY"), Currency.Euro, 0.482m),
                new ExchangeRate(Currency.Parse("USD"), Currency.Euro, 1.445m),
            ],
            rates);
    }

    [Fact]
    public void Rejects_a_rate_line_referencing_twice_the_same_currency()
    {
        string[] lines = [Sample[0], "EUR/EUR : 1", Sample[3]];

        Assert.Throws<StatementFormatException>(() => CsvStatementParser.Parse(lines));
    }

    [Fact]
    public void Reads_transactions_with_invariant_number_and_date_formats()
    {
        var transactions = CsvStatementParser.Parse(Sample).Statement.Transactions;

        Assert.Equal(2, transactions.Count);
        Assert.Equal(new Transaction(new DateOnly(2022, 10, 6), new Money(-504.61m, Currency.Euro), "Loisir"), transactions[0]);
        Assert.Equal(new Transaction(new DateOnly(2022, 10, 15), new Money(-408.61m, Currency.Parse("JPY")), "Transport"), transactions[1]);
    }

    [Theory]
    [InlineData("06/10/2022;-504.61;EUR")]
    [InlineData("2022-10-06;-504.61;EUR;Loisir")]
    [InlineData("06/10/2022;abc;EUR;Loisir")]
    [InlineData("06/10/2022;-504.61;EUR;")]
    public void Rejects_malformed_transaction_lines(string badLine)
    {
        string[] lines = [.. Sample[..4], badLine];

        Assert.Throws<StatementFormatException>(() => CsvStatementParser.Parse(lines));
    }
}

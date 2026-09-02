namespace AccountsKata.Core.Domain;

/// <summary>
/// The only input the features need: a balance known at <paramref name="StatementDate"/> and the
/// transaction history. Where it comes from (CSV, API, user input) and how currencies are
/// converted are separate concerns.
/// </summary>
public sealed record AccountStatement(
    DateOnly StatementDate,
    Money StatementBalance,
    IReadOnlyList<Transaction> Transactions)
{
    /// <summary>Currency the account is denominated in; results are computed in it by default.</summary>
    public Currency AccountCurrency => StatementBalance.Currency;

    public DateRange CoveredPeriod =>
        Transactions.Count == 0
            ? new DateRange(StatementDate, StatementDate)
            : new DateRange(Transactions.Min(t => t.Date), StatementDate);
}

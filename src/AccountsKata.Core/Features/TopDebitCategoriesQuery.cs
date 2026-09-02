using AccountsKata.Core.Domain;

namespace AccountsKata.Core.Features;

public sealed record CategoryTotal(string Category, Money Total, int TransactionCount);

/// <summary>
/// Feature 2: largest debit categories over the whole history.
/// Added without touching feature 1, the parser or the domain: that is the extension point.
/// </summary>
public sealed class TopDebitCategoriesQuery(AccountStatement statement, ICurrencyConverter converter)
{
    private readonly AccountStatement _statement = statement ?? throw new ArgumentNullException(nameof(statement));
    private readonly ICurrencyConverter _converter = converter ?? throw new ArgumentNullException(nameof(converter));

    /// <param name="currency">Display currency; defaults to the account currency.</param>
    public IReadOnlyList<CategoryTotal> Top(int count, DateRange? period = null, Currency? currency = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var accountCurrency = _statement.AccountCurrency;
        var display = currency ?? accountCurrency;

        return _statement.Transactions
            .Where(t => t.IsDebit && (period is null || period.Value.Contains(t.Date)))
            .GroupBy(t => t.Category, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CategoryTotal(
                group.Key,
                new Money(group.Sum(t => _converter.Convert(t.Amount, accountCurrency).Amount), accountCurrency),
                group.Count()))
            .OrderBy(total => total.Total.Amount)
            .ThenBy(total => total.Category, StringComparer.OrdinalIgnoreCase)
            .Take(count)
            .Select(total => total with { Total = _converter.Convert(total.Total, display) })
            .ToList();
    }
}

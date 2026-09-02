using AccountsKata.Core.Domain;

namespace AccountsKata.Core.Features;

/// <summary>
/// Feature 1: value of the account at a given date.
/// A date is inclusive: the balance is the one at the <em>end</em> of that day.
/// The known balance is an anchor at the statement date, so the history is replayed
/// relative to it, which works for dates before and after the statement date.
/// </summary>
public sealed class AccountBalanceQuery
{
    private readonly ICurrencyConverter _converter;
    private readonly Currency _accountCurrency;
    private readonly DateOnly[] _dates;

    /// <summary>Running total in the account currency; the display currency is applied once, at the end.</summary>
    private readonly decimal[] _cumulative;

    private readonly decimal _anchorBalance;
    private readonly decimal _cumulativeAtAnchor;

    public AccountBalanceQuery(AccountStatement statement, ICurrencyConverter converter)
    {
        ArgumentNullException.ThrowIfNull(statement);
        ArgumentNullException.ThrowIfNull(converter);

        _converter = converter;
        _accountCurrency = statement.AccountCurrency;

        var ordered = statement.Transactions.OrderBy(t => t.Date).ToArray();
        _dates = new DateOnly[ordered.Length];
        _cumulative = new decimal[ordered.Length + 1];

        for (var i = 0; i < ordered.Length; i++)
        {
            _dates[i] = ordered[i].Date;
            _cumulative[i + 1] = _cumulative[i] + converter.Convert(ordered[i].Amount, _accountCurrency).Amount;
        }

        _anchorBalance = statement.StatementBalance.Amount;
        _cumulativeAtAnchor = _cumulative[CountUpTo(statement.StatementDate)];
    }

    /// <param name="currency">Display currency; defaults to the account currency.</param>
    public Money At(DateOnly date, Currency? currency = null)
    {
        var balance = new Money(_anchorBalance + _cumulative[CountUpTo(date)] - _cumulativeAtAnchor, _accountCurrency);

        return currency is null ? balance : _converter.Convert(balance, currency.Value);
    }

    /// <summary>Number of transactions dated on or before <paramref name="date"/>.</summary>
    private int CountUpTo(DateOnly date)
    {
        var low = 0;
        var high = _dates.Length;
        while (low < high)
        {
            var mid = low + ((high - low) / 2);
            if (_dates[mid] <= date)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }
}

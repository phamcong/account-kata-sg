using AccountsKata.Core.Domain;

namespace AccountsKata.Core.Features;

public sealed record BalancePoint(DateOnly Date, Money Balance);

/// <summary>
/// Feature 3: evolution of the balance over a period. Built entirely on top of
/// <see cref="AccountBalanceQuery"/> — no change to the domain, the parser or the other features.
/// </summary>
public sealed class BalanceHistoryQuery(AccountStatement statement, ICurrencyConverter converter)
{
    private readonly AccountBalanceQuery _balance = new(statement, converter);

    public IReadOnlyList<BalancePoint> Monthly(DateRange period, Currency? currency = null)
    {
        if (period.End < period.Start)
        {
            throw new ArgumentException("The end date must not precede the start date.", nameof(period));
        }

        var points = new List<BalancePoint> { Point(period.Start, currency) };

        for (var cursor = EndOfMonth(period.Start); cursor < period.End; cursor = EndOfMonth(cursor.AddDays(1)))
        {
            if (cursor > period.Start)
            {
                points.Add(Point(cursor, currency));
            }
        }

        if (period.End != period.Start)
        {
            points.Add(Point(period.End, currency));
        }

        return points;
    }

    private BalancePoint Point(DateOnly date, Currency? currency) => new(date, _balance.At(date, currency));

    private static DateOnly EndOfMonth(DateOnly date) =>
        new(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
}

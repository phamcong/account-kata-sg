using AccountsKata.Core.Domain;
using AccountsKata.Core.Features;

namespace AccountsKata.Tests;

public class BalanceHistoryQueryTests
{
    private static readonly ICurrencyConverter Converter = new CurrencyPairConverter([]);

    private static BalanceHistoryQuery Query() =>
        new(
            new AccountStatement(
                new DateOnly(2022, 4, 30),
                Money.Euros(0m),
                [
                    new(new DateOnly(2022, 2, 10), Money.Euros(-100m), "Habitation"),
                    new(new DateOnly(2022, 3, 20), Money.Euros(-50m), "Loisir"),
                ]),
            Converter);

    [Fact]
    public void Reports_the_bounds_and_every_month_end_in_between()
    {
        var points = Query().Monthly(new DateRange(new DateOnly(2022, 1, 15), new DateOnly(2022, 4, 10)));

        Assert.Equal(
            [
                new DateOnly(2022, 1, 15),
                new DateOnly(2022, 1, 31),
                new DateOnly(2022, 2, 28),
                new DateOnly(2022, 3, 31),
                new DateOnly(2022, 4, 10),
            ],
            points.Select(p => p.Date));
        Assert.Equal(Money.Euros(150m), points[0].Balance);
        Assert.Equal(Money.Euros(50m), points[2].Balance);
        Assert.Equal(Money.Euros(0m), points[^1].Balance);
    }

    [Fact]
    public void Does_not_duplicate_a_bound_that_already_is_a_month_end()
    {
        var points = Query().Monthly(new DateRange(new DateOnly(2022, 1, 31), new DateOnly(2022, 3, 31)));

        Assert.Equal(points.Select(p => p.Date).Distinct(), points.Select(p => p.Date));
        Assert.Equal(3, points.Count);
    }

    [Fact]
    public void Returns_a_single_point_for_a_single_day() =>
        Assert.Single(Query().Monthly(new DateRange(new DateOnly(2022, 2, 15), new DateOnly(2022, 2, 15))));

    [Fact]
    public void Rejects_an_inverted_period() =>
        Assert.Throws<ArgumentException>(() =>
            Query().Monthly(new DateRange(new DateOnly(2022, 3, 1), new DateOnly(2022, 2, 1))));
}

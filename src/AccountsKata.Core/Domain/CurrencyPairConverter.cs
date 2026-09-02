namespace AccountsKata.Core.Domain;

/// <summary>1 <see cref="From"/> = <see cref="Value"/> <see cref="To"/>.</summary>
public sealed record ExchangeRate(Currency From, Currency To, decimal Value);

/// <summary>
/// Converts between any two currencies connected by the declared rates: a declared pair is used
/// directly, its inverse is derived, and an undeclared pair is chained through intermediates.
/// Works unchanged with today's rates (all quoted against the euro) and with a future full matrix.
/// </summary>
public sealed class CurrencyPairConverter : ICurrencyConverter
{
    private readonly Dictionary<Currency, Dictionary<Currency, decimal>> _rates = [];
    private readonly Dictionary<(Currency From, Currency To), decimal> _resolved = [];

    public CurrencyPairConverter(IEnumerable<ExchangeRate> rates)
    {
        ArgumentNullException.ThrowIfNull(rates);

        foreach (var rate in rates)
        {
            if (rate.Value <= 0m)
            {
                throw new ArgumentException($"Rate {rate.From}/{rate.To} must be strictly positive.", nameof(rates));
            }

            Declare(rate.From, rate.To, rate.Value);
            Declare(rate.To, rate.From, 1m / rate.Value);
        }
    }

    public Money Convert(Money money, Currency target) =>
        money.Currency == target
            ? money
            : new Money(money.Amount * Factor(money.Currency, target), target);

    private void Declare(Currency from, Currency to, decimal value)
    {
        if (!_rates.TryGetValue(from, out var edges))
        {
            _rates[from] = edges = [];
        }

        edges[to] = value;
    }

    /// <summary>Shortest chain of declared rates, so a direct pair always wins over a derived one.</summary>
    private decimal Factor(Currency from, Currency to)
    {
        if (_resolved.TryGetValue((from, to), out var cached))
        {
            return cached;
        }

        var visited = new HashSet<Currency> { from };
        var queue = new Queue<(Currency Currency, decimal Factor)>();
        queue.Enqueue((from, 1m));

        while (queue.Count > 0)
        {
            var (current, factor) = queue.Dequeue();
            if (!_rates.TryGetValue(current, out var edges))
            {
                continue;
            }

            foreach (var (next, rate) in edges)
            {
                if (!visited.Add(next))
                {
                    continue;
                }

                var reached = factor * rate;
                if (next == to)
                {
                    _resolved[(from, to)] = reached;
                    return reached;
                }

                queue.Enqueue((next, reached));
            }
        }

        throw new KeyNotFoundException($"No exchange rate connects '{from}' to '{to}'.");
    }
}

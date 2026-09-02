using System.Globalization;
using AccountsKata.Core.Domain;
using AccountsKata.Core.Features;
using AccountsKata.Core.Parsing;

namespace AccountsKata.Api;

/// <summary>
/// Second adapter over the same features as the CLI: nothing in <c>Features/</c> or <c>Domain/</c>
/// changed to expose them over HTTP.
/// </summary>
public static class AccountEndpoints
{
    private const string DateFormat = "dd/MM/yyyy";

    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").WithTags("Account");

        api.MapGet("/statement", (AccountStatement statement, IExchangeRateSource rateSource) =>
                new StatementSummaryResponse(
                    Format(statement.StatementDate),
                    Round(statement.StatementBalance.Amount),
                    statement.AccountCurrency.Code,
                    Format(statement.CoveredPeriod.Start),
                    Format(statement.CoveredPeriod.End),
                    statement.Transactions.Count,
                    [.. statement.Transactions.Select(t => t.Category).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)],
                    [.. rateSource.LoadRates().Select(r => new ExchangeRateResponse(r.From.Code, r.To.Code, r.Value))]))
            .WithName("GetStatement")
            .WithSummary("Statement metadata")
            .WithDescription("Anchor balance, covered period, categories and declared exchange rates.");

        api.MapGet("/balance", (string date, string? currency, AccountBalanceQuery query) => Guarded(() =>
            {
                if (!TryParseSupportedDate(date, out var parsed, out var error) ||
                    !TryParseCurrency(currency, out var target, out error))
                {
                    return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);
                }

                var balance = query.At(parsed, target);

                return Results.Ok(new BalanceResponse(Format(parsed), Round(balance.Amount), balance.Currency.Code));
            }))
            .WithName("GetBalance")
            .WithSummary("Account value at a date")
            .WithDescription($"Feature 1. Date in {DateFormat}, within {SupportedPeriod.Default}. Optional currency, defaults to the account currency.")
            .Produces<BalanceResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        api.MapGet("/balance/history", (string from, string to, string? currency, BalanceHistoryQuery query) => Guarded(() =>
            {
                if (!TryParseSupportedDate(from, out var start, out var error) ||
                    !TryParseSupportedDate(to, out var end, out error) ||
                    !TryParseCurrency(currency, out var target, out error))
                {
                    return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);
                }

                if (end < start)
                {
                    return Results.Problem("The end date must not precede the start date.", statusCode: StatusCodes.Status400BadRequest);
                }

                var points = query.Monthly(new DateRange(start, end), target);

                return Results.Ok(new BalanceHistoryResponse(
                    Format(start),
                    Format(end),
                    points[0].Balance.Currency.Code,
                    Round(points[^1].Balance.Amount - points[0].Balance.Amount),
                    [.. points.Select(p => new BalancePointResponse(Format(p.Date), Round(p.Balance.Amount)))]));
            }))
            .WithName("GetBalanceHistory")
            .WithSummary("Monthly balance evolution")
            .WithDescription("Feature 3. Balance at both bounds and at every month end in between.")
            .Produces<BalanceHistoryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        api.MapGet("/debits/top", (int count, string? currency, string? from, string? to, TopDebitCategoriesQuery query) => Guarded(() =>
            {
                if (count <= 0)
                {
                    return Results.Problem("count must be a positive integer.", statusCode: StatusCodes.Status400BadRequest);
                }

                if (!TryParseCurrency(currency, out var target, out var error))
                {
                    return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);
                }

                DateRange? period = null;
                if (from is not null || to is not null)
                {
                    if (!TryParseSupportedDate(from, out var start, out error) ||
                        !TryParseSupportedDate(to, out var end, out error))
                    {
                        return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);
                    }

                    period = new DateRange(start, end);
                }

                var totals = query.Top(count, period, target);

                return Results.Ok(totals
                    .Select((total, index) => new CategoryTotalResponse(
                        index + 1,
                        total.Category,
                        Round(total.Total.Amount),
                        total.Total.Currency.Code,
                        total.TransactionCount))
                    .ToList());
            }))
            .WithName("GetTopDebitCategories")
            .WithSummary("Largest debit categories")
            .WithDescription("Feature 2. Ranked most negative first. Optional from/to restrict the period.")
            .Produces<IReadOnlyList<CategoryTotalResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static IResult Guarded(Func<IResult> handler)
    {
        try
        {
            return handler();
        }
        catch (Exception ex) when (ex is KeyNotFoundException or ArgumentException)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static bool TryParseSupportedDate(string? value, out DateOnly date, out string? error)
    {
        if (!DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            error = $"'{value}' is not a {DateFormat} date.";
            return false;
        }

        if (!SupportedPeriod.Default.Contains(date))
        {
            error = $"The date must fall within the supported period {SupportedPeriod.Default}.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryParseCurrency(string? value, out Currency? currency, out string? error)
    {
        currency = null;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (value.Trim().Length != 3)
        {
            error = $"'{value}' is not a three-letter currency code.";
            return false;
        }

        currency = Currency.Parse(value);
        return true;
    }

    private static string Format(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    private static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}

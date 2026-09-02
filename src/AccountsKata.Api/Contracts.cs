namespace AccountsKata.Api;

/// <summary>Account value at a given date.</summary>
public sealed record BalanceResponse(string Date, decimal Amount, string Currency);

/// <summary>Total debited on a category over the requested period.</summary>
public sealed record CategoryTotalResponse(int Rank, string Category, decimal Amount, string Currency, int TransactionCount);

public sealed record BalancePointResponse(string Date, decimal Amount);

public sealed record BalanceHistoryResponse(
    string From,
    string To,
    string Currency,
    decimal Change,
    IReadOnlyList<BalancePointResponse> Points);

/// <summary>1 <c>From</c> = <c>Rate</c> <c>To</c>.</summary>
public sealed record ExchangeRateResponse(string From, string To, decimal Rate);

public sealed record StatementSummaryResponse(
    string StatementDate,
    decimal StatementBalance,
    string AccountCurrency,
    string HistoryStart,
    string HistoryEnd,
    int TransactionCount,
    IReadOnlyList<string> Categories,
    IReadOnlyList<ExchangeRateResponse> Rates);

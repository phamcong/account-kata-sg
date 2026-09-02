namespace AccountsKata.Core.Parsing;

using AccountsKata.Core.Domain;

/// <summary>Ports: the statement and the rates are two independent inputs that happen to share a file.</summary>
public interface IStatementSource
{
    AccountStatement LoadStatement();
}

public interface IExchangeRateSource
{
    IReadOnlyList<ExchangeRate> LoadRates();
}

public sealed class StatementFormatException(string message) : Exception(message);

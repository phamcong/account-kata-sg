using System.Text;
using AccountsKata.Core.Domain;

namespace AccountsKata.Core.Parsing;

public sealed record CsvStatement(AccountStatement Statement, IReadOnlyList<ExchangeRate> Rates);

public sealed class CsvFileStatementSource(string path) : IStatementSource, IExchangeRateSource
{
    private readonly string _path = path ?? throw new ArgumentNullException(nameof(path));
    private CsvStatement? _parsed;

    public AccountStatement LoadStatement() => Parsed().Statement;

    public IReadOnlyList<ExchangeRate> LoadRates() => Parsed().Rates;

    private CsvStatement Parsed()
    {
        if (_parsed is not null)
        {
            return _parsed;
        }

        if (!File.Exists(_path))
        {
            throw new FileNotFoundException($"Statement file not found: {_path}", _path);
        }

        return _parsed = CsvStatementParser.Parse(File.ReadLines(_path, Encoding.UTF8));
    }
}

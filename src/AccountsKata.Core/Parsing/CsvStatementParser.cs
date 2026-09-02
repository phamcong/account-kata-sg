using System.Globalization;
using System.Text.RegularExpressions;
using AccountsKata.Core.Domain;

namespace AccountsKata.Core.Parsing;

/// <summary>
/// Reads the statement layout: one balance line, then N exchange-rate lines, then the column
/// header, then the transactions. The number of rate lines is not hard-coded, so a fourth
/// currency only requires an extra line in the file.
/// </summary>
public static partial class CsvStatementParser
{
    private const char Separator = ';';
    private static readonly string[] DateFormats = ["dd/MM/yyyy"];

    public static CsvStatement Parse(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        DateOnly? statementDate = null;
        Money? statementBalance = null;
        var rates = new List<ExchangeRate>();
        var transactions = new List<Transaction>();
        var inTransactions = false;
        var lineNumber = 0;

        foreach (var raw in lines)
        {
            lineNumber++;
            var line = raw.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (inTransactions)
            {
                transactions.Add(ParseTransaction(line, lineNumber));
                continue;
            }

            if (statementBalance is null)
            {
                (statementDate, statementBalance) = ParseBalance(line, lineNumber);
                continue;
            }

            if (IsColumnHeader(line))
            {
                inTransactions = true;
                continue;
            }

            rates.Add(ParseRate(line, lineNumber));
        }

        if (statementDate is null || statementBalance is null)
        {
            throw new StatementFormatException("Statement is missing its balance line.");
        }

        return new CsvStatement(
            new AccountStatement(statementDate.Value, statementBalance.Value, transactions),
            rates);
    }

    private static bool IsColumnHeader(string line) =>
        line.StartsWith("Date" + Separator, StringComparison.OrdinalIgnoreCase);

    private static (DateOnly Date, Money Balance) ParseBalance(string line, int lineNumber)
    {
        var match = BalanceLine().Match(line);
        if (!match.Success)
        {
            throw new StatementFormatException($"Line {lineNumber}: expected 'Compte au dd/MM/yyyy : <amount> <CUR>' but got '{line}'.");
        }

        return (
            ParseDate(match.Groups["date"].Value, lineNumber),
            new Money(ParseAmount(match.Groups["amount"].Value, lineNumber), Currency.Parse(match.Groups["currency"].Value)));
    }

    /// <summary>
    /// <c>A/B : r</c> is read literally as "1 A = r B". The converter derives the inverse and any
    /// missing pair, so no assumption is made about which side is the quote currency.
    /// </summary>
    private static ExchangeRate ParseRate(string line, int lineNumber)
    {
        var match = RateLine().Match(line);
        if (!match.Success)
        {
            throw new StatementFormatException($"Line {lineNumber}: expected '<CUR>/<CUR> : <rate>' but got '{line}'.");
        }

        var from = Currency.Parse(match.Groups["left"].Value);
        var to = Currency.Parse(match.Groups["right"].Value);

        if (from == to)
        {
            throw new StatementFormatException($"Line {lineNumber}: a rate line must reference two different currencies.");
        }

        return new ExchangeRate(from, to, ParseAmount(match.Groups["rate"].Value, lineNumber));
    }

    private static Transaction ParseTransaction(string line, int lineNumber)
    {
        var fields = line.Split(Separator);
        if (fields.Length != 4)
        {
            throw new StatementFormatException($"Line {lineNumber}: expected 4 fields separated by '{Separator}' but got {fields.Length}.");
        }

        var date = ParseDate(fields[0].Trim(), lineNumber);
        var amount = ParseAmount(fields[1].Trim(), lineNumber);
        var currency = Currency.Parse(fields[2]);
        var category = fields[3].Trim();

        if (category.Length == 0)
        {
            throw new StatementFormatException($"Line {lineNumber}: category is empty.");
        }

        return new Transaction(date, new Money(amount, currency), category);
    }

    private static DateOnly ParseDate(string value, int lineNumber) =>
        DateOnly.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new StatementFormatException($"Line {lineNumber}: '{value}' is not a dd/MM/yyyy date.");

    private static decimal ParseAmount(string value, int lineNumber) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : throw new StatementFormatException($"Line {lineNumber}: '{value}' is not a decimal amount.");

    [GeneratedRegex(@"^Compte\s+au\s+(?<date>[\d/]+)\s*:\s*(?<amount>-?[\d.]+)\s*(?<currency>[A-Za-z]{3})$", RegexOptions.IgnoreCase)]
    private static partial Regex BalanceLine();

    [GeneratedRegex(@"^(?<left>[A-Za-z]{3})\s*/\s*(?<right>[A-Za-z]{3})\s*:\s*(?<rate>[\d.]+)$")]
    private static partial Regex RateLine();
}

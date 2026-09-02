using System.Globalization;

namespace AccountsKata.Core.Domain;

public readonly record struct DateRange(DateOnly Start, DateOnly End)
{
    public bool Contains(DateOnly date) => date >= Start && date <= End;

    public override string ToString() =>
        $"{Start.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} .. {End.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}";
}

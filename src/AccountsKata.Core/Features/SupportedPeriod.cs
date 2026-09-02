using AccountsKata.Core.Domain;

namespace AccountsKata.Core.Features;

/// <summary>Query window stated by the kata; every adapter validates user input against it.</summary>
public static class SupportedPeriod
{
    public static readonly DateRange Default = new(new DateOnly(2022, 1, 1), new DateOnly(2023, 3, 1));
}

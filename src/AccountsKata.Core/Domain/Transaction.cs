namespace AccountsKata.Core.Domain;

/// <summary>Category is a free string: new categories need no code change.</summary>
public sealed record Transaction(DateOnly Date, Money Amount, string Category)
{
    public bool IsDebit => Amount.Amount < 0m;
}

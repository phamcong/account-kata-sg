namespace AccountsKata.Core.Domain;

/// <summary>The core abstraction: any pair, any rate provider. Features know nothing else.</summary>
public interface ICurrencyConverter
{
    Money Convert(Money money, Currency target);
}

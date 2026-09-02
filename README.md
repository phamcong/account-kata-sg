# Account management

Value of an account at a given date, from a multi-currency statement. Exposed as an HTTP API with
Swagger UI.

## Running

```powershell
dotnet test
dotnet run --project src/AccountsKata.Api
```

Swagger UI opens on <http://localhost:5080>; the OpenAPI document is at `/swagger/v1/swagger.json`.

## Endpoints

| Endpoint | Feature |
| --- | --- |
| `GET /api/balance?date=15/06/2022&currency=USD` | 1 — account value at a date |
| `GET /api/debits/top?count=3&from=&to=&currency=` | 2 — largest debit categories |
| `GET /api/balance/history?from=01/11/2022&to=28/02/2023` | 3 — monthly balance evolution |
| `GET /api/statement` | anchor balance, covered period, categories, declared rates |

Dates are `dd/MM/yyyy` and must fall within 01/01/2022 .. 01/03/2023. `currency` is optional and
defaults to the account currency.

```jsonc
// GET /api/balance?date=28/02/2023
{ "date": "28/02/2023", "amount": 8300.00, "currency": "EUR" }

// GET /api/debits/top?count=3
[{ "rank": 1, "category": "Alimentation", "amount": -567392.63, "currency": "EUR", "transactionCount": 1165 }, ...]

// GET /api/balance?date=28/02/2023&currency=GBP  -> 400 ProblemDetails
{ "title": "Bad Request", "status": 400, "detail": "No exchange rate connects 'EUR' to 'GBP'." }
```

## The core

The CSV file is only one possible source. The business logic knows about two inputs:

```csharp
// 1. the history plus a balance known at a date, in the account currency
var statement = new AccountStatement(
    new DateOnly(2023, 2, 28),
    new Money(8300.00m, Currency.Euro),
    transactions);

// 2. rates, as pairs: 1 From = Value To
ICurrencyConverter converter = new CurrencyPairConverter(
[
    new ExchangeRate(Currency.Parse("JPY"), Currency.Euro, 0.482m),
    new ExchangeRate(Currency.Parse("USD"), Currency.Euro, 1.445m),
]);

new AccountBalanceQuery(statement, converter).At(new DateOnly(2022, 6, 15));
```

Those objects can be built from a file, an API, a database or user input alike — the core tests
never open a file.

## Layout

```
src/AccountsKata.Core        # business logic, no dependency
  Domain/                    # Currency, Money, Transaction, AccountStatement,
                             # ICurrencyConverter + CurrencyPairConverter
  Features/                  # one use case = one class
  Parsing/                   # CSV adapter behind IStatementSource / IExchangeRateSource
src/AccountsKata.Api         # HTTP adapter (minimal API + Swagger UI)
tests/AccountsKata.Tests     # unit tests + end-to-end tests on the provided CSV
```

## Decisions

- **Rates are pairs, not a pivot table**: `CurrencyPairConverter` uses a declared pair directly,
  derives its inverse, and chains missing pairs along the shortest path. It therefore behaves
  identically today (everything quoted against the euro) and tomorrow with a full matrix. Features
  only ever see `Money Convert(Money, Currency)`.
- **Account currency**: totals are accumulated in the currency of the anchor balance, and the
  display currency is applied once, to the final result. Converting every transaction into the
  target currency before summing would multiply rounding errors.
- **Conversion direction**: `A/B : r` is read literally as "1 A = r B". With the CSV
  (`JPY/EUR : 0.482`) this gives `amount_EUR = amount * rate`, the only reading that reproduces the
  three expected categories; the gap between `Habitation` (3rd) and `Communication` (4th) is only
  0.18%. The kata statement writes the pair the other way round (`EUR/JPY`), which is an error in
  that document.
- **The balance is an anchor, not an opening balance**: 8300.00 EUR on 28/02/2023. The value at a
  date is obtained by replaying the history relative to that anchor, in both directions.
- **Dates are inclusive**: the value returned is the one at the *end* of the requested day.
- **`decimal` everywhere**, never `double`; rounding happens on display only.
- **`CultureInfo.InvariantCulture`** for `dd/MM/yyyy` dates and the `.` decimal separator,
  otherwise an `fr-FR` machine reads `-504.61` as `-50461`.

## Extending

| Need | Change |
| --- | --- |
| New currency | one rate line in the statement file — no code |
| Display in another currency | `currency` query parameter, or the `currency` parameter of the features |
| Account held in another currency | nothing, the currency comes from the anchor balance |
| Full rate matrix | nothing, `CurrencyPairConverter` already handles it |
| Another rate provider (live API) | implement `ICurrencyConverter` |
| New category | nothing, a category is a free string |
| New source (JSON, database, user input) | implement `IStatementSource` / `IExchangeRateSource` |
| New feature | a class in `Features/`, one endpoint in `AccountEndpoints` |

Category names (`Alimentation`, `Habitation`, ...) are kept as they appear in the input data.

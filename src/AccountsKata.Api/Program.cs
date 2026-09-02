using AccountsKata.Api;
using AccountsKata.Core.Domain;
using AccountsKata.Core.Features;
using AccountsKata.Core.Parsing;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "Accounts API",
    Version = "v1",
    Description = "Account value at a date, largest debit categories and monthly balance evolution, "
                + "over a multi-currency statement.",
}));

builder.Services.AddSingleton(_ =>
    new CsvFileStatementSource(StatementFileLocator.Resolve(builder.Configuration["Statement:File"])));
builder.Services.AddSingleton<IStatementSource>(sp => sp.GetRequiredService<CsvFileStatementSource>());
builder.Services.AddSingleton<IExchangeRateSource>(sp => sp.GetRequiredService<CsvFileStatementSource>());

builder.Services.AddSingleton(sp => sp.GetRequiredService<IStatementSource>().LoadStatement());
builder.Services.AddSingleton<ICurrencyConverter>(sp =>
    new CurrencyPairConverter(sp.GetRequiredService<IExchangeRateSource>().LoadRates()));

builder.Services.AddSingleton<AccountBalanceQuery>();
builder.Services.AddSingleton<TopDebitCategoriesQuery>();
builder.Services.AddSingleton<BalanceHistoryQuery>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Accounts API v1");
    options.RoutePrefix = string.Empty;
    options.DocumentTitle = "Accounts API";
});

app.MapAccountEndpoints();

app.Run();

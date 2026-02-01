using MockProvider.AccountService.Models.Responses;
using MockProvider.AccountService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<AccountDataGenerator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var dataGenerator = app.Services.GetRequiredService<AccountDataGenerator>();

app.MapGet("/api/accounts/{accountId:long}/balance", (long accountId) =>
{
    var balance = dataGenerator.GetBalance(accountId);
    return Results.Ok(balance);
})
.WithName("GetAccountBalance");

app.Run();
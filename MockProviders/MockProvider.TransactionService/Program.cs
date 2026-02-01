using MockProvider.TransactionService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<TransactionDataGenerator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var dataGenerator = app.Services.GetRequiredService<TransactionDataGenerator>();

app.MapGet("/api/transactions", (
    long accountId,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    int page = 1,
    int pageSize = 50) =>
{
    if (accountId <= 0)
        return Results.BadRequest(new { message = "accountId is required and must be greater than 0" });

    var response = dataGenerator.GetTransactions(accountId, fromDate, toDate, page, pageSize);

    return Results.Ok(response);
})
.WithName("GetTransactions");

app.Run();
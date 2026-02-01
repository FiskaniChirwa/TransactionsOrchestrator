using MockProvider.CustomerService.Models.Responses;
using MockProvider.CustomerService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<CustomerDataGenerator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var dataGenerator = app.Services.GetRequiredService<CustomerDataGenerator>();

// Note: customerId is now long instead of string
app.MapGet("/api/customers/{customerId:long}", (long customerId) =>
{
    var customer = dataGenerator.GetCustomer(customerId);

    if (customer == null)
        return Results.NotFound(new { message = $"Customer {customerId} not found" });

    return Results.Ok(customer);
})
.WithName("GetCustomer");

app.MapGet("/api/customers/{customerId:long}/accounts", (long customerId) =>
{
    var accounts = dataGenerator.GetCustomerAccounts(customerId);

    if (accounts == null)
        return Results.NotFound(new { message = $"Customer {customerId} not found" });

    return Results.Ok(accounts);
})
.WithName("GetCustomerAccounts");

app.Run();
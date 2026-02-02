using Bogus;
using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Tests.Helpers;

public static class TestData
{
    public static CustomerResponse GenerateCustomer(long? customerId = null)
    {
        var faker = new Faker();
        
        return new CustomerResponse
        {
            CustomerId = customerId ?? faker.Random.Long(1000, 9999),
            FirstName = faker.Name.FirstName(),
            LastName = faker.Name.LastName(),
            Email = faker.Internet.Email(),
            PhoneNumber = faker.Phone.PhoneNumber(),
            DateOfBirth = DateOnly.FromDateTime(faker.Date.Past(40, DateTime.Now.AddYears(-18)))
        };
    }

    public static List<AccountResponse> GenerateAccounts(long customerId, int count = 2)
    {
        var faker = new Faker();
        var accounts = new List<AccountResponse>();

        for (int i = 0; i < count; i++)
        {
            accounts.Add(new AccountResponse
            {
                AccountId = 10000 + i,
                AccountNumber = $"ACC-{customerId}-{i + 1:D4}",
                AccountType = faker.PickRandom("Checking", "Savings"),
                Currency = "ZAR",
                Status = "Active",
                OpenedDate = new DateTime()
            });
        }

        return accounts;
    }

    public static List<TransactionResponse> GenerateTransactions(long accountId, int count = 10)
    {
        var faker = new Faker();
        var transactions = new List<TransactionResponse>();
        var merchants = new[] { "WOOLWORTHS", "UBER", "NETFLIX", "SHELL", "MCDONALDS" };
        var mccCodes = new[] { "5411", "4121", "7995", "5541", "5814" };

        for (int i = 0; i < count; i++)
        {
            var merchantIndex = faker.Random.Int(0, merchants.Length - 1);
            
            transactions.Add(new TransactionResponse
            {
                TransactionId = 100000 + i,
                AccountId = accountId,
                Date = faker.Date.Recent(90),
                Amount = Math.Round(faker.Random.Decimal(-500, -10), 2),
                Currency = "ZAR",
                Description = $"POS Purchase - {merchants[merchantIndex]}",
                Type = "Debit",
                Status = "Completed",
                MerchantName = merchants[merchantIndex],
                MerchantCode = mccCodes[merchantIndex],
                MerchantCategory = null,
                BalanceAfter = Math.Round(faker.Random.Decimal(1000, 50000), 2)
            });
        }

        return transactions.OrderBy(t => t.Date).ToList();
    }
}
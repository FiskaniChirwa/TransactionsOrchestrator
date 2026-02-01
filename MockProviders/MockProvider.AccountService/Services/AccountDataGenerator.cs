using Bogus;
using MockProvider.AccountService.Models.Responses;
using MockProvider.Shared.Constants;

namespace MockProvider.AccountService.Services;

public class AccountDataGenerator
{
    private readonly Dictionary<long, AccountBalanceResponse> _balances;

    public AccountDataGenerator()
    {
        _balances = [];

        var faker = new Faker();

        // Generate balances ONLY for FIXED account IDs
        foreach (var accountId in MockDataConstants.AllAccountIds)
        {
            _balances[accountId] = GetBalance(faker, accountId);
        }
    }

    private AccountBalanceResponse GetBalance(Faker faker, long accountId)
    {
        var template = MockDataConstants.AccountTemplates[accountId];

        // Different balance ranges based on account type
        var (minBalance, maxBalance) = template.AccountType switch
        {
            "Checking" => (1000m, 30000m),
            "Savings" => (5000m, 100000m),
            "Investment" => (10000m, 500000m),
            _ => (1000m, 50000m)
        };

        var currentBalance = Math.Round(faker.Random.Decimal(minBalance, maxBalance), 2);
        var availableBalance = Math.Round(currentBalance * faker.Random.Decimal(0.8m, 1.0m), 2);

        return new AccountBalanceResponse
        {
            AccountId = accountId,
            CurrentBalance = currentBalance,
            AvailableBalance = availableBalance,
            Currency = "ZAR",
            LastUpdated = DateTime.UtcNow
        };
    }

    public AccountBalanceResponse? GetBalance(long accountId)
    {
        return _balances.GetValueOrDefault(accountId);
    }
}
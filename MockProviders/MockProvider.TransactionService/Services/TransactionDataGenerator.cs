using Bogus;
using MockProvider.TransactionService.Models.Responses;
using MockProvider.Shared.Constants;

namespace MockProvider.TransactionService.Services;

public class TransactionDataGenerator
{
    private readonly Dictionary<long, List<TransactionResponse>> _transactionsByAccount;
    private static long _transactionIdCounter = 100000;

    public TransactionDataGenerator()
    {
        _transactionsByAccount = [];

        foreach (var accountId in MockDataConstants.AllAccountIds)
        {
            _transactionsByAccount[accountId] = GenerateTransactionsForAccount(accountId);
        }
    }

    private List<TransactionResponse> GenerateTransactionsForAccount(long accountId)
    {
        var faker = new Faker();
        var transactionCount = faker.Random.Int(1, 10);
        var startDate = DateTime.UtcNow.AddMonths(-3);
        var currentBalance = faker.Random.Decimal(5000, 50000);

        var transactions = new List<TransactionResponse>();

        // Get account template to determine behavior
        var template = MockDataConstants.AccountTemplates[accountId];

        // Define merchant data with MCC codes
        var merchants = new[]
        {
            // Groceries (MCC 5411)
            new { Name = "WOOLWORTHS", Code = "5411", MinAmount = -50, MaxAmount = -800 },
            new { Name = "CHECKERS", Code = "5411", MinAmount = -40, MaxAmount = -600 },
            new { Name = "PICK N PAY", Code = "5411", MinAmount = -45, MaxAmount = -700 },
            new { Name = "SPAR", Code = "5411", MinAmount = -30, MaxAmount = -500 },
            
            // Restaurants (MCC 5812)
            new { Name = "OCEAN BASKET", Code = "5812", MinAmount = -100, MaxAmount = -500 },
            new { Name = "SPUR STEAK RANCH", Code = "5812", MinAmount = -120, MaxAmount = -600 },
            
            // Fast Food (MCC 5814)
            new { Name = "MCDONALD'S", Code = "5814", MinAmount = -40, MaxAmount = -200 },
            new { Name = "KFC", Code = "5814", MinAmount = -45, MaxAmount = -180 },
            new { Name = "NANDO'S", Code = "5814", MinAmount = -60, MaxAmount = -250 },
            
            // Transport - Fuel (MCC 5541)
            new { Name = "SHELL", Code = "5541", MinAmount = -200, MaxAmount = -800 },
            new { Name = "ENGEN", Code = "5541", MinAmount = -200, MaxAmount = -750 },
            new { Name = "BP", Code = "5541", MinAmount = -200, MaxAmount = -800 },
            
            // Transport - Ride Services (MCC 4121)
            new { Name = "UBER", Code = "4121", MinAmount = -50, MaxAmount = -300 },
            new { Name = "BOLT", Code = "4121", MinAmount = -40, MaxAmount = -250 },
            
            // Entertainment - Streaming (MCC 7995)
            new { Name = "NETFLIX", Code = "7995", MinAmount = -99, MaxAmount = -200 },
            new { Name = "SPOTIFY", Code = "7995", MinAmount = -60, MaxAmount = -120 },
            new { Name = "DSTV", Code = "7995", MinAmount = -300, MaxAmount = -800 },
            
            // Entertainment - Cinema (MCC 7832)
            new { Name = "STER KINEKOR", Code = "7832", MinAmount = -80, MaxAmount = -300 },
            
            // Utilities - Telecom (MCC 4814)
            new { Name = "VODACOM", Code = "4814", MinAmount = -200, MaxAmount = -1000 },
            new { Name = "MTN", Code = "4814", MinAmount = -200, MaxAmount = -1000 },
            
            // Utilities - Electric/Water (MCC 4900)
            new { Name = "CITY OF CAPE TOWN", Code = "4900", MinAmount = -500, MaxAmount = -2000 },
            new { Name = "ESKOM", Code = "4900", MinAmount = -800, MaxAmount = -3000 },
            
            // Retail - Department Stores (MCC 5311)
            new { Name = "EDGARS", Code = "5311", MinAmount = -200, MaxAmount = -2000 },
            
            // Retail - Clothing (MCC 5651)
            new { Name = "MR PRICE", Code = "5651", MinAmount = -100, MaxAmount = -1500 },
            new { Name = "ACKERMANS", Code = "5651", MinAmount = -100, MaxAmount = -1200 },
            
            // Online Retail (MCC 5999)
            new { Name = "TAKEALOT", Code = "5999", MinAmount = -100, MaxAmount = -3000 },
            
            // Pharmacy (MCC 5912)
            new { Name = "CLICKS PHARMACY", Code = "5912", MinAmount = -50, MaxAmount = -800 },
            new { Name = "DIS-CHEM", Code = "5912", MinAmount = -60, MaxAmount = -900 }
        };

        var transactionFaker = new Faker<TransactionResponse>()
            .RuleFor(t => t.TransactionId, f => Interlocked.Increment(ref _transactionIdCounter))
            .RuleFor(t => t.AccountId, f => accountId)
            .RuleFor(t => t.Date, f => f.Date.Between(startDate, DateTime.UtcNow))
            .RuleFor(t => t.Currency, f => "ZAR")
            .RuleFor(t => t.Type, f => f.PickRandom("Debit", "Credit"))
            .RuleFor(t => t.Status, f => f.PickRandom(new[] { "Completed" }.Concat(
                faker.Random.Bool(0.95f) ? new[] { "Completed" } : new[] { "Pending", "Failed" }
            ).ToArray()))
            .RuleFor(t => t.Description, (f, t) => $"POS Purchase - {t.MerchantName}");

        for (int i = 0; i < transactionCount; i++)
        {
            var transaction = transactionFaker.Generate();

            // Adjust income probability based on account type
            var incomeProbability = template.AccountType switch
            {
                "Checking" => 0.15f,    // 15% income (salary deposits)
                "Savings" => 0.20f,     // 20% income (transfers in, interest)
                "Investment" => 0.30f,  // 30% income (dividends, returns)
                _ => 0.10f
            };

            if (faker.Random.Bool(incomeProbability))
            {
                // Income transaction
                transaction.Amount = Math.Round(faker.Random.Decimal(5000, 25000), 2);
                transaction.MerchantName = null;
                transaction.MerchantCode = null;
                transaction.Description = faker.PickRandom(
                    "SALARY DEPOSIT",
                    "FREELANCE PAYMENT",
                    "INVESTMENT RETURN",
                    "BANK INTEREST",
                    "TRANSFER IN"
                );
                transaction.Type = "Credit";
                transaction.MerchantCategory = null;
            }
            else
            {
                // Expense transaction
                var merchant = faker.PickRandom(merchants);
                transaction.Amount = Math.Round(faker.Random.Decimal((decimal)merchant.MinAmount, (decimal)merchant.MaxAmount), 2);
                transaction.MerchantName = $"{merchant.Name} {faker.Address.City().ToUpper()}";
                transaction.MerchantCode = merchant.Code;
                transaction.MerchantCategory = null;
                transaction.Type = "Debit";
            }

            currentBalance += transaction.Amount;
            transaction.BalanceAfter = Math.Round(currentBalance, 2);

            transactions.Add(transaction);
        }

        return transactions.OrderBy(t => t.Date).ToList();
    }

    public TransactionListResponse GetTransactions(
        long accountId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50)
    {
        if (!_transactionsByAccount.TryGetValue(accountId, out var transactions))
        {
            return new TransactionListResponse
            {
                AccountId = accountId,
                Transactions = new List<TransactionResponse>(),
                PageInfo = new PageInfo
                {
                    TotalCount = 0,
                    Page = page,
                    PageSize = pageSize
                }
            };
        }

        var filtered = transactions.AsEnumerable();

        if (fromDate.HasValue)
            filtered = filtered.Where(t => t.Date >= fromDate.Value);

        if (toDate.HasValue)
            filtered = filtered.Where(t => t.Date <= toDate.Value);

        var filteredList = filtered.ToList();
        var totalCount = filteredList.Count;

        var paginatedTransactions = filteredList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new TransactionListResponse
        {
            AccountId = accountId,
            Transactions = paginatedTransactions,
            PageInfo = new PageInfo
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            }
        };
    }
}
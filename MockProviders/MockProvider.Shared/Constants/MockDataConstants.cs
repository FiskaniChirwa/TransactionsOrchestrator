namespace MockProvider.Shared.Constants;

public static class MockDataConstants
{
    // Customer IDs
    public static readonly long[] CustomerIds = [1001, 1002, 1003];

    // Customer to Accounts mapping
    public static readonly Dictionary<long, long[]> CustomerAccounts = new()
    {
        { 1001, new[] { 10000L, 10001L } },           // John - 2 accounts
        { 1002, new[] { 10002L } },                   // Sarah - 1 account
        { 1003, new[] { 10003L, 10004L, 10005L } }    // Michael - 3 accounts
    };

    // Account details
    public static readonly Dictionary<long, AccountTemplate> AccountTemplates = new()
    {
        { 10000, new AccountTemplate(1001, "ACC-1001-0001", "Checking", "2020-01-15") },
        { 10001, new AccountTemplate(1001, "ACC-1001-0002", "Savings", "2020-01-15") },
        { 10002, new AccountTemplate(1002, "ACC-1002-0001", "Checking", "2021-03-10") },
        { 10003, new AccountTemplate(1003, "ACC-1003-0001", "Checking", "2019-06-20") },
        { 10004, new AccountTemplate(1003, "ACC-1003-0002", "Savings", "2019-06-20") },
        { 10005, new AccountTemplate(1003, "ACC-1003-0003", "Investment", "2020-12-01") }
    };

    // All account IDs
    public static readonly long[] AllAccountIds = [10000, 10001, 10002, 10003, 10004, 10005];
}

public record AccountTemplate(long CustomerId, string AccountNumber, string AccountType, string OpenedDate);
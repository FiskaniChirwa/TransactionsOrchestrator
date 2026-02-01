using Bogus;
using MockProvider.CustomerService.Models.Responses;
using MockProvider.Shared.Constants;

namespace MockProvider.CustomerService.Services;

public class CustomerDataGenerator
{
    private readonly Dictionary<long, CustomerResponse> _customers;

    public CustomerDataGenerator()
    {
        _customers = [];

        var faker = new Faker();

        // Generate data for each customer using FIXED customer IDs
        foreach (var customerId in MockDataConstants.CustomerIds)
        {
            var customer = GenerateCustomer(faker, customerId);
            _customers[customerId] = customer;
        }
    }

    private CustomerResponse GenerateCustomer(Faker faker, long customerId)
    {
        var firstName = faker.Name.FirstName();
        var lastName = faker.Name.LastName();

        return new CustomerResponse
        {
            CustomerId = customerId,
            FirstName = firstName,
            LastName = lastName,
            Email = faker.Internet.Email(firstName, lastName).ToLower(),
            PhoneNumber = faker.Phone.PhoneNumber("+27 ## ### ####"),
            DateOfBirth = DateOnly.FromDateTime(faker.Date.Past(40, DateTime.Now.AddYears(-18))),
        };
    }

    public CustomerResponse? GetCustomer(long customerId)
    {
        return _customers.GetValueOrDefault(customerId);
    }

    public CustomerAccountsResponse? GetCustomerAccounts(long customerId)
    {
        if (!_customers.ContainsKey(customerId))
            return null;

        var accountIds = MockDataConstants.CustomerAccounts[customerId];

        var accounts = accountIds.Select(accountId =>
        {
            var template = MockDataConstants.AccountTemplates[accountId];

            return new AccountResponse
            {
                AccountId = accountId,
                AccountNumber = template.AccountNumber,
                AccountType = template.AccountType,
                Currency = "ZAR",
                Status = "Active",
                OpenedDate = DateTime.Parse(template.OpenedDate)
            };
        }).ToList();

        return new CustomerAccountsResponse
        {
            CustomerId = customerId,
            Accounts = accounts
        };
    }
}
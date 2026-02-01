using Microsoft.Extensions.Logging;
using TransactionAggregation.Models.Common;
using TransactionAggregation.Models.Responses;
using TransactionAggregation.Core.Services;
using TransactionAggregation.Infrastructure.Clients;
using TransactionAggregation.Infrastructure.Outbox;
using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Core.Services;

public class TransactionAggregationService : ITransactionAggregationService
{
    private readonly ICustomerApiClient _customerApiClient;
    private readonly ITransactionApiClient _transactionApiClient;
    private readonly IAccountApiClient _accountApiClient;
    private readonly IDocumentApiClient _documentApiClient;
    private readonly ICategorizationService _categorizationService;
    private readonly IOutboxPublisher _outboxPublisher;
    private readonly ILogger<TransactionAggregationService> _logger;

    public TransactionAggregationService(
        ICustomerApiClient customerApiClient,
        ITransactionApiClient transactionApiClient,
        IAccountApiClient accountApiClient,
        IDocumentApiClient documentApiClient,
        ICategorizationService categorizationService,
        IOutboxPublisher outboxPublisher,
        ILogger<TransactionAggregationService> logger)
    {
        _customerApiClient = customerApiClient;
        _transactionApiClient = transactionApiClient;
        _accountApiClient = accountApiClient;
        _documentApiClient = documentApiClient;
        _categorizationService = categorizationService;
        _outboxPublisher = outboxPublisher;
        _logger = logger;
    }

    public async Task<Result<AggregatedTransactionResponse>> GetCustomerTransactionsAsync(
        long customerId,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        try
        {
            // 1. Get customer info
            var customerResult = await _customerApiClient.GetCustomerAsync(customerId);
            if (!customerResult.Success)
            {
                return Result<AggregatedTransactionResponse>.FailureResult(
                    customerResult.ErrorMessage!,
                    customerResult.ErrorCode!);
            }

            // 2. Get customer accounts
            var accountsResult = await _customerApiClient.GetCustomerAccountsAsync(customerId);
            if (!accountsResult.Success)
            {
                return Result<AggregatedTransactionResponse>.FailureResult(
                    accountsResult.ErrorMessage!,
                    accountsResult.ErrorCode!);
            }

            var customer = customerResult.Data!;
            var accounts = accountsResult.Data!;

            // 3. For each account, get transactions and balance
            var accountsWithTransactions = new List<AccountWithTransactions>();
            var allTransactions = new List<TransactionResponse>();

            foreach (var account in accounts)
            {
                // Get transactions for account
                var transactionsResult = await _transactionApiClient.GetTransactionsAsync(
                    account.AccountId,
                    fromDate,
                    toDate);

                if (!transactionsResult.Success)
                {
                    _logger.LogWarning(
                        "Failed to get transactions for account {AccountId}: {Error}",
                        account.AccountId,
                        transactionsResult.ErrorMessage);
                    continue;
                }

                var transactions = transactionsResult.Data!.Transactions;

                // Categorize transactions
                foreach (var transaction in transactions.Where(t => string.IsNullOrEmpty(t.MerchantCategory)))
                {
                    transaction.MerchantCategory = _categorizationService.CategorizeTransaction(transaction);
                }

                // Get current balance
                var balanceResult = await _accountApiClient.GetAccountBalanceAsync(account.AccountId);

                var accountWithTransactions = new AccountWithTransactions
                {
                    AccountId = account.AccountId,
                    AccountType = account.AccountType,
                    AccountNumber = account.AccountNumber,
                    Currency = account.Currency,
                    CurrentBalance = balanceResult.Success ? balanceResult.Data!.CurrentBalance : 0,
                    AvailableBalance = balanceResult.Success ? balanceResult.Data!.AvailableBalance : 0,
                    Transactions = transactions
                };

                accountsWithTransactions.Add(accountWithTransactions);
                allTransactions.AddRange(transactions);
            }

            // 4. Build aggregated response
            var response = new AggregatedTransactionResponse
            {
                CustomerId = customer.CustomerId,
                CustomerName = $"{customer.FirstName} {customer.LastName}",
                Accounts = accountsWithTransactions,
                TotalTransactions = allTransactions.Count,
                DateRange = fromDate.HasValue || toDate.HasValue
                    ? new DateRangeFilter(fromDate, toDate)
                    : null
            };

            // 5. Publish transactions to Fraud Engine via Outbox
            if (allTransactions.Any())
            {
                await _outboxPublisher.PublishTransactionsAsync(allTransactions, customerId);
            }

            return Result<AggregatedTransactionResponse>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error aggregating transactions for customer {CustomerId}",
                customerId);

            return Result<AggregatedTransactionResponse>.FailureResult(
                "Failed to aggregate transactions",
                "AGGREGATION_ERROR");
        }
    }

    public async Task<Result<TransactionSummaryResponse>> GetTransactionSummaryAsync(
        long customerId,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        try
        {
            var aggregatedResult = await GetCustomerTransactionsAsync(customerId, fromDate, toDate);

            if (!aggregatedResult.Success)
            {
                return Result<TransactionSummaryResponse>.FailureResult(
                    aggregatedResult.ErrorMessage!,
                    aggregatedResult.ErrorCode!);
            }

            var aggregatedData = aggregatedResult.Data!;

            var allTransactions = aggregatedData.Accounts
                .SelectMany(a => a.Transactions)
                .ToList();

            var totalDebits = allTransactions
                .Where(t => t.Type?.Equals("Debit", StringComparison.OrdinalIgnoreCase) ?? false)
                .Sum(t => t.Amount);

            var totalCredits = allTransactions
                .Where(t => t.Type?.Equals("Credit", StringComparison.OrdinalIgnoreCase) ?? false)
                .Sum(t => t.Amount);

            var categorySummaries = allTransactions
                .GroupBy(t => t.MerchantCategory ?? "Uncategorized")
                .Select(g => new CategorySummary
                {
                    Category = g.Key,
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(t => t.Amount),
                    AverageTransactionAmount = g.Average(t => t.Amount),
                    PercentageOfTotal = totalDebits > 0
                    ? (g.Sum(t => t.Amount) / totalDebits) * 100
                    : 0
                })
                .OrderByDescending(c => c.TotalAmount)
                .ToList();

            var summary = new TransactionSummaryResponse
            {
                CustomerId = aggregatedData.CustomerId,
                CustomerName = aggregatedData.CustomerName,
                TotalDebits = totalDebits,
                TotalCredits = totalCredits,
                NetAmount = totalCredits - totalDebits,
                CategorySummaries = categorySummaries,
                DateRange = aggregatedData.DateRange
            };

            return Result<TransactionSummaryResponse>.SuccessResult(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error generating transaction summary for customer {CustomerId}",
                customerId);

            return Result<TransactionSummaryResponse>.FailureResult(
                "Failed to generate transaction summary",
                "SUMMARY_ERROR");
        }
    }

    public async Task<Result<StatementGenerationResponse>> GenerateTransactionStatementAsync(
        long customerId,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        try
        {
            // 1. Get aggregated transaction data
            var aggregatedResult = await GetCustomerTransactionsAsync(customerId, fromDate, toDate);

            if (!aggregatedResult.Success)
            {
                return Result<StatementGenerationResponse>.FailureResult(
                    aggregatedResult.ErrorMessage!,
                    aggregatedResult.ErrorCode!);
            }

            var aggregatedData = aggregatedResult.Data!;

            // 2. Transform to Document Service format
            var documentRequest = new GenerateDocumentRequest
            {
                DocumentType = DocumentType.TransactionStatement,
                Data = new Dictionary<string, object>
                {
                    ["customerId"] = aggregatedData.CustomerId,
                    ["customerName"] = aggregatedData.CustomerName,
                    ["accounts"] = aggregatedData.Accounts,
                    ["totalTransactions"] = aggregatedData.TotalTransactions,
                    ["dateRange"] = aggregatedData.DateRange ?? new DateRangeFilter(fromDate, toDate)
                },
                Options = new DocumentOptions
                {
                    TokenExpiryMinutes = 60,
                    MaxDownloads = 5
                }
            };

            // 3. Call Document Service
            Result<GenerateDocumentResponse> documentResult;
            try
            {
                documentResult = await _documentApiClient.GenerateDocumentAsync(documentRequest);

                // FIX: Check the Success property of the Result wrapper
                if (documentResult == null || !documentResult.Success)
                {
                    return Result<StatementGenerationResponse>.FailureResult(
                        documentResult?.ErrorMessage ?? "Failed to generate document.",
                        documentResult?.ErrorCode ?? "DOCUMENT_SERVICE_ERROR");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Document Service unavailable for customer {CustomerId}", customerId);
                return Result<StatementGenerationResponse>.FailureResult(
                    "The statement generation service is temporarily unavailable.",
                    "DOCUMENT_SERVICE_UNAVAILABLE");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Document Service timeout for customer {CustomerId}", customerId);
                return Result<StatementGenerationResponse>.FailureResult(
                    "The statement generation request took too long.",
                    "DOCUMENT_SERVICE_TIMEOUT");
            }

            // 4. Build response (Safe to access .Data now)
            var documentData = documentResult.Data!;
            var response = new StatementGenerationResponse
            {
                StatementId = documentData.DocumentId,
                DownloadUrl = documentData.DownloadUrl,
                ExpiresAt = documentData.ExpiresAt,
                FileName = documentData.FileName,
                MaxDownloads = documentData.MaxDownloads
            };

            return Result<StatementGenerationResponse>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating statement for customer {CustomerId}", customerId);
            return Result<StatementGenerationResponse>.FailureResult(
                "Failed to generate transaction statement",
                "STATEMENT_GENERATION_ERROR");
        }
    }
}
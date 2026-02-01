

using TransactionAggregation.Models.Common;
using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Core.Services;

public class CategorizationService : ICategorizationService
{
    public List<CategorySummary> GroupTransactionsByCategory(List<TransactionResponse> transactions)
    {
        if (transactions.Count == 0)
            return [];

        // First, categorize all transactions that don't have a category
        foreach (var transaction in transactions.Where(t => string.IsNullOrEmpty(t.MerchantCategory)))
        {
            transaction.MerchantCategory = CategorizeTransaction(transaction);
        }

        // Group by category and calculate summaries
        var grouped = transactions
            .GroupBy(t => t.MerchantCategory)
            .Select(g => new CategorySummary
            {
                Category = g.Key ?? "Unknown",
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                AverageTransactionAmount = g.Average(t => t.Amount)
            })
            .ToList();

        // Calculate percentages (based on total debits only for spending categories)
        var totalDebits = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));

        foreach (var summary in grouped)
        {
            if (totalDebits > 0 && summary.TotalAmount < 0)
            {
                // For spending categories, calculate percentage of total spending
                summary.PercentageOfTotal = Math.Abs(summary.TotalAmount) / totalDebits * 100;
            }
            else if (summary.TotalAmount > 0)
            {
                // For income categories, show as positive percentage
                var totalCredits = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
                summary.PercentageOfTotal = totalCredits > 0
                    ? summary.TotalAmount / totalCredits * 100
                    : 0;
            }
        }

        // Sort by absolute amount (largest spending/income first)
        return [.. grouped.OrderByDescending(c => Math.Abs(c.TotalAmount))];
    }

    public Dictionary<string, List<TransactionResponse>> GroupByCategory(List<TransactionResponse> transactions)
    {
        // Categorize transactions that don't have a category
        foreach (var transaction in transactions.Where(t => string.IsNullOrEmpty(t.MerchantCategory)))
        {
            transaction.MerchantCategory = CategorizeTransaction(transaction);
        }

        return transactions
            .GroupBy(t => t.MerchantCategory ?? "Uncategorized")
            .ToDictionary(
                g => g.Key,
                g => g.ToList()
            );
    }

    public decimal CalculateTotalForCategory(string category, List<TransactionResponse> transactions)
    {
        return transactions
            .Where(t => t.MerchantCategory?.Equals(category, StringComparison.OrdinalIgnoreCase) ?? false)
            .Sum(t => t.Amount);
    }

    public string CategorizeTransaction(TransactionResponse transaction)
    {
        // Income detection (positive amount)
        if (transaction.Amount > 0)
        {
            return CategorizeIncome(transaction);
        }

        // Try MCC code first (if available)
        if (!string.IsNullOrEmpty(transaction.MerchantCode))
        {
            var categoryFromMcc = CategorizeByMCC(transaction.MerchantCode);
            if (categoryFromMcc != "Other")
                return categoryFromMcc;
        }

        // Fall back to merchant name analysis
        if (!string.IsNullOrEmpty(transaction.MerchantName))
        {
            return CategorizeByMerchantName(transaction.MerchantName);
        }

        return "Other";
    }

    private static string CategorizeIncome(TransactionResponse transaction)
    {
        var description = transaction.Description?.ToUpper() ?? "";
        var merchantName = transaction.MerchantName?.ToUpper() ?? "";

        // Salary
        if (description.Contains("SALARY") || description.Contains("PAYROLL") ||
            merchantName.Contains("SALARY") || merchantName.Contains("PAYROLL"))
            return "Income";

        // Investment returns
        if (description.Contains("DIVIDEND") || description.Contains("INTEREST") ||
            description.Contains("INVESTMENT"))
            return "Income";

        // Refunds
        if (description.Contains("REFUND") || description.Contains("REVERSAL") ||
            merchantName.Contains("REFUND"))
            return "Refund";

        // Freelance/Other payments
        if (description.Contains("PAYMENT") || description.Contains("TRANSFER"))
            return "Income";

        return "Income";
    }

    private static string CategorizeByMCC(string mccCode)
    {
        return mccCode switch
        {
            // Groceries
            "5411" or "5422" => "Groceries",

            // Restaurants & Food
            "5812" or "5814" => "Restaurants",

            // Transport
            "5541" or "5542" or "5172" or "4121" => "Transport",

            // Utilities
            "4900" or "4814" => "Utilities",

            // Entertainment
            "7832" or "7995" or "7929" => "Entertainment",

            // Retail/Shopping
            "5311" or "5651" or "5944" or "5999" => "Shopping",

            // Health/Pharmacy
            "5912" or "8011" => "Health",

            // Services
            "0742" or "7230" or "7298" => "Services",

            // Financial/ATM
            "6010" or "6011" => "Financial",

            _ => "Other"
        };
    }

    private static string CategorizeByMerchantName(string merchantName)
    {
        var name = merchantName.ToUpper();

        // Groceries
        if (name.Contains("WOOLWORTHS") || name.Contains("CHECKERS") ||
            name.Contains("PICK N PAY") || name.Contains("SPAR") ||
            name.Contains("FOOD") || name.Contains("MARKET"))
            return "Groceries";

        // Restaurants
        if (name.Contains("RESTAURANT") || name.Contains("MCDONALD") ||
            name.Contains("KFC") || name.Contains("NANDO") ||
            name.Contains("STEERS") || name.Contains("WIMPY") ||
            name.Contains("SPUR") || name.Contains("OCEAN BASKET"))
            return "Restaurants";

        // Transport
        if (name.Contains("UBER") || name.Contains("BOLT") ||
            name.Contains("SHELL") || name.Contains("ENGEN") ||
            name.Contains("BP") || name.Contains("TOTAL") ||
            name.Contains("FUEL") || name.Contains("PETROL"))
            return "Transport";

        // Entertainment
        if (name.Contains("CINEMA") || name.Contains("NETFLIX") ||
            name.Contains("SPOTIFY") || name.Contains("DSTV") ||
            name.Contains("STER KINEKOR") || name.Contains("NU METRO"))
            return "Entertainment";

        // Utilities
        if (name.Contains("CITY OF") || name.Contains("ESKOM") ||
            name.Contains("VODACOM") || name.Contains("MTN") ||
            name.Contains("TELKOM") || name.Contains("MUNICIPALITY"))
            return "Utilities";

        // Shopping
        if (name.Contains("EDGARS") || name.Contains("MR PRICE") ||
            name.Contains("ACKERMANS") || name.Contains("TAKEALOT") ||
            name.Contains("AMAZON"))
            return "Shopping";

        // Health
        if (name.Contains("PHARMACY") || name.Contains("CLICKS") ||
            name.Contains("DIS-CHEM") || name.Contains("DOCTOR") ||
            name.Contains("HOSPITAL") || name.Contains("CLINIC"))
            return "Health";

        return "Other";
    }
}
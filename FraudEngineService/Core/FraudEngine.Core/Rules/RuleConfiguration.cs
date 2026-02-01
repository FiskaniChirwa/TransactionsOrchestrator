namespace FraudEngine.Core.Rules;

public class RuleConfiguration
{
    public Dictionary<string, decimal> CategoryAmountThresholds { get; set; } = new()
    {
        { "Groceries", 5000m },
        { "Restaurants", 3000m },
        { "Entertainment", 2000m },
        { "Shopping", 10000m },
        { "Travel", 20000m },
        { "Default", 5000m }
    };

    public int UnusualHourStart { get; set; } = 23;
    public int UnusualHourEnd { get; set; } = 6;

    public int VelocityWindowMinutes { get; set; } = 60;
    public int VelocityThreshold { get; set; } = 5;

    public List<string> HighRiskCategories { get; set; } = new()
    {
        "Cryptocurrency",
        "Gambling",
        "Online Gaming",
        "Wire Transfers",
        "Money Transfer Services"
    };

    public int HighAmountRuleWeight { get; set; } = 30;
    public int UnusualTimeRuleWeight { get; set; } = 15;
    public int VelocityRuleWeight { get; set; } = 35;
    public int FirstTimeCategoryWeight { get; set; } = 10;
    public int HighRiskCategoryWeight { get; set; } = 40;
}
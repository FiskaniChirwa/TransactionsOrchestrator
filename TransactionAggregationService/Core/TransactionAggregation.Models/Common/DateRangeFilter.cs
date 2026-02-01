namespace TransactionAggregation.Models.Common;

public class DateRangeFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public DateRangeFilter() { }

    public DateRangeFilter(DateTime? fromDate, DateTime? toDate)
    {
        FromDate = fromDate;
        ToDate = toDate;
    }

    public override string ToString()
    {
        if (FromDate.HasValue && ToDate.HasValue)
            return $"{FromDate.Value:yyyy-MM-dd} to {ToDate.Value:yyyy-MM-dd}";
        
        if (FromDate.HasValue)
            return $"From {FromDate.Value:yyyy-MM-dd}";
        
        if (ToDate.HasValue)
            return $"Until {ToDate.Value:yyyy-MM-dd}";
        
        return "All time";
    }
}
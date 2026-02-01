namespace FraudEngine.Models.Requests;

public class GetAlertsRequest
{
    public long? CustomerId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}
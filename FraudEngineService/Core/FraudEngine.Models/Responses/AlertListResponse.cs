namespace FraudEngine.Models.Responses;

public class AlertListResponse
{
    public List<FraudAlertResponse> Data { get; set; } = [];
    public int Count { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public long? CustomerId { get; set; }
}
using System.Net;
using System.Net.Http.Json;
using FraudEngine.Core.Common;
using FraudEngine.Core.Repositories;
using FraudEngine.Core.Services;
using FraudEngine.Models.Events;
using FraudEngine.Models.Responses;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using FluentAssertions;

namespace FraudEngine.Tests.Integration.Routes;

public class FraudRoutesTests : IClassFixture<WebApplicationFactory<global::Program>>
{
    private readonly WebApplicationFactory<global::Program> _factory;
    private readonly Mock<IProcessedEventRepository> _processedEventRepoMock = new();
    private readonly Mock<IFraudAnalysisService> _fraudServiceMock = new();

    public FraudRoutesTests(WebApplicationFactory<global::Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => _processedEventRepoMock.Object);
                services.AddScoped(_ => _fraudServiceMock.Object);
            });
        });
    }

    [Fact]
    public async Task Analyze_NewTransaction_ReturnsOkAndStoresResult()
    {
        // Arrange
        var client = _factory.CreateClient();
        var eventId = Guid.NewGuid().ToString();
        var txEvent = new TransactionEvent { TransactionId = 500, Amount = 1000m };
        var analysisResponse = new FraudAnalysisResponse { TransactionId = 500, IsFraudulent = false, RiskScore = 5 };

        _processedEventRepoMock.Setup(x => x.GetByEventIdAsync(eventId)).ReturnsAsync((FraudAnalysisResponse?)null);
        _fraudServiceMock.Setup(x => x.AnalyzeAsync(It.IsAny<FraudEngine.Core.Models.Transaction>()))
                         .ReturnsAsync(Result<FraudAnalysisResponse>.SuccessResult(analysisResponse));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/fraud/analyze")
        {
            Content = JsonContent.Create(txEvent)
        };
        request.Headers.Add("X-Event-Id", eventId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FraudAnalysisResponse>();
        result!.TransactionId.Should().Be(500);
        
        // Verify it was stored for idempotency
        _processedEventRepoMock.Verify(x => x.AddAsync(eventId, 500, It.IsAny<FraudAnalysisResponse>()), Times.Once);
    }

    [Fact]
    public async Task Analyze_MissingEventIdHeader_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var txEvent = new TransactionEvent { TransactionId = 500 };

        // Act - No "X-Event-Id" header added
        var response = await client.PostAsJsonAsync("/api/fraud/analyze", txEvent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("MISSING_EVENT_ID");
    }
}
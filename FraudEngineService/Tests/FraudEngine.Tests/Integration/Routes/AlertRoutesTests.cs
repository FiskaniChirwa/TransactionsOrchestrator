using System.Net;
using System.Net.Http.Json;
using FraudEngine.Core.Repositories;
using FraudEngine.Models.Responses;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using FluentAssertions;

namespace FraudEngine.Tests.Integration.Routes;

public class AlertRoutesTests : IClassFixture<WebApplicationFactory<global::Program>>
{
    private readonly WebApplicationFactory<global::Program> _factory;
    private readonly Mock<IFraudAlertRepository> _alertRepoMock = new();

    public AlertRoutesTests(WebApplicationFactory<global::Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services => services.AddScoped(_ => _alertRepoMock.Object));
        });
    }

    [Fact]
    public async Task GetAlertById_WhenExists_ReturnsOkWithData()
    {
        // Arrange
        var client = _factory.CreateClient();
        var alertId = 12345L;
        var mockAlert = new FraudEngine.Core.Models.Alert { Id = alertId, TransactionId = 99, RiskScore = 85 };
        
        _alertRepoMock.Setup(x => x.GetByIdAsync(alertId)).ReturnsAsync(mockAlert);

        // Act
        var response = await client.GetAsync($"/api/fraud/alerts/{alertId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AlertListResponse>();
        result!.Data?.FirstOrDefault()?.RiskScore.Should().Be(85);
    }

    [Fact]
    public async Task GetAlertByTransactionId_WhenMissing_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        _alertRepoMock.Setup(x => x.GetByTransactionIdAsync(It.IsAny<long>()))
                      .ReturnsAsync((FraudEngine.Core.Models.Alert?)null);

        // Act
        var response = await client.GetAsync("/api/fraud/alerts/transaction/777");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ALERT_NOT_FOUND");
    }
}
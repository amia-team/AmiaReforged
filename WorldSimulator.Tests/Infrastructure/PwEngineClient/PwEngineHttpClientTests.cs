using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Contrib.HttpClient;
using NUnit.Framework;
using WorldSimulator.Infrastructure.PwEngineClient;

namespace WorldSimulator.Tests.Infrastructure.PwEngineClient;

/// <summary>
/// Unit tests for PwEngineHttpClient using minimal mocks.
/// Uses Moq.Contrib.HttpClient for clean HttpClient mocking.
/// Follows BDD-style naming: Given_When_Then
/// </summary>
[TestFixture]
public class PwEngineHttpClientTests
{
    private Mock<IHttpClientFactory> _httpClientFactory = null!;
    private Mock<IConfiguration> _configuration = null!;
    private Mock<HttpMessageHandler> _httpMessageHandler = null!;

    [SetUp]
    public void SetUp()
    {
        _httpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClientFactory = new Mock<IHttpClientFactory>();
        _configuration = new Mock<IConfiguration>();

        // Setup all required configuration values
        _configuration.Setup(c => c["PwEngine:Retry:MaxAttempts"]).Returns("3");
        _configuration.Setup(c => c["PwEngine:CircuitBreaker:FailuresBeforeOpening"]).Returns("5");
        _configuration.Setup(c => c["PwEngine:CircuitBreaker:DurationOfBreakSeconds"]).Returns("30");

        // Setup GetValue<int> calls that PwEngineHttpClient uses
        _configuration.Setup(c => c.GetSection("PwEngine:Retry:MaxAttempts").Value).Returns("3");
        _configuration.Setup(c => c.GetSection("PwEngine:CircuitBreaker:FailuresBeforeOpening").Value).Returns("5");
        _configuration.Setup(c => c.GetSection("PwEngine:CircuitBreaker:DurationOfBreakSeconds").Value).Returns("30");
    }

    [Test]
    public async Task IsHealthyAsync_WhenServerIsHealthy_ShouldReturnTrue()
    {
        // Arrange
        HttpClient httpClient = _httpMessageHandler.CreateClient();
        httpClient.BaseAddress = new Uri("http://localhost:8080/api/worldengine/");

        _httpMessageHandler
            .SetupRequest(HttpMethod.Get, "http://localhost:8080/api/worldengine/health")
            .ReturnsResponse(System.Net.HttpStatusCode.OK,
                """{"status":"healthy","service":"WorldEngine","timestamp":"2025-10-30T00:00:00Z"}""",
                "application/json");

        _httpClientFactory.Setup(f => f.CreateClient("PwEngine")).Returns(httpClient);

        PwEngineHttpClient client = new PwEngineHttpClient(
            _httpClientFactory.Object,
            _configuration.Object,
            NullLogger<PwEngineHttpClient>.Instance);

        // Act
        bool result = await client.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task IsHealthyAsync_WhenServerReturnsError_ShouldReturnFalse()
    {
        // Arrange
        HttpClient httpClient = _httpMessageHandler.CreateClient();
        httpClient.BaseAddress = new Uri("http://localhost:8080/api/worldengine/");

        _httpMessageHandler
            .SetupRequest(HttpMethod.Get, "http://localhost:8080/api/worldengine/health")
            .ReturnsResponse(System.Net.HttpStatusCode.InternalServerError);

        _httpClientFactory.Setup(f => f.CreateClient("PwEngine")).Returns(httpClient);

        PwEngineHttpClient client = new PwEngineHttpClient(
            _httpClientFactory.Object,
            _configuration.Object,
            NullLogger<PwEngineHttpClient>.Instance);

        // Act
        bool result = await client.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task IsHealthyAsync_WhenNetworkErrorOccurs_ShouldReturnFalse()
    {
        // Arrange
        HttpClient httpClient = _httpMessageHandler.CreateClient();
        httpClient.BaseAddress = new Uri("http://localhost:8080/api/worldengine/");

        _httpMessageHandler
            .SetupRequest(HttpMethod.Get, "http://localhost:8080/api/worldengine/health")
            .Throws(new HttpRequestException("Connection refused"));

        _httpClientFactory.Setup(f => f.CreateClient("PwEngine")).Returns(httpClient);

        PwEngineHttpClient client = new PwEngineHttpClient(
            _httpClientFactory.Object,
            _configuration.Object,
            NullLogger<PwEngineHttpClient>.Instance);

        // Act
        bool result = await client.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task IsHealthyAsync_WhenRequestTimesOut_ShouldReturnFalse()
    {
        // Arrange
        HttpClient httpClient = _httpMessageHandler.CreateClient();
        httpClient.BaseAddress = new Uri("http://localhost:8080/api/worldengine/");
        httpClient.Timeout = TimeSpan.FromMilliseconds(100);

        _httpMessageHandler
            .SetupRequest(HttpMethod.Get, "http://localhost:8080/api/worldengine/health")
            .Throws(new TaskCanceledException("Request timeout"));

        _httpClientFactory.Setup(f => f.CreateClient("PwEngine")).Returns(httpClient);

        PwEngineHttpClient client = new PwEngineHttpClient(
            _httpClientFactory.Object,
            _configuration.Object,
            NullLogger<PwEngineHttpClient>.Instance);

        // Act
        bool result = await client.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task IsHealthyAsync_WhenValidJsonReturned_ShouldParseCorrectly()
    {
        // Arrange
        HttpClient httpClient = _httpMessageHandler.CreateClient();
        httpClient.BaseAddress = new Uri("http://localhost:8080/api/worldengine/");

        const string expectedStatus = "healthy";
        _httpMessageHandler
            .SetupRequest(HttpMethod.Get, "http://localhost:8080/api/worldengine/health")
            .ReturnsResponse(System.Net.HttpStatusCode.OK,
                $$"""{"status":"{{expectedStatus}}","service":"WorldEngine","timestamp":"2025-10-30T00:00:00Z"}""",
                "application/json");

        _httpClientFactory.Setup(f => f.CreateClient("PwEngine")).Returns(httpClient);

        PwEngineHttpClient client = new PwEngineHttpClient(
            _httpClientFactory.Object,
            _configuration.Object,
            NullLogger<PwEngineHttpClient>.Instance);

        // Act
        bool result = await client.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();

        // Verify the request was made
        _httpMessageHandler.VerifyRequest(HttpMethod.Get,
            "http://localhost:8080/api/worldengine/health",
            Times.Once());
    }
}


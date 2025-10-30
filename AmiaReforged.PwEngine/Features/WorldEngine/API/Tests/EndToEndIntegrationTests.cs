using System.Net;
using System.Text.Json;
using FluentAssertions;
using NLog;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Tests;

/// <summary>
/// End-to-end integration specs that verify PwEngine server works with real HTTP clients.
/// NO MOCKS - real HTTP communication!
/// NO cross-project references - uses standard HttpClient only.
/// </summary>
[TestFixture]
public class EndToEndIntegrationSpecs
{
    private WorldEngineHttpServer? _server;
    private HttpClient? _httpClient;
    private Logger _logger = null!;
    private const string TestApiKey = "e2e-test-key";
    private const int TestPort = 58081;

    [SetUp]
    public void SetUp()
    {
        // Setup PwEngine server
        _logger = LogManager.GetCurrentClassLogger();
        var router = new WorldEngineApiRouter(_logger);

        _server = new WorldEngineHttpServer(router, _logger, TestApiKey, TestPort);
        _server.Start();

        // Give server time to start
        Thread.Sleep(200);

        // Setup standard HttpClient (no WorldSimulator dependencies!)
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{TestPort}/api/worldengine/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", TestApiKey);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
        _server?.Dispose();
    }

    [Test]
    public async Task EndToEnd_WhenHealthCheckCalled_ShouldSucceed()
    {
        // Act
        var response = await _httpClient!.GetAsync("health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }

    [Test]
    public async Task EndToEnd_WhenMultipleHealthChecksCalled_ShouldAllSucceed()
    {
        // Act
        var tasks = Enumerable.Range(1, 5)
            .Select(_ => _httpClient!.GetAsync("health"))
            .ToArray();
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    [Test]
    public async Task EndToEnd_WhenServerRestarted_ShouldRecoverGracefully()
    {
        // Arrange - First health check succeeds
        var firstResponse = await _httpClient!.GetAsync("health");
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Restart server
        _server!.Dispose();
        Thread.Sleep(100);

        var router = new WorldEngineApiRouter(_logger);
        _server = new WorldEngineHttpServer(router, _logger, TestApiKey, TestPort);
        _server.Start();
        Thread.Sleep(200);

        // Assert - Health check succeeds after restart
        var secondResponse = await _httpClient.GetAsync("health");
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task EndToEnd_WhenConcurrentRequests_ShouldAllSucceed()
    {
        // Act - Make 20 concurrent requests
        var tasks = Enumerable.Range(1, 20)
            .Select(_ => _httpClient!.GetAsync("health"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(20);
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    [Test]
    public async Task EndToEnd_WhenValidJsonReturned_ShouldParseCorrectly()
    {
        // Act
        var response = await _httpClient!.GetAsync("health");
        var json = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<HealthResponse>(json, options);

        // Assert
        data.Should().NotBeNull();
        data.Status.Should().Be("healthy");
        data.Service.Should().Be("WorldEngine");
    }

    [Test]
    public async Task EndToEnd_WhenApiKeyMissing_ShouldReturn401()
    {
        // Arrange - Client without API key
        using var clientWithoutKey = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{TestPort}/api/worldengine/"),
            Timeout = TimeSpan.FromSeconds(5)
        };

        // Act
        var response = await clientWithoutKey.GetAsync("health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task EndToEnd_WhenInvalidApiKey_ShouldReturn401()
    {
        // Arrange - Client with wrong API key
        using var clientWithBadKey = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{TestPort}/api/worldengine/"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        clientWithBadKey.DefaultRequestHeaders.Add("X-API-Key", "wrong-key");

        // Act
        var response = await clientWithBadKey.GetAsync("health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Test DTO - mirrors what the server returns
    private record HealthResponse(string Status, string Service, DateTime Timestamp);
}


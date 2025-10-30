using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NLog;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Tests;

/// <summary>
/// Integration specs for the HTTP server.
/// Uses real HttpListener and HttpClient - NO MOCKS!
/// BDD-style: Given_When_Then naming
/// </summary>
[TestFixture]
public class HttpServerIntegrationSpecs
{
    private WorldEngineHttpServer? _server;
    private HttpClient? _httpClient;
    private Logger _logger = null!;
    private const string TestApiKey = "test-api-key-123";
    private const int TestPort = 58080; // Use non-standard port to avoid conflicts

    [SetUp]
    public void SetUp()
    {
        _logger = LogManager.GetCurrentClassLogger();

        // Create router with test routes
        var router = new WorldEngineApiRouter(_logger);

        // Add test-specific routes
        router.AddRoute("GET", "/api/worldengine/test/ping",
            async ctx => new ApiResult(200, new { message = "pong" }),
            "TestPing");

        router.AddRoute("GET", "/api/worldengine/test/echo/{value}",
            async ctx => new ApiResult(200, new { echo = ctx.GetRouteValue("value") }),
            "TestEcho");

        router.AddRoute("POST", "/api/worldengine/test/data",
            async ctx =>
            {
                var data = await ctx.ReadJsonBodyAsync<TestData>();
                return new ApiResult(200, new { received = data?.Value });
            },
            "TestData");

        // Create and start server
        _server = new WorldEngineHttpServer(router, _logger, TestApiKey, TestPort);
        _server.Start();

        // Give server time to start
        Thread.Sleep(100);

        // Create HTTP client
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{TestPort}"),
            Timeout = TimeSpan.FromSeconds(5)
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
    public async Task SimpleGetRequest_WhenRouteExists_ShouldReturnSuccess()
    {
        // Act
        var response = await _httpClient!.GetAsync("/api/worldengine/test/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("pong");
    }

    [Test]
    public async Task ParameterizedRoute_WhenCalled_ShouldExtractParameter()
    {
        // Act
        var response = await _httpClient!.GetAsync("/api/worldengine/test/echo/hello");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<EchoResponse>();
        content.Should().NotBeNull();
        content!.Echo.Should().Be("hello");
    }

    [Test]
    public async Task PostWithJsonBody_WhenValidData_ShouldReceiveData()
    {
        // Arrange
        var testData = new TestData("test-value-123");
        var content = new StringContent(
            JsonSerializer.Serialize(testData),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _httpClient!.PostAsync("/api/worldengine/test/data", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DataResponse>();
        result.Should().NotBeNull();
        result!.Received.Should().Be("test-value-123");
    }

    [Test]
    public async Task RequestWithoutApiKey_WhenMade_ShouldReturn401()
    {
        // Arrange
        var clientWithoutKey = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{TestPort}")
        };

        // Act
        var response = await clientWithoutKey.GetAsync("/api/worldengine/test/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        clientWithoutKey.Dispose();
    }

    [Test]
    public async Task RequestWithInvalidApiKey_WhenMade_ShouldReturn401()
    {
        // Arrange
        var clientWithBadKey = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{TestPort}")
        };
        clientWithBadKey.DefaultRequestHeaders.Add("X-API-Key", "wrong-key");

        // Act
        var response = await clientWithBadKey.GetAsync("/api/worldengine/test/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        clientWithBadKey.Dispose();
    }

    [Test]
    public async Task NonExistentRoute_WhenCalled_ShouldReturn404()
    {
        // Act
        var response = await _httpClient!.GetAsync("/api/worldengine/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Not found");
    }

    [Test]
    public async Task MultipleConcurrentRequests_WhenMade_ShouldAllSucceed()
    {
        // Act - Make multiple concurrent requests
        var tasks = Enumerable.Range(1, 10)
            .Select(i => _httpClient!.GetAsync($"/api/worldengine/test/echo/{i}"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    [Test]
    public async Task ResponseContentType_WhenReturned_ShouldBeApplicationJson()
    {
        // Act
        var response = await _httpClient!.GetAsync("/api/worldengine/test/ping");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    // Test DTOs
    private record TestData(string Value);
    private record EchoResponse(string Echo);
    private record DataResponse(string Received);
}


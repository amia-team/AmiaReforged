using NLog;
using NUnit.Framework;
using System.Text.Json;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Tests;

/// <summary>
/// Tests for EchoController - verifies inter-service communication endpoints.
/// </summary>
[TestFixture]
public class EchoControllerTests
{
    private RouteTable _routeTable = null!;
    private Logger _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _routeTable = new RouteTable(_logger);
        _routeTable.ScanType(typeof(Controllers.EchoController));
    }

    [Test]
    public void ScanAssembly_WhenEchoControllerScanned_ShouldDiscoverRoutes()
    {
        // Arrange & Act
        List<(string Method, string Pattern, string Handler)> routes = _routeTable.GetRoutes().ToList();

        // Assert
        Assert.That(routes, Is.Not.Empty, "Should discover routes");
        Assert.That(routes.Count, Is.EqualTo(2), "Should discover exactly 2 routes (ping + hello)");
        Assert.That(routes.Any(r => r.Pattern.Contains("/echo/ping")), Is.True, "Should discover /echo/ping route");
        Assert.That(routes.Any(r => r.Pattern.Contains("/echo/hello")), Is.True, "Should discover /echo/hello route");
    }

    [Test]
    public async Task EchoPing_WhenCalled_ShouldReturnPong()
    {
        // Arrange & Act
        ApiResult? result = await _routeTable.DispatchAsync(
            "GET",
            "/api/worldengine/echo/ping",
            null!,
            CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result.StatusCode, Is.EqualTo(200), "Should return 200 OK");

        // Verify response structure
        string jsonResponse = JsonSerializer.Serialize(result.Data);
        using JsonDocument doc = JsonDocument.Parse(jsonResponse);

        Assert.That(doc.RootElement.TryGetProperty("pong", out JsonElement pongElement), Is.True, "Should have 'pong' property");
        Assert.That(pongElement.GetBoolean(), Is.True, "pong should be true");

        Assert.That(doc.RootElement.TryGetProperty("service", out JsonElement serviceElement), Is.True, "Should have 'service' property");
        Assert.That(serviceElement.GetString(), Is.EqualTo("PwEngine"), "service should be 'PwEngine'");

        Assert.That(doc.RootElement.TryGetProperty("receivedAt", out JsonElement _), Is.True, "Should have 'receivedAt' timestamp");
    }

    [Test]
    public async Task EchoHello_WhenCalledWithoutBody_ShouldReturn400()
    {
        // Arrange & Act
        // Following existing test pattern - pass null for RouteContext when no body
        ApiResult? result = await _routeTable.DispatchAsync(
            "POST",
            "/api/worldengine/echo/hello",
            null!,
            CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result.StatusCode, Is.EqualTo(400), "Should return 400 Bad Request when body is missing");

        // Verify error response
        string jsonResponse = JsonSerializer.Serialize(result.Data);
        using JsonDocument doc = JsonDocument.Parse(jsonResponse);

        Assert.That(doc.RootElement.TryGetProperty("error", out JsonElement errorElement), Is.True, "Should have 'error' property");
        Assert.That(errorElement.GetString(), Is.EqualTo("Bad Request"), "Should indicate bad request");

        Assert.That(doc.RootElement.TryGetProperty("message", out JsonElement _), Is.True, "Should have 'message' property");
    }

    // TODO: Add tests for EchoHello with valid body once RouteContext mock is properly implemented
    // For now, we test the route discovery and basic error handling
}


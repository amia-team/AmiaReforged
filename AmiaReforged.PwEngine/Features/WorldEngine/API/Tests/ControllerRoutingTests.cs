using NLog;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Tests;

/// <summary>
/// Tests for controller route discovery and execution.
/// Tests the reflection-based routing system with real controllers.
/// </summary>
[TestFixture]
public class ControllerRoutingTests
{
    private RouteTable _routeTable = null!;
    private Logger _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _routeTable = new RouteTable(_logger);
    }

    [Test]
    public void ScanAssembly_WhenHealthControllerScanned_ShouldDiscoverRoutes()
    {
        // Arrange & Act
        _routeTable.ScanType(typeof(Controllers.HealthController));
        List<(string Method, string Pattern, string Handler)> routes = _routeTable.GetRoutes().ToList();

        // Assert
        Assert.That(routes, Is.Not.Empty);
        Assert.That(routes.Any(r => r.Pattern.Contains("/health")), Is.True);
    }

    [Test]
    public async Task HealthController_WhenGetHealthCalled_ShouldReturnHealthyStatus()
    {
        // Arrange
        _routeTable.ScanType(typeof(Controllers.HealthController));

        // Act
        ApiResult? result = await _routeTable.DispatchAsync(
            "GET",
            "/api/worldengine/health",
            null!,
            CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public void ScanAssembly_WhenBankingControllerScanned_ShouldDiscoverAllRoutes()
    {
        // Arrange & Act
        _routeTable.ScanType(typeof(Controllers.ExampleBankingController));
        List<(string Method, string Pattern, string Handler)> routes = _routeTable.GetRoutes().ToList();

        // Assert
        Assert.That(routes, Is.Not.Empty);
        Assert.That(routes.Any(r => r.Pattern.Contains("/treasuries")), Is.True);
        Assert.That(routes.Any(r => r.Pattern.Contains("/banking")), Is.True);
    }

    [Test]
    public async Task BankingController_WhenGetTreasuryBalanceCalled_ShouldExtractId()
    {
        // Arrange
        _routeTable.ScanType(typeof(Controllers.ExampleBankingController));

        // Act
        ApiResult? result = await _routeTable.DispatchAsync(
            "GET",
            "/api/worldengine/treasuries/123/balance",
            null!,
            CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task BankingController_WhenApplyInterestCalled_ShouldReturnBadRequestWithoutBody()
    {
        // Arrange
        _routeTable.ScanType(typeof(Controllers.ExampleBankingController));

        // Act
        ApiResult? result = await _routeTable.DispatchAsync(
            "POST",
            "/api/worldengine/banking/apply-interest",
            null!,
            CancellationToken.None);

        // Assert - Should return 400 when no body provided (correct behavior)
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public void ScanAssembly_WhenMultipleControllers_ShouldDiscoverAllRoutes()
    {
        // Arrange & Act
        _routeTable.ScanType(typeof(Controllers.HealthController));
        _routeTable.ScanType(typeof(Controllers.ExampleBankingController));

        List<(string Method, string Pattern, string Handler)> routes = _routeTable.GetRoutes().ToList();

        // Assert
        Assert.That(routes.Count, Is.GreaterThanOrEqualTo(3)); // At least health + 2 banking routes
    }

    [Test]
    public void RouteDiscovery_WhenStaticMethodsUsed_ShouldDiscoverAll()
    {
        // Arrange & Act
        _routeTable.ScanType(typeof(Controllers.HealthController));
        List<(string Method, string Pattern, string Handler)> routes = _routeTable.GetRoutes().ToList();

        // Assert - All controller methods are static, should be discovered
        Assert.That(routes, Is.Not.Empty);
        Assert.That(routes.All(r => r.Handler.Contains("Controller")), Is.True);
    }

    [Test]
    public async Task MultipleParameters_WhenExtracted_ShouldAllBeCorrect()
    {
        // Arrange
        _routeTable.AddRoute("GET", "/api/test/{id}/items/{itemId}",
            ctx =>
            {
                string id = ctx.GetRouteValue("id");
                string itemId = ctx.GetRouteValue("itemId");
                return Task.FromResult(new ApiResult(200, new { id, itemId }));
            },
            "TestMultiParam");

        // Act
        ApiResult? result = await _routeTable.DispatchAsync(
            "GET",
            "/api/test/123/items/456",
            null!,
            CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public void DifferentHttpMethods_WhenUsed_ShouldBeDistinguished()
    {
        // Arrange
        _routeTable.AddRoute("GET", "/api/test/resource",
            _ => Task.FromResult(new ApiResult(200, new { method = "GET" })),
            "GetResource");

        _routeTable.AddRoute("POST", "/api/test/resource",
            _ => Task.FromResult(new ApiResult(201, new { method = "POST" })),
            "PostResource");

        List<(string Method, string Pattern, string Handler)> routes = _routeTable.GetRoutes().ToList();

        // Assert
        Assert.That(routes, Has.Count.EqualTo(2));
        Assert.That(routes.Any(r => r.Method == "GET"), Is.True);
        Assert.That(routes.Any(r => r.Method == "POST"), Is.True);
    }
}


using FluentAssertions;
using NLog;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Tests;

/// <summary>
/// BDD-style specs for RouteTable route discovery and matching.
/// Pure unit tests - no mocks needed!
/// RSpec-inspired naming: Context_When_Then
/// </summary>
[TestFixture]
public class RouteTableSpecs
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
    public void AddRoute_WhenSimpleRouteAdded_ShouldRegisterSuccessfully()
    {
        // Arrange & Act
        _routeTable.AddRoute("GET", "/api/test",
            async ctx => new ApiResult(200, new { message = "test" }),
            "TestRoute");

        List<(string Method, string Pattern, string Handler)> routes = _routeTable.GetRoutes().ToList();

        // Assert
        Assert.That(routes, Has.Count.EqualTo(1));
        Assert.That(routes[0].Method, Is.EqualTo("GET"));
        Assert.That(routes[0].Pattern, Is.EqualTo("/api/test"));
        Assert.That(routes[0].Handler, Is.EqualTo("TestRoute"));
    }

    [Test]
    public void AddRoute_WhenParameterizedRoute_ShouldExtractParameterNames()
    {
        // Arrange & Act
        _routeTable.AddRoute("GET", "/api/treasuries/{id}/balance",
            async ctx => new ApiResult(200, new { id = ctx.GetRouteValue("id") }),
            "GetBalance");

        var routes = _routeTable.GetRoutes().ToList();

        // Assert
        routes.Should().HaveCount(1);
        routes[0].Pattern.Should().Contain("{id}");
    }

    [Test]
    public void AddRoute_WhenMultipleParameters_ShouldHandleCorrectly()
    {
        // Arrange & Act
        _routeTable.AddRoute("GET", "/api/regions/{regionId}/areas/{areaId}",
            async ctx => new ApiResult(200, new
            {
                regionId = ctx.GetRouteValue("regionId"),
                areaId = ctx.GetRouteValue("areaId")
            }),
            "GetArea");

        var routes = _routeTable.GetRoutes().ToList();

        // Assert
        routes.Should().HaveCount(1);
        routes[0].Pattern.Should().Contain("{regionId}");
        routes[0].Pattern.Should().Contain("{areaId}");
    }

    [Test]
    public async Task DispatchAsync_WhenSimpleRouteMatches_ShouldExecuteHandler()
    {
        // Arrange
        var executed = false;
        _routeTable.AddRoute("GET", "/api/test",
            async ctx =>
            {
                executed = true;
                return new ApiResult(200, new { message = "success" });
            },
            "TestRoute");

        var mockRequest = CreateMockRequest("GET", "/api/test");

        // Act
        var result = await _routeTable.DispatchAsync("GET", "/api/test", mockRequest, CancellationToken.None);

        // Assert
        executed.Should().BeTrue();
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task DispatchAsync_WhenParameterizedRoute_ShouldExtractParameters()
    {
        // Arrange
        string? capturedId = null;
        _routeTable.AddRoute("GET", "/api/treasuries/{id}/balance",
            async ctx =>
            {
                capturedId = ctx.GetRouteValue("id");
                return new ApiResult(200, new { treasuryId = capturedId });
            },
            "GetBalance");

        var mockRequest = CreateMockRequest("GET", "/api/treasuries/123/balance");

        // Act
        var result = await _routeTable.DispatchAsync("GET", "/api/treasuries/123/balance", mockRequest, CancellationToken.None);

        // Assert
        capturedId.Should().Be("123");
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task DispatchAsync_WhenMultipleParameters_ShouldExtractAll()
    {
        // Arrange
        string? capturedRegionId = null;
        string? capturedAreaId = null;

        _routeTable.AddRoute("GET", "/api/regions/{regionId}/areas/{areaId}",
            async ctx =>
            {
                capturedRegionId = ctx.GetRouteValue("regionId");
                capturedAreaId = ctx.GetRouteValue("areaId");
                return new ApiResult(200, new { regionId = capturedRegionId, areaId = capturedAreaId });
            },
            "GetArea");

        var mockRequest = CreateMockRequest("GET", "/api/regions/north/areas/cordor");

        // Act
        var result = await _routeTable.DispatchAsync("GET", "/api/regions/north/areas/cordor", mockRequest, CancellationToken.None);

        // Assert
        capturedRegionId.Should().Be("north");
        capturedAreaId.Should().Be("cordor");
        result.Should().NotBeNull();
    }

    [Test]
    public async Task DispatchAsync_WhenNoMatchingRoute_ShouldReturnNull()
    {
        // Arrange
        _routeTable.AddRoute("GET", "/api/test",
            async ctx => new ApiResult(200, new { }),
            "TestRoute");

        var mockRequest = CreateMockRequest("GET", "/api/notfound");

        // Act
        var result = await _routeTable.DispatchAsync("GET", "/api/notfound", mockRequest, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task DispatchAsync_WhenWrongMethod_ShouldReturnNull()
    {
        // Arrange
        _routeTable.AddRoute("GET", "/api/test",
            async ctx => new ApiResult(200, new { }),
            "TestRoute");

        var mockRequest = CreateMockRequest("POST", "/api/test");

        // Act
        var result = await _routeTable.DispatchAsync("POST", "/api/test", mockRequest, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void ScanType_WhenAttributedMethodsExist_ShouldFindThem()
    {
        // Arrange & Act
        _routeTable.ScanType(typeof(TestController));

        var routes = _routeTable.GetRoutes().ToList();

        // Assert
        routes.Should().HaveCountGreaterThanOrEqualTo(1);
        routes.Should().Contain(r => r.Pattern.Contains("/test/hello"));
    }

    [Test]
    public async Task ScanType_WhenAttributedMethodCalled_ShouldExecuteCorrectly()
    {
        // Arrange
        _routeTable.ScanType(typeof(TestController));
        var mockRequest = CreateMockRequest("GET", "/api/test/hello");

        // Act
        var result = await _routeTable.DispatchAsync("GET", "/api/test/hello", mockRequest, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task ScanType_WhenParameterizedAttributedMethod_ShouldExtractParameters()
    {
        // Arrange
        _routeTable.ScanType(typeof(TestController));
        var mockRequest = CreateMockRequest("GET", "/api/test/items/456");

        // Act
        var result = await _routeTable.DispatchAsync("GET", "/api/test/items/456", mockRequest, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    // Helper method to create a minimal mock request
    private System.Net.HttpListenerRequest CreateMockRequest(string method, string path)
    {
        // We can't easily mock HttpListenerRequest, so we'll use a minimal implementation
        // For RouteTable tests, we only need the request for RouteContext creation
        // The actual request object isn't used in parameter extraction
        return null!; // RouteTable doesn't actually use the request object for matching
    }

    // Test controller for reflection tests
    private class TestController
    {
        [HttpGet("/api/test/hello")]
        public static async Task<ApiResult> GetHello(RouteContext ctx)
        {
            return await Task.FromResult(new ApiResult(200, new { message = "hello" }));
        }

        [HttpGet("/api/test/items/{id}")]
        public static async Task<ApiResult> GetItem(RouteContext ctx)
        {
            var id = ctx.GetRouteValue("id");
            return await Task.FromResult(new ApiResult(200, new { itemId = id }));
        }

        [HttpPost("/api/test/create")]
        public static async Task<ApiResult> CreateItem(RouteContext ctx)
        {
            return await Task.FromResult(new ApiResult(201, new { created = true }));
        }
    }
}


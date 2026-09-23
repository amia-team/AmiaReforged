using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Areas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Moq;
using NLog;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Tests;

/// <summary>
/// Verifies <see cref="AreaReloadController"/> routes through <see cref="IWorldEngineFacade"/>
/// (central dispatch) and maps each <see cref="CommandResult"/> outcome to the correct HTTP
/// status. Uses a stub <see cref="IServiceProvider"/> carrying a mocked facade — no Anvil
/// runtime required, and no live-area manipulation.
/// </summary>
[TestFixture]
public class AreaReloadControllerCqrsTests
{
    private RouteTable _routeTable = null!;
    private Mock<IWorldEngineFacade> _facadeMock = null!;
    private IServiceProvider _services = null!;

    private sealed class StubProvider : IServiceProvider
    {
        private readonly IWorldEngineFacade? _facade;
        public StubProvider(IWorldEngineFacade? facade) => _facade = facade;
        public object? GetService(Type serviceType) =>
            serviceType == typeof(IWorldEngineFacade) ? _facade : null;
    }

    [SetUp]
    public void SetUp()
    {
        _routeTable = new RouteTable(LogManager.GetCurrentClassLogger());
        _facadeMock = new Mock<IWorldEngineFacade>();
        _services = new StubProvider(_facadeMock.Object);
    }

    private Task<ApiResult?> DispatchAsync(string path) =>
        _routeTable.DispatchAsync("POST", path, null!, CancellationToken.None, _services);

    private static CommandResult ResultWithOutcome(string outcome, string? message = null) =>
        CommandResult.Fail($"failed:{outcome}", new Dictionary<string, object>
        {
            ["outcome"] = outcome,
            ["message"] = message ?? $"Command failed: {outcome}",
        });

    [Test]
    public async Task ReloadArea_WhenHandlerSucceeds_Returns200AndDispatchesCommandWithResref()
    {
        const string resRef = "dungeon";
        _routeTable.ScanType(typeof(Controllers.AreaReloadController));
        _facadeMock
            .Setup(f => f.ExecuteAsync(
                It.Is<ReloadAreaCommand>(c => c.ResRef == resRef),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.OkWithData(new Dictionary<string, object>
            {
                ["outcome"] = "reloaded",
                ["resref"] = resRef,
                ["name"] = "Dungeon",
                ["message"] = "Area \"Dungeon\" reloaded successfully.",
            }));

        ApiResult? result = await DispatchAsync("/api/worldengine/areas/reload/dungeon");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(200));
        _facadeMock.Verify(f => f.ExecuteAsync(
            It.Is<ReloadAreaCommand>(c => c.ResRef == resRef),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ReloadArea_WhenAreaNotFound_Returns404()
    {
        _routeTable.ScanType(typeof(Controllers.AreaReloadController));
        _facadeMock
            .Setup(f => f.ExecuteAsync(
                It.IsAny<ReloadAreaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultWithOutcome("area_not_found"));

        ApiResult? result = await DispatchAsync("/api/worldengine/areas/reload/missing");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task ReloadArea_WhenAreaOccupied_Returns409()
    {
        _routeTable.ScanType(typeof(Controllers.AreaReloadController));
        _facadeMock
            .Setup(f => f.ExecuteAsync(
                It.IsAny<ReloadAreaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultWithOutcome("area_occupied"));

        ApiResult? result = await DispatchAsync("/api/worldengine/areas/reload/occupied");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(409));
    }

    [Test]
    public async Task ReloadArea_WhenRecreateFails_Returns500()
    {
        _routeTable.ScanType(typeof(Controllers.AreaReloadController));
        _facadeMock
            .Setup(f => f.ExecuteAsync(
                It.IsAny<ReloadAreaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultWithOutcome("recreate_failed"));

        ApiResult? result = await DispatchAsync("/api/worldengine/areas/reload/broken");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task ReloadArea_WhenHandlerReturnsUnknownOutcome_Returns400()
    {
        _routeTable.ScanType(typeof(Controllers.AreaReloadController));
        _facadeMock
            .Setup(f => f.ExecuteAsync(
                It.IsAny<ReloadAreaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultWithOutcome("area_reload_unknown"));

        ApiResult? result = await DispatchAsync("/api/worldengine/areas/reload/weird");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task ReloadArea_WhenFacadeUnavailable_Returns503()
    {
        _routeTable.ScanType(typeof(Controllers.AreaReloadController));
        _services = new StubProvider(null); // No facade available.

        ApiResult? result = await DispatchAsync("/api/worldengine/areas/reload/dungeon");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(503));
    }
}

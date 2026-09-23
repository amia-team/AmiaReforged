using AmiaReforged.PwEngine.Features.WorldEngine.Application.Areas;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Areas.Tests;

/// <summary>
/// Test double for <see cref="IAreaReloadRuntime"/> that returns a pre-configured
/// outcome without touching any NWN runtime. The same fake is reused by the
/// handler tests and the dispatcher integration tests.
/// </summary>
internal sealed class FakeAreaReloadRuntime : IAreaReloadRuntime
{
    private readonly AreaReloadStatus _status;
    private readonly string _name;

    public FakeAreaReloadRuntime(AreaReloadStatus status, string name = "TestArea")
    {
        _status = status;
        _name = name;
    }

    /// <summary>True once <see cref="ReloadAreaAsync"/> has been invoked.</summary>
    public bool Called { get; private set; }

    /// <summary>The resref passed to the most recent call.</summary>
    public string? LastResRef { get; private set; }

    public Task<AreaReloadResult> ReloadAreaAsync(string resRef)
    {
        Called = true;
        LastResRef = resRef;

        AreaReloadResult result = _status switch
        {
            AreaReloadStatus.NotFound => AreaReloadResult.NotFound(resRef),
            AreaReloadStatus.Occupied => AreaReloadResult.Occupied(resRef, _name, 2),
            AreaReloadStatus.Reloaded => AreaReloadResult.Reloaded(resRef, _name),
            AreaReloadStatus.RecreateFailed => AreaReloadResult.RecreateFailed(resRef, _name),
            _ => throw new ArgumentOutOfRangeException(nameof(_status), _status, "Unknown reload status"),
        };

        return Task.FromResult(result);
    }
}

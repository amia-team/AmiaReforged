using AmiaReforged.PwEngine.Features.WorldEngine.Application.Areas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Areas.Tests;

/// <summary>
/// Verifies <see cref="ReloadAreaCommandHandler"/> maps each runtime outcome to the
/// correct <see cref="CommandResult"/>. No NWN runtime is required — the runtime is
/// replaced by <see cref="FakeAreaReloadRuntime"/>.
/// </summary>
[TestFixture]
public class AreaReloadCommandHandlerTests
{
    private static ReloadAreaCommandHandler CreateHandler(FakeAreaReloadRuntime runtime) =>
        new(runtime);

    [Test]
    public async Task MissingArea_WhenDispatched_ReturnsFailedCommandWithAreaNotFound()
    {
        FakeAreaReloadRuntime runtime = new(AreaReloadStatus.NotFound);
        ReloadAreaCommandHandler handler = CreateHandler(runtime);

        CommandResult result = await handler.HandleAsync(new ReloadAreaCommand { ResRef = "my_area" });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("area_not_found"));
        Assert.That(runtime.Called, Is.True);
    }

    [Test]
    public async Task OccupiedArea_WhenDispatched_ReturnsFailedCommandWithAreaOccupied()
    {
        FakeAreaReloadRuntime runtime = new(AreaReloadStatus.Occupied);
        ReloadAreaCommandHandler handler = CreateHandler(runtime);

        CommandResult result = await handler.HandleAsync(new ReloadAreaCommand { ResRef = "my_area" });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("area_occupied"));
    }

    [Test]
    public async Task ReloadSuccess_WhenDispatched_ReturnsSuccessfulCommandWithData()
    {
        FakeAreaReloadRuntime runtime = new(AreaReloadStatus.Reloaded, "Dungeon");
        ReloadAreaCommandHandler handler = CreateHandler(runtime);

        CommandResult result = await handler.HandleAsync(new ReloadAreaCommand { ResRef = "dungeon" });

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!["outcome"], Is.EqualTo("reloaded"));
        Assert.That(result.Data!["resref"], Is.EqualTo("dungeon"));
        Assert.That(result.Data!["name"], Is.EqualTo("Dungeon"));
    }

    [Test]
    public async Task RecreateFailure_WhenDispatched_ReturnsFailedCommandWithRecreateFailed()
    {
        FakeAreaReloadRuntime runtime = new(AreaReloadStatus.RecreateFailed);
        ReloadAreaCommandHandler handler = CreateHandler(runtime);

        CommandResult result = await handler.HandleAsync(new ReloadAreaCommand { ResRef = "bad_area" });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("recreate_failed"));
    }

    [Test]
    public async Task EmptyResRef_WhenDispatched_RejectsWithoutCallingRuntime()
    {
        FakeAreaReloadRuntime runtime = new(AreaReloadStatus.Reloaded);
        ReloadAreaCommandHandler handler = CreateHandler(runtime);

        CommandResult result = await handler.HandleAsync(new ReloadAreaCommand { ResRef = "   " });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("area_resref_empty"));
        Assert.That(runtime.Called, Is.False);
    }
}

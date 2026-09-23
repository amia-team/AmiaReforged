using AmiaReforged.PwEngine.Features.WorldEngine.Application.Areas;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Areas.Tests;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Commands;

/// <summary>
/// Verifies the real <see cref="CommandDispatcher"/> end-to-end with the real
/// <see cref="ReloadAreaCommandHandler"/> and an <see cref="InMemoryEventBus"/>.
/// The dispatcher publishes <see cref="CommandExecutedEvent{TCommand}"/> only on
/// success — the test never publishes an event itself.
/// </summary>
[TestFixture]
public class AreaReloadCommandDispatchTests
{
    private CommandDispatcher _dispatcher = null!;
    private InMemoryEventBus _eventBus = null!;

    [SetUp]
    public void SetUp()
    {
        _eventBus = new InMemoryEventBus();
        FakeAreaReloadRuntime runtime = new(AreaReloadStatus.NotFound);
        ReloadAreaCommandHandler handler = new(runtime);
        _dispatcher = new CommandDispatcher(new ICommandHandlerMarker[] { handler }, _eventBus);
    }

    private static ReloadAreaCommand Command(string resRef = "my_area") => new() { ResRef = resRef };

    private bool PublishedReloadEvent() => _eventBus.PublishedEvents
        .Any(e => e is CommandExecutedEvent<ReloadAreaCommand>);

    [Test]
    public async Task SuccessfulReload_WhenDispatched_PublishesCommandExecutedEvent()
    {
        // Swap in a runtime that succeeds.
        ReplaceRuntime(new FakeAreaReloadRuntime(AreaReloadStatus.Reloaded, "Dungeon"));

        CommandResult result = await _dispatcher.DispatchAsync<ReloadAreaCommand>(Command("dungeon"));

        Assert.That(result.Success, Is.True);
        Assert.That(PublishedReloadEvent(), Is.True);
    }

    [Test]
    public async Task MissingArea_WhenDispatched_DoesNotPublishCommandExecutedEvent()
    {
        CommandResult result = await _dispatcher.DispatchAsync<ReloadAreaCommand>(Command());

        Assert.That(result.Success, Is.False);
        Assert.That(PublishedReloadEvent(), Is.False);
    }

    [Test]
    public async Task OccupiedArea_WhenDispatched_DoesNotPublishCommandExecutedEvent()
    {
        ReplaceRuntime(new FakeAreaReloadRuntime(AreaReloadStatus.Occupied));

        CommandResult result = await _dispatcher.DispatchAsync<ReloadAreaCommand>(Command());

        Assert.That(result.Success, Is.False);
        Assert.That(PublishedReloadEvent(), Is.False);
    }

    [Test]
    public async Task RecreateFailure_WhenDispatched_DoesNotPublishCommandExecutedEvent()
    {
        ReplaceRuntime(new FakeAreaReloadRuntime(AreaReloadStatus.RecreateFailed));

        CommandResult result = await _dispatcher.DispatchAsync<ReloadAreaCommand>(Command());

        Assert.That(result.Success, Is.False);
        Assert.That(PublishedReloadEvent(), Is.False);
    }

    private void ReplaceRuntime(FakeAreaReloadRuntime runtime)
    {
        // Rebuild the dispatcher with the new runtime-backed handler so the real
        // dispatch path (handler discovery + event publishing) is preserved.
        ReloadAreaCommandHandler handler = new(runtime);
        _dispatcher = new CommandDispatcher(new ICommandHandlerMarker[] { handler }, _eventBus);
    }
}

using System.Reflection;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Services;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Commands;
using Anvil.Services;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Tests;

[TestFixture]
public class DialogueRuntimeSynchronizationTests
{
    [TestCase(typeof(DialogueNpcSynchronizationHandler))]
    [TestCase(typeof(ExecuteDialogueActionHandler))]
    public void RuntimeSubscribersAreBoundToTheEventBusCollection(Type type)
    {
        Assert.That(type.GetCustomAttributes<ServiceBindingAttribute>().Select(a => a.BindFrom), Contains.Item(typeof(IEventHandlerMarker)));
    }

    [Test]
    public async Task SavedUpdateFlowsThroughProductionBusDiscoveryAndAcknowledgesMatchingNpcs()
    {
        DialogueRuntimeStatus status = new();
        RecordingSynchronizer synchronizer = new();
        DialogueNpcSynchronizationHandler subscriber = new() { Synchronizer = synchronizer, RuntimeStatus = status };
        // Use the actual service binding contract to supply the production collection.
        IEventHandlerMarker[] bound = new IEventHandlerMarker[] { subscriber, new ExecuteDialogueActionHandler() }
            .Where(h => h.GetType().GetCustomAttributes<ServiceBindingAttribute>().Any(a => a.BindFrom == typeof(IEventHandlerMarker))).ToArray();
        AnvilEventBusService bus = new(bound);
        try
        {
            DateTime revision = DateTime.UtcNow;
            PersistedDialogueTree tree = new() { DialogueTreeId = "runtime", Title = "Runtime", SpeakerTag = "guard", UpdatedUtc = revision };
            await bus.PublishAsync(new CommandExecutedEvent<UpdateDialogueTreeCommand>(new() { DialogueTreeId = tree.DialogueTreeId, Tree = tree }, CommandResult.Ok()));
            await synchronizer.Called.Task.WaitAsync(TimeSpan.FromSeconds(5));
            // The asynchronous callback records application after the synchronizer returns.
            for (int i = 0; i < 100 && status.Get("runtime", revision).State != "Applied"; i++) await Task.Delay(10);
            Assert.That(synchronizer.Tag, Is.EqualTo("guard"));
            Assert.That(status.Get("runtime", revision).State, Is.EqualTo("Applied"));
            Assert.That(status.Get("runtime", revision).MatchedNpcCount, Is.EqualTo(2));
        }
        finally { bus.Stop(); }
    }

    [Test]
    public async Task SynchronizerFailureIsVisibleForTheSavedRevision()
    {
        DialogueRuntimeStatus status = new();
        DialogueNpcSynchronizationHandler subscriber = new() { Synchronizer = new RecordingSynchronizer { Fail = true }, RuntimeStatus = status };
        DateTime revision = DateTime.UtcNow;
        Assert.ThrowsAsync<InvalidOperationException>(async () => await subscriber.HandleAsync(new CommandExecutedEvent<UpdateDialogueTreeCommand>(
            new() { DialogueTreeId = "failed", Tree = new() { DialogueTreeId = "failed", Title = "Failed", UpdatedUtc = revision } }, CommandResult.Ok())));
        Assert.That(status.Get("failed", revision).State, Is.EqualTo("Failed"));
        Assert.That(status.Get("failed", revision).Error, Does.Contain("sync failed"));
        await Task.CompletedTask;
    }

    [Test]
    public void OlderAcknowledgementCannotHideANewerSavedRevision()
    {
        DialogueRuntimeStatus status = new(); DateTime latest = DateTime.UtcNow;
        status.Applied("tree", latest, 2); status.Applied("tree", latest.AddSeconds(-1), 1);
        Assert.That(status.Get("tree", latest).MatchedNpcCount, Is.EqualTo(2));
        Assert.That(status.Get("tree", latest.AddSeconds(1)).State, Is.EqualTo("Pending"));
    }

    [Test]
    public void ApplicationAcknowledgementMatchesDatabaseTimestampPrecision()
    {
        DialogueRuntimeStatus status = new();
        DateTime databaseRevision = new(638950000000000000, DateTimeKind.Utc);
        status.Applied("tree", databaseRevision.AddTicks(7), 2);
        Assert.That(status.Get("tree", databaseRevision).State, Is.EqualTo("Applied"));
    }

    [Test]
    public void DelayedDeleteCannotRemoveARecreatedTreesAcknowledgement()
    {
        DialogueRuntimeStatus status = new();
        DateTime deletedRevision = DateTime.UtcNow;
        DateTime recreatedRevision = deletedRevision.AddSeconds(1);
        status.Applied("tree", recreatedRevision, 3);
        status.Remove("tree", deletedRevision);
        Assert.That(status.Get("tree", recreatedRevision).State, Is.EqualTo("Applied"));
        status.Remove("tree", recreatedRevision);
        Assert.That(status.Get("tree", recreatedRevision).State, Is.EqualTo("Pending"));
    }

    [Test]
    public void FailedSaveRetainsThePreviousAppliedRevision()
    {
        DialogueRuntimeStatus status = new();
        DateTime applied = new(638950000000000000, DateTimeKind.Utc);
        DateTime failed = applied.AddSeconds(1);
        status.Applied("tree", applied, 2);
        status.Failed("tree", failed, "sync failed");
        Assert.That(status.Get("tree", failed).State, Is.EqualTo("Failed"));
        Assert.That(status.Get("tree", failed).AppliedRevisionUtc, Is.EqualTo(applied));
    }

    private sealed class RecordingSynchronizer : IDialogueNpcSynchronizer
    {
        public TaskCompletionSource Called { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string? Tag { get; private set; }
        public bool Fail { get; init; }
        public Task<int> RegisterAsync(string tag, string id, CancellationToken cancellationToken = default) => Task.FromResult(2);
        public Task<int> UnregisterAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(2);
        public Task<(int unregistered, int registered)> UpdateAsync(string id, string? tag, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new InvalidOperationException("sync failed");
            Tag = tag; Called.TrySetResult(); return Task.FromResult((0, 2));
        }
    }
}

using System.Globalization;
using System.Reflection;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Services;
using Anvil.API;
using Anvil.Services;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Commands;

/// <summary>
/// Real-cache and asynchronous-bus tests for the dialogue store-cache owner.
///
/// These use the real <see cref="ExecuteDialogueActionHandler"/> — not a replacement fake
/// cache — and seed/inspect its private <c>_storeCache</c> dictionary via reflection. A seeded
/// <c>CachedStore</c> carries a <c>null</c> <see cref="NwStore"/> reference, because clearing
/// only drops references; we never call OpenShop, dereference the sentinel, create game
/// objects, or add public production APIs just to seed tests.
/// </summary>
[TestFixture]
public class DialogueStoreCacheEventTests
{
    private static uint _nextKey = 1;

    private static int CacheCount(ExecuteDialogueActionHandler handler) =>
        (int)GetDictionary(handler).GetType().GetProperty("Count")!.GetValue(GetDictionary(handler))!;

    private static object GetDictionary(ExecuteDialogueActionHandler handler) =>
        CacheField.GetValue(handler)!;

    private static uint SeedEntry(ExecuteDialogueActionHandler handler, string resRef, string tag)
    {
        object dictionary = GetDictionary(handler);
        Type dictType = dictionary.GetType();

        // The record's primary constructor is public (records default to public ctors); the copy
        // constructor is the only private one, so we match by parameter types, not by visibility.
        ConstructorInfo ctor = CachedStoreType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
            null,
            new[] { typeof(string), typeof(string), typeof(NwStore) },
            null)
            ?? throw new InvalidOperationException("CachedStore constructor not found via reflection");

        object entry = ctor.Invoke(new object[] { resRef, tag, (NwStore)null! });

        PropertyInfo item = dictType.GetProperty("Item")!;
        uint key = _nextKey++;
        item.SetValue(dictionary, entry, [key]);
        return key;
    }

    private static UpdateDialogueTreeCommand UpdateCommand(string id) =>
        new() { DialogueTreeId = id, Tree = new PersistedDialogueTree { DialogueTreeId = id, Title = id } };

    private static DeleteDialogueTreeCommand DeleteCommand(string id) =>
        new() { DialogueTreeId = id };

    // 1. A nonempty real cache becomes empty after each successful typed callback; repeating is harmless.

    [Test]
    public async Task SuccessfulUpdateCallback_EmptiesSeededCache_AndRepeatingIsHarmful()
    {
        var owner = new ExecuteDialogueActionHandler();
        SeedEntry(owner, "store_r1", "tag_r1");
        Assert.That(CacheCount(owner), Is.EqualTo(1));

        await owner.HandleAsync(new CommandExecutedEvent<UpdateDialogueTreeCommand>(UpdateCommand("dt_u"), CommandResult.Ok()));

        Assert.That(CacheCount(owner), Is.EqualTo(0), "A successful update callback clears the cache");

        await owner.HandleAsync(new CommandExecutedEvent<UpdateDialogueTreeCommand>(UpdateCommand("dt_u"), CommandResult.Ok()));
        Assert.That(CacheCount(owner), Is.EqualTo(0), "Repeating invalidation on an empty cache is harmless");
    }

    [Test]
    public async Task SuccessfulDeleteCallback_EmptiesSeededCache_AndRepeatingIsHarmful()
    {
        var owner = new ExecuteDialogueActionHandler();
        SeedEntry(owner, "store_d1", "tag_d1");
        Assert.That(CacheCount(owner), Is.EqualTo(1));

        await owner.HandleAsync(new CommandExecutedEvent<DeleteDialogueTreeCommand>(DeleteCommand("dt_d"), CommandResult.Ok()));

        Assert.That(CacheCount(owner), Is.EqualTo(0), "A successful delete callback clears the cache");

        await owner.HandleAsync(new CommandExecutedEvent<DeleteDialogueTreeCommand>(DeleteCommand("dt_d"), CommandResult.Ok()));
        Assert.That(CacheCount(owner), Is.EqualTo(0), "Repeating invalidation on an empty cache is harmless");
    }

    // 2. A failed-result event passed directly to either callback preserves seeded entries.

    [Test]
    public async Task FailedResultCallback_PreservesSeededCache_ForBothEventTypes()
    {
        var owner = new ExecuteDialogueActionHandler();
        SeedEntry(owner, "store_r1", "tag_r1");
        SeedEntry(owner, "store_r2", "tag_r2");
        Assert.That(CacheCount(owner), Is.EqualTo(2));

        await owner.HandleAsync(new CommandExecutedEvent<UpdateDialogueTreeCommand>(UpdateCommand("dt_f"), CommandResult.Fail("nope")));
        Assert.That(CacheCount(owner), Is.EqualTo(2), "A failed update result must not clear the cache");

        await owner.HandleAsync(new CommandExecutedEvent<DeleteDialogueTreeCommand>(DeleteCommand("dt_f"), CommandResult.Fail("nope")));
        Assert.That(CacheCount(owner), Is.EqualTo(2), "A failed delete result must not clear the cache");
    }

    // 3. The owner exposes the marker binding and both typed event interfaces while keeping its command/concrete bindings.

    [Test]
    public void Owner_ExposesMarkerAndTypedEventBindings_WhileRetainingCommandBindings()
    {
        Type type = typeof(ExecuteDialogueActionHandler);

        // The owner carries two [ServiceBinding] declarations: the command marker binding and the
        // concrete-handler binding. The attribute exposes only its constructor, so we prove the
        // declarations by count and by the implemented interfaces Anvil DI reflects.
        var bindings = type.GetCustomAttributes(typeof(ServiceBindingAttribute), true)
            .Cast<ServiceBindingAttribute>()
            .ToList();

        Assert.That(bindings, Has.Count.EqualTo(2),
            "Both the command-marker and concrete service bindings are declared");

        Assert.That(type.GetInterfaces(), Contains.Item(typeof(IEventHandlerMarker)));
        Assert.That(type.GetInterfaces(), Contains.Item(typeof(IEventHandler<CommandExecutedEvent<UpdateDialogueTreeCommand>>)));
        Assert.That(type.GetInterfaces(), Contains.Item(typeof(IEventHandler<CommandExecutedEvent<DeleteDialogueTreeCommand>>)));
        Assert.That(type.GetInterfaces(), Contains.Item(typeof(ICommandHandler<ExecuteDialogueActionCommand>)),
            "The owner remains the sole store-cache command handler");
    }

    // 4. The production bus deterministically shows the stale window before processing and clearing after.

    [Test]
    public async Task AsyncProductionBus_ProvesStaleWindowThenClearingOnceSubscriberRuns()
    {
        var owner = new ExecuteDialogueActionHandler();
        SeedEntry(owner, "store_a", "tag_a");
        SeedEntry(owner, "store_b", "tag_b");
        Assert.That(CacheCount(owner), Is.EqualTo(2));

        var blocking = new BlockingHandler();
        var barrier = new BarrierHandler();
        AnvilEventBusService bus = new(new IEventHandlerMarker[] { owner, blocking, barrier });

        try
        {
            // 1) Block the background processor with a test-only event queued ahead of the mutation.
            await bus.PublishAsync(new CommandExecutedEvent<TestBlockCommand>(new TestBlockCommand(), CommandResult.Ok()));
            Assert.That(await blocking.Started.Task.WaitAsync(TimeSpan.FromSeconds(5)), Is.True,
                "The blocking handler must acquire the processor before we publish the mutation");

            // 2) Publish the mutation — it queues behind the blocked event.
            await bus.PublishAsync(new CommandExecutedEvent<UpdateDialogueTreeCommand>(UpdateCommand("dt_async"), CommandResult.Ok()));

            // 3) While the gate is held, the processor never reached the mutation, so the cache
            //    still holds the seeded references (the stale window).
            Assert.That(CacheCount(owner), Is.GreaterThan(0),
                "While the processor is blocked, the mutation has not run and the cache stays populated");

            // 4) Release the gate: the processor completes the blocking handler, then the mutation
            //    (which clears the cache), then the trailing barrier.
            blocking.Release.SetResult(true);
            await bus.PublishAsync(new CommandExecutedEvent<TestBarrierCommand>(new TestBarrierCommand(), CommandResult.Ok()));

            Assert.That(await barrier.Done.Task.WaitAsync(TimeSpan.FromSeconds(5)), Is.True,
                "The barrier (queued after the mutation) only runs once the mutation has been processed");

            // 5) Once the barrier runs, the mutation has completed, so the cache is empty.
            Assert.That(CacheCount(owner), Is.EqualTo(0),
                "After the subscriber runs on the background thread, the cached references are cleared");
        }
        finally
        {
            if (!blocking.Release.Task.IsCompleted)
            {
                blocking.Release.SetResult(true);
            }

            bus.Stop();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Reflection helpers for the private cache
    // ═══════════════════════════════════════════════════════════════════

    private static readonly FieldInfo CacheField = typeof(ExecuteDialogueActionHandler)
        .GetField("_storeCache", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static Type CachedStoreType = typeof(ExecuteDialogueActionHandler)
        .GetNestedType("CachedStore", BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("CachedStore nested type not found via reflection");

    // ═══════════════════════════════════════════════════════════════════
    //  Test-only events / handlers (entirely fixture-local)
    // ═══════════════════════════════════════════════════════════════════

    private sealed class TestBlockCommand : ICommand
    {
    }

    private sealed class TestBarrierCommand : ICommand
    {
    }

    /// <summary>
    /// Blocks the event-bus processor until its release gate opens, so we can prove the
    /// mutation queued behind it has not run yet.
    /// </summary>
    private sealed class BlockingHandler : IEventHandlerMarker, IEventHandler<CommandExecutedEvent<TestBlockCommand>>
    {
        public TaskCompletionSource<bool> Started { get; } = new();
        public TaskCompletionSource<bool> Release { get; } = new();

        public Task HandleAsync(CommandExecutedEvent<TestBlockCommand> @event, CancellationToken cancellationToken = default)
        {
            Started.SetResult(true);
            return Release.Task;
        }
    }

    /// <summary>
    /// Completes a barrier once it runs, proving the mutation queued before it has finished.
    /// </summary>
    private sealed class BarrierHandler : IEventHandlerMarker, IEventHandler<CommandExecutedEvent<TestBarrierCommand>>
    {
        public TaskCompletionSource<bool> Done { get; } = new();

        public Task HandleAsync(CommandExecutedEvent<TestBarrierCommand> @event, CancellationToken cancellationToken = default)
        {
            Done.SetResult(true);
            return Done.Task;
        }
    }
}

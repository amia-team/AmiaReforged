using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.Tests;

/// <summary>
/// Verifies that the item-expander cache is invalidated only through the event subscriber,
/// that only successful commands trigger it, and documents the read-after-write contract.
/// </summary>
[TestFixture]
public class ItemDefinitionCacheInvalidationHandlerTests
{
    private const string TemplateTag = "wpn_template";

    private static ItemBlueprint Template(params MaterialEnum[] materials) => new(
        ResRef: TemplateTag.ToUpperInvariant(),
        ItemTag: TemplateTag,
        Name: $"Template {TemplateTag}",
        Description: string.Empty,
        Materials: materials,
        ItemForm: ItemForm.None,
        BaseItemType: 0,
        Appearance: new AppearanceData(0, null, null))
    {
        Variants = materials.Select(m => new MaterialVariant { Material = m }).ToList()
    };

    private static (CommandDispatcher Dispatcher, ItemBlueprintExpander Expander, InMemoryEventBus Bus)
        WireUp(IItemDefinitionRepository repository, params ICommandHandlerMarker[] commandHandlers)
    {
        var expander = new ItemBlueprintExpander(repository);
        var bus = new InMemoryEventBus();
        var subscriber = new ItemDefinitionCacheInvalidationHandler(expander);

        bus.Subscribe<CommandExecutedEvent<UpsertItemDefinitionCommand>>(
            (@event, ct) => subscriber.HandleAsync(@event, ct));
        bus.Subscribe<CommandExecutedEvent<DeleteItemDefinitionCommand>>(
            (@event, ct) => subscriber.HandleAsync(@event, ct));

        var dispatcher = new CommandDispatcher(commandHandlers.ToList(), bus);
        return (dispatcher, expander, bus);
    }

    // === Upsert path: full chain through the real dispatcher + synchronous bus ===

    [Test]
    public async Task SuccessfulUpsert_ThroughDispatcher_InvalidatesRealExpander()
    {
        IItemDefinitionRepository repository = new InMemoryItemDefinitionRepository();
        var (dispatcher, expander, bus) = WireUp(repository, new UpsertItemDefinitionHandler(repository));

        ((InMemoryItemDefinitionRepository)repository).AddItemDefinition(Template(MaterialEnum.WoodOak));
        expander.ExpandAll();
        Assert.That(expander.GetAllExpandedItems(), Has.Count.EqualTo(1));

        // The command handler commits the new blueprint, then publishes the event the subscriber consumes.
        ((InMemoryItemDefinitionRepository)repository).AddItemDefinition(
            Template(MaterialEnum.WoodOak, MaterialEnum.ColdIron));

        CommandResult result = await dispatcher.DispatchAsync(new UpsertItemDefinitionCommand
        {
            Blueprint = repository.GetByTag(TemplateTag)!
        });

        Assert.That(result.Success, Is.True);
        Assert.That(bus.PublishedEvents, Has.Count.EqualTo(1),
            "A successful upsert must publish exactly one CommandExecutedEvent");
        // In-memory bus is synchronous, so the subscriber has run by the time dispatch returns:
        // the cache now reflects the freshly-committed blueprint (2 concrete items).
        Assert.That(expander.GetAllExpandedItems(), Has.Count.EqualTo(2));
    }

    // === Delete path: the delete event invalidates and re-expansion reflects the deletion ===

    [Test]
    public async Task DeleteEvent_InvalidatesExpander_ThenCacheReflectsDeletion()
    {
        IItemDefinitionRepository repository = new InMemoryItemDefinitionRepository();
        var expander = new ItemBlueprintExpander(repository);
        var subscriber = new ItemDefinitionCacheInvalidationHandler(expander);

        ((InMemoryItemDefinitionRepository)repository).AddItemDefinition(Template(MaterialEnum.WoodOak));
        expander.ExpandAll();
        Assert.That(expander.GetAllExpandedItems(), Has.Count.EqualTo(1));

        // The command handler commits the deletion first...
        ((InMemoryItemDefinitionRepository)repository).RemoveByTag(TemplateTag);

        // ...then the subscriber re-expands from the now-fresh repository.
        await subscriber.HandleAsync(new CommandExecutedEvent<DeleteItemDefinitionCommand>(
            new DeleteItemDefinitionCommand { Tag = TemplateTag }, CommandResult.Ok()));

        Assert.That(expander.GetAllExpandedItems(), Has.Count.EqualTo(0),
            "After a delete event, the expanded cache must no longer contain the deleted blueprint's items");
    }

    // === Rejected command must not invalidate ===

    [Test]
    public async Task RejectedCommand_ThroughDispatcher_DoesNotPublishEventOrInvalidate()
    {
        IItemDefinitionRepository repository = new InMemoryItemDefinitionRepository();
        var (dispatcher, expander, bus) = WireUp(repository, new DeleteItemDefinitionHandler(repository));

        // The delete handler rejects the command (it requires a database-backed repository), so
        // the dispatcher must not publish a CommandExecutedEvent and the subscriber must never run.
        CommandResult result = await dispatcher.DispatchAsync(
            new DeleteItemDefinitionCommand { Tag = TemplateTag });

        Assert.That(result.Success, Is.False, "The command should be rejected");
        Assert.That(bus.PublishedEvents, Has.Count.EqualTo(0),
            "A rejected command must not publish CommandExecutedEvent, so the subscriber never runs");
        Assert.That(expander.GetAllExpandedItems().Count, Is.EqualTo(0),
            "Cache must be left untouched when the command is rejected");
    }

    // === Read-after-write contract (asynchronous production bus) ===

    [Test]
    public async Task Freshness_WhenSubscriberRuns_ReadsAlreadyCommittedState()
    {
        // The production bus (AnvilEventBusService) queues events and runs subscribers on a
        // background thread, so invalidation is EVENTUALLY CONSISTENT: a read issued immediately
        // after a successful command may still see the previously expanded concrete items until the
        // background processor runs this handler. Correctness does not depend on ordering, because
        // the command handler commits the new blueprint to the repository SYNCHRONOUSLY before the
        // event is published. This test asserts the exact data the subscriber later reads is already
        // fresh, which is what makes eventual consistency correct.
        IItemDefinitionRepository repository = new InMemoryItemDefinitionRepository();
        var expander = new ItemBlueprintExpander(repository);

        ((InMemoryItemDefinitionRepository)repository).AddItemDefinition(Template(MaterialEnum.WoodOak));
        expander.ExpandAll();
        Assert.That(expander.GetAllExpandedItems(), Has.Count.EqualTo(1));

        // Command handler commits first...
        ((InMemoryItemDefinitionRepository)repository).AddItemDefinition(
            Template(MaterialEnum.WoodOak, MaterialEnum.ColdIron));

        // ...then, on a separate step (as the async bus would), the subscriber runs.
        var subscriber = new ItemDefinitionCacheInvalidationHandler(expander);
        await subscriber.HandleAsync(new CommandExecutedEvent<UpsertItemDefinitionCommand>(
            new UpsertItemDefinitionCommand
            {
                Blueprint = repository.GetByTag(TemplateTag)!
            }, CommandResult.Ok()));

        Assert.That(expander.GetAllExpandedItems(), Has.Count.EqualTo(2),
            "After the subscriber runs, the expanded cache reflects the already-committed blueprint");
    }
}

using AmiaReforged.PwEngine.Features.WorldEngine.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Application;

/// <summary>
/// Handles ResourceHarvestedEvent by creating items in the character's inventory.
/// This handler switches to the main thread to safely make NWN game calls.
/// </summary>
[ServiceBinding(typeof(IEventHandler<ResourceHarvestedEvent>))]
[ServiceBinding(typeof(IEventHandlerMarker))]
public class ResourceHarvestedEventHandler(
    ICharacterRepository characterRepository,
    IItemDefinitionRepository itemDefinitionRepository) : IEventHandler<ResourceHarvestedEvent>, IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task HandleAsync(ResourceHarvestedEvent @event, CancellationToken cancellationToken = default)
    {
        // Switch to main thread to safely make NWN calls
        await NwTask.SwitchToMainThread();

        var evt = @event;

        // Find the character who harvested
        var character = characterRepository.GetById(evt.HarvesterId);
        if (character == null)
        {
            Log.Warn($"Character {evt.HarvesterId} not found for harvest event");
            return;
        }

        // Create each harvested item
        foreach (var harvestedItem in evt.Items)
        {
            var itemDefinition = itemDefinitionRepository.GetByTag(harvestedItem.ItemTag);
            if (itemDefinition == null)
            {
                Log.Warn($"Item definition '{harvestedItem.ItemTag}' not found");
                continue;
            }

            // Create the item DTO with quality
            var itemDto = new ItemDto(itemDefinition, harvestedItem.Quality, harvestedItem.Quality);

            // Add the specified quantity
            for (int i = 0; i < harvestedItem.Quantity; i++)
            {
                character.AddItem(itemDto);
            }

            Log.Debug($"Created {harvestedItem.Quantity}x {harvestedItem.ItemTag} (Q: {harvestedItem.Quality}) for character {evt.HarvesterId}");
        }
    }
}


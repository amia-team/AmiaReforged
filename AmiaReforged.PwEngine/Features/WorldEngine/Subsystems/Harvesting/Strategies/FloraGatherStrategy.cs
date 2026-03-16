using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Nui;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Services;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Strategies;

/// <summary>
/// Harvest strategy for Flora-type nodes (plants, herbs, mushrooms, etc.).
/// Player interacts (clicks/uses) with the placeable — no combat required.
/// The plant is instantly harvested and removed on use.
/// </summary>
[ServiceBinding(typeof(INodeHarvestStrategy))]
public sealed class FloraGatherStrategy(
    RuntimeCharacterService characterService,
    Lazy<RuntimeNodeService> runtimeNodeService,
    IResourceNodeInstanceRepository nodeRepository,
    IEventBus eventBus,
    WindowDirector windowDirector) : INodeHarvestStrategy
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly HashSet<ResourceType> Types = new() { ResourceType.Flora };

    public IReadOnlySet<ResourceType> SupportedTypes => Types;

    public void WireEvents(NwPlaceable placeable, SpawnedNode node)
    {
        placeable.OnUsed += HandleGather;
    }

    public void UnwireEvents(NwPlaceable placeable)
    {
        placeable.OnUsed -= HandleGather;
    }

    private void HandleGather(PlaceableEvents.OnUsed obj)
    {
        NwPlaceable plc = obj.Placeable;
        if (!plc.IsValid) return;
        if (obj.UsedBy is null) return;
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;

        RuntimeCharacter? character = characterService.GetRuntimeCharacter(obj.UsedBy);
        if (character is null) return;

        SpawnedNode? spawnedNode = runtimeNodeService.Value.GetSpawnedNode(plc.UUID);
        if (spawnedNode is null) return;

        ResourceNodeInstance node = spawnedNode.Instance;
        ResourceNodeDefinition def = node.Definition;

        // Tool check (optional — many plants won't need a tool)
        if (def.Requirement.RequiredItemType != ItemForm.None)
        {
            ItemSnapshot? tool = character.GetEquipment().GetValueOrDefault(EquipmentSlots.RightHand);
            if (tool?.Type != def.Requirement.RequiredItemType)
            {
                player.FloatingTextString("You need the correct tool to gather this.");
                return;
            }
        }

        Guid characterId = character.GetId().Value;
        Guid nodeId = node.Id;

        _ = NwTask.Run(async () =>
        {
            try
            {
                await NwTask.SwitchToMainThread();

                // Calculate outputs
                List<HarvestedItem> harvestedItems = CalculateOutputs(node, character);

                // Publish harvest event — ResourceHarvestedEventHandler will grant items
                await eventBus.PublishAsync(new ResourceHarvestedEvent(
                    characterId,
                    nodeId,
                    def.Tag,
                    harvestedItems.ToArray(),
                    0, // remaining uses — always depleted on gather
                    DateTime.UtcNow));

                // Publish depletion event
                await eventBus.PublishAsync(new NodeDepletedEvent(
                    nodeId,
                    node.Area,
                    def.Tag,
                    characterId,
                    DateTime.UtcNow));

                await NwTask.SwitchToMainThread();

                // Show gathering VFX
                Effect gatherEffect = Effect.VisualEffect(VfxType.ImpHeadNature);
                plc.Location.ApplyEffect(EffectDuration.Instant, gatherEffect);

                // Show instant-complete progress bar
                HarvestProgressView view = new(player, $"Gathering {def.Name}");
                HarvestProgressPresenter presenter = view.Presenter;
                windowDirector.OpenWindow(presenter);
                presenter.UpdateProgress(1, 1);
                presenter.Complete();

                string summary = string.Join(", ", harvestedItems.Select(h => $"{h.Quantity}x {h.ItemTag}"));
                player.FloatingTextString($"Gathered: {summary}");

                // Destroy the node
                nodeRepository.Delete(node);
                nodeRepository.SaveChanges();
                node.Destroy();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error gathering flora");
            }
        });
    }

    /// <summary>
    /// Calculates harvest outputs using the standard <see cref="HarvestOutput"/> array
    /// with knowledge modifiers for quality and yield.
    /// </summary>
    private static List<HarvestedItem> CalculateOutputs(ResourceNodeInstance node, ICharacter character)
    {
        List<HarvestedItem> outputs = [];
        List<KnowledgeHarvestEffect> applicable =
            character.KnowledgeEffectsForResource(node.Definition.Tag, node.Definition.Type);

        foreach (HarvestOutput harvestOutput in node.Definition.Outputs)
        {
            // Chance check
            if (harvestOutput.Chance < 100)
            {
                int roll = Random.Shared.Next(100);
                if (roll >= harvestOutput.Chance) continue;
            }

            // Quality modifiers
            int totalQuality = (int)node.Quality;
            foreach (KnowledgeHarvestEffect qe in applicable.Where(e => e.StepModified == HarvestStep.Quality))
            {
                totalQuality = qe.Operation switch
                {
                    EffectOperation.Additive => totalQuality + (int)qe.Value,
                    EffectOperation.PercentMult => totalQuality + totalQuality * (int)qe.Value,
                    _ => totalQuality
                };
            }

            totalQuality = Math.Clamp(totalQuality, CraftingQuality.MinCraftable, CraftingQuality.MaxCraftable);

            // Yield modifiers
            int totalQuantity = harvestOutput.Quantity;
            foreach (KnowledgeHarvestEffect ye in applicable.Where(e => e.StepModified == HarvestStep.ItemYield))
            {
                totalQuantity = ye.Operation switch
                {
                    EffectOperation.Additive => totalQuantity + (int)ye.Value,
                    EffectOperation.PercentMult => totalQuantity + totalQuantity * (int)ye.Value,
                    _ => totalQuantity
                };
            }

            outputs.Add(new HarvestedItem(harvestOutput.ItemDefinitionTag, totalQuantity, (IPQuality)totalQuality));
        }

        return outputs;
    }
}

using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Events;
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
/// Harvest strategy for Tree-type nodes.
/// Uses the same attack pattern as minerals but with different behavior:
/// the player chops for <c>BaseHarvestRounds</c> rounds, then the tree is
/// felled (destroyed) and a quality-scaled random number of logs are granted.
/// Trees are always single-use — once felled, they're gone.
/// </summary>
[ServiceBinding(typeof(INodeHarvestStrategy))]
public sealed class TreeFellingStrategy(
    RuntimeCharacterService characterService,
    Lazy<RuntimeNodeService> runtimeNodeService,
    ICharacterRepository characterRepository,
    IResourceNodeInstanceRepository nodeRepository,
    IEventBus eventBus) : INodeHarvestStrategy
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Tracks chop progress per placeable UUID. Keyed by placeable UUID,
    /// value is accumulated progress ticks. Cleared when the tree is felled.
    /// </summary>
    private readonly Dictionary<Guid, int> _chopProgress = new();

    private static readonly HashSet<ResourceType> Types = new() { ResourceType.Tree };

    public IReadOnlySet<ResourceType> SupportedTypes => Types;

    public void WireEvents(NwPlaceable placeable, SpawnedNode node)
    {
        placeable.OnPhysicalAttacked += HandleChop;
    }

    public void UnwireEvents(NwPlaceable placeable)
    {
        placeable.OnPhysicalAttacked -= HandleChop;
        _chopProgress.Remove(placeable.UUID);
    }

    private void HandleChop(PlaceableEvents.OnPhysicalAttacked obj)
    {
        NwPlaceable plc = obj.Placeable;
        if (!plc.IsValid) return;
        if (obj.Attacker is null) return;
        if (!obj.Attacker.IsPlayerControlled(out NwPlayer? player)) return;

        RuntimeCharacter? character = characterService.GetRuntimeCharacter(obj.Attacker);
        if (character is null) return;

        SpawnedNode? spawnedNode = runtimeNodeService.Value.GetSpawnedNode(plc.UUID);
        if (spawnedNode is null) return;

        ResourceNodeInstance node = spawnedNode.Instance;
        ResourceNodeDefinition def = node.Definition;

        // Tool check
        if (def.Requirement.RequiredItemType != ItemForm.None)
        {
            ItemSnapshot? tool = character.GetEquipment().GetValueOrDefault(EquipmentSlots.RightHand);
            if (tool?.Type != def.Requirement.RequiredItemType)
            {
                player.FloatingTextString("You need the correct tool to chop this tree.");
                return;
            }
        }

        // Calculate progress modifier from knowledge effects
        int progressMod = 0;
        foreach (KnowledgeHarvestEffect effect in character
                     .KnowledgeEffectsForResource(def.Tag, def.Type)
                     .Where(e => e.StepModified == HarvestStep.HarvestStepRate))
        {
            if (effect.Operation == EffectOperation.Additive)
            {
                progressMod += (int)effect.Value;
            }
        }

        // Accumulate progress
        _chopProgress.TryGetValue(plc.UUID, out int current);
        current += 1 + progressMod;
        _chopProgress[plc.UUID] = current;

        int required = def.BaseHarvestRounds > 0 ? def.BaseHarvestRounds : 1;

        if (current < required)
        {
            // Still chopping — show VFX and progress text
            Effect dustEffect = Effect.VisualEffect(VfxType.ImpDustExplosion, false, 0.4f);
            plc.Location.ApplyEffect(EffectDuration.Instant, dustEffect);
            player.FloatingTextString($"Chopping... ({current}/{required})");
            return;
        }

        // Tree is felled — compute yield and fire events
        _chopProgress.Remove(plc.UUID);

        Guid characterId = character.GetId().Value;
        Guid nodeId = node.Id;

        _ = NwTask.Run(async () =>
        {
            try
            {
                await NwTask.SwitchToMainThread();

                // Calculate log yield
                List<HarvestedItem> harvestedItems = CalculateTreeYield(node, character);

                // Publish harvest event — the existing ResourceHarvestedEventHandler will grant items
                await eventBus.PublishAsync(new ResourceHarvestedEvent(
                    characterId,
                    nodeId,
                    def.Tag,
                    harvestedItems.ToArray(),
                    0, // remaining uses — always 0 for trees
                    DateTime.UtcNow));

                // Publish depletion event — always depleted after one felling
                await eventBus.PublishAsync(new NodeDepletedEvent(
                    nodeId,
                    node.Area,
                    def.Tag,
                    characterId,
                    DateTime.UtcNow));

                await NwTask.SwitchToMainThread();

                // Build summary text
                string summary = string.Join(", ", harvestedItems.Select(h => $"{h.Quantity}x {h.ItemTag}"));
                player.FloatingTextString($"Timber! Harvested: {summary}");

                // Destroy node in repository and game world
                nodeRepository.Delete(node);
                nodeRepository.SaveChanges();
                node.Destroy();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error felling tree");
            }
        });
    }

    /// <summary>
    /// Calculates the number and quality of logs yielded when a tree is felled.
    /// Uses <see cref="TreeProperties"/> for the baseline min/max, then adjusts
    /// by the node's quality (richness). Higher quality = more logs.
    /// </summary>
    private static List<HarvestedItem> CalculateTreeYield(ResourceNodeInstance node, ICharacter character)
    {
        TreeProperties? treeProps = node.Definition.TreeProperties;
        if (treeProps is null)
        {
            // Fallback: use standard HarvestOutput[] if no TreeProperties configured
            return CalculateFallbackOutputs(node, character);
        }

        // Quality offset: Average is the baseline (offset = 0)
        // Each quality level above/below Average shifts the range by 1
        int qualityOffset = (int)node.Quality - (int)IPQuality.Average;

        int adjustedMin = Math.Max(1, treeProps.MinLogs + qualityOffset);
        int adjustedMax = Math.Max(adjustedMin, treeProps.MaxLogs + qualityOffset);

        // Apply knowledge yield modifiers
        int yieldMod = 0;
        foreach (KnowledgeHarvestEffect effect in character
                     .KnowledgeEffectsForResource(node.Definition.Tag, node.Definition.Type)
                     .Where(e => e.StepModified == HarvestStep.ItemYield))
        {
            if (effect.Operation == EffectOperation.Additive)
            {
                yieldMod += (int)effect.Value;
            }
        }

        adjustedMin = Math.Max(1, adjustedMin + yieldMod);
        adjustedMax = Math.Max(adjustedMin, adjustedMax + yieldMod);

        int logCount = Random.Shared.Next(adjustedMin, adjustedMax + 1);

        // Quality modifiers for the logs themselves
        int totalQuality = (int)node.Quality;
        foreach (KnowledgeHarvestEffect qe in character
                     .KnowledgeEffectsForResource(node.Definition.Tag, node.Definition.Type)
                     .Where(e => e.StepModified == HarvestStep.Quality))
        {
            totalQuality = qe.Operation switch
            {
                EffectOperation.Additive => totalQuality + (int)qe.Value,
                EffectOperation.PercentMult => totalQuality + totalQuality * (int)qe.Value,
                _ => totalQuality
            };
        }

        totalQuality = Math.Clamp(totalQuality, CraftingQuality.MinCraftable, CraftingQuality.MaxCraftable);

        return [new HarvestedItem(treeProps.LogItemTag, logCount, (IPQuality)totalQuality)];
    }

    /// <summary>
    /// Fallback: if no <see cref="TreeProperties"/> is configured, use the standard
    /// <see cref="HarvestOutput"/> array with the same logic as minerals.
    /// </summary>
    private static List<HarvestedItem> CalculateFallbackOutputs(ResourceNodeInstance node, ICharacter character)
    {
        List<HarvestedItem> outputs = [];
        List<KnowledgeHarvestEffect> applicable =
            character.KnowledgeEffectsForResource(node.Definition.Tag, node.Definition.Type);

        foreach (HarvestOutput harvestOutput in node.Definition.Outputs)
        {
            if (harvestOutput.Chance < 100)
            {
                int roll = Random.Shared.Next(100);
                if (roll >= harvestOutput.Chance) continue;
            }

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

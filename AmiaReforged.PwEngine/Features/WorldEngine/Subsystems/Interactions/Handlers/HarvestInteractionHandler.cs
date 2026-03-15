using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Handlers;

/// <summary>
/// Interaction handler for harvesting resource nodes.
/// Replaces the direct logic in <c>HarvestResourceCommandHandler</c> while preserving
/// identical game-play behavior: tool check → tick with knowledge rate mods → yield/quality on complete.
/// </summary>
[ServiceBinding(typeof(IInteractionHandler))]
public sealed class HarvestInteractionHandler(
    IResourceNodeInstanceRepository nodeRepository,
    IEventBus eventBus) : IInteractionHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public string InteractionTag => "harvesting";
    public InteractionTargetMode TargetMode => InteractionTargetMode.Node;

    public PreconditionResult CanStart(ICharacter character, InteractionContext context)
    {
        ResourceNodeInstance? node = FindNode(context.TargetId);
        if (node is null)
        {
            return PreconditionResult.Fail("Resource node not found");
        }

        // Tool check — mirrors original HarvestResourceCommandHandler behavior
        if (node.Definition.Requirement.RequiredItemType != ItemForm.None)
        {
            character.GetEquipment().TryGetValue(EquipmentSlots.RightHand, out ItemSnapshot? tool);
            if (tool?.Type != node.Definition.Requirement.RequiredItemType)
            {
                return PreconditionResult.Fail("Required tool not equipped");
            }
        }

        return PreconditionResult.Success();
    }

    public int CalculateRequiredRounds(ICharacter character, InteractionContext context)
    {
        ResourceNodeInstance node = FindNode(context.TargetId)!;
        return node.Definition.BaseHarvestRounds;
    }

    public TickResult OnTick(InteractionSession session, ICharacter character)
    {
        ResourceNodeInstance? node = FindNode(session.TargetId);
        if (node is null)
        {
            session.Status = InteractionStatus.Failed;
            return new TickResult(InteractionStatus.Failed, session.Progress, session.RequiredRounds,
                "Resource node disappeared during harvest");
        }

        // Calculate progress modifier from knowledge effects
        int progressMod = 0;
        foreach (KnowledgeHarvestEffect effect in character
                     .KnowledgeEffectsForResource(node.Definition.Tag, node.Definition.Type)
                     .Where(e => e.StepModified == HarvestStep.HarvestStepRate))
        {
            if (effect.Operation == EffectOperation.Additive)
            {
                progressMod += (int)effect.Value;
            }
        }

        int newProgress = session.IncrementProgress(1 + progressMod);
        InteractionStatus status = session.IsComplete
            ? InteractionStatus.Completed
            : InteractionStatus.Active;

        return new TickResult(status, newProgress, session.RequiredRounds);
    }

    public async Task<InteractionOutcome> OnCompleteAsync(
        InteractionSession session,
        ICharacter character,
        CancellationToken ct = default)
    {
        ResourceNodeInstance? node = FindNode(session.TargetId);
        if (node is null)
        {
            return InteractionOutcome.Failed("Resource node no longer exists");
        }

        // Calculate harvest outputs with knowledge modifiers
        List<HarvestedItem> harvestedItems = CalculateHarvestOutputs(node, character);

        // Publish domain event (listened to by item-grant subsystem, etc.)
        await eventBus.PublishAsync(new ResourceHarvestedEvent(
            session.CharacterId,
            node.Id,
            node.Definition.Tag,
            harvestedItems.ToArray(),
            node.Uses - 1,
            DateTime.UtcNow), ct);

        // Decrement uses and reset for next harvest
        node.DecrementUses();
        node.ResetHarvestProgress();

        // Check depletion
        if (node.Uses <= 0)
        {
            await eventBus.PublishAsync(new NodeDepletedEvent(
                node.Id,
                node.Area,
                node.Definition.Tag,
                session.CharacterId,
                DateTime.UtcNow), ct);

            nodeRepository.Delete(node);
            nodeRepository.SaveChanges();

            return InteractionOutcome.Succeeded("Node depleted", new Dictionary<string, object>
            {
                ["status"] = "NodeDepleted",
                ["items"] = harvestedItems
            });
        }

        nodeRepository.Update(node);
        nodeRepository.SaveChanges();

        return InteractionOutcome.Succeeded(data: new Dictionary<string, object>
        {
            ["items"] = harvestedItems
        });
    }

    public void OnCancel(InteractionSession session, ICharacter character)
    {
        Log.Debug("Harvest cancelled for character {CharacterId} on node {NodeId}",
            session.CharacterId, session.TargetId);
        // No cleanup needed — progress is simply discarded
    }

    private ResourceNodeInstance? FindNode(Guid targetId)
        => nodeRepository.GetInstances().FirstOrDefault(n => n.Id == targetId);

    private static List<HarvestedItem> CalculateHarvestOutputs(ResourceNodeInstance node, ICharacter character)
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

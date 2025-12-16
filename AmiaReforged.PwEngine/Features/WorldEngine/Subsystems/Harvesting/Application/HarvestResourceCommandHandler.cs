using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Application;

[ServiceBinding(typeof(ICommandHandler<HarvestResourceCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public class HarvestResourceCommandHandler(
    IResourceNodeInstanceRepository nodeRepository,
    ICharacterRepository characterRepository,
    IEventBus eventBus) : ICommandHandler<HarvestResourceCommand>
{
    // Cache active harvest sessions to preserve transient HarvestProgress
    private static readonly Dictionary<Guid, ResourceNodeInstance> _activeHarvestSessions = new();

    public async Task<CommandResult> HandleAsync(HarvestResourceCommand command, CancellationToken cancellationToken = default)
    {

        // Get or load the node instance (use cached version to preserve HarvestProgress)
        if (!_activeHarvestSessions.TryGetValue(command.NodeInstanceId, out ResourceNodeInstance? node))
        {
            node = nodeRepository.GetInstances().FirstOrDefault(n => n.Id == command.NodeInstanceId);
            if (node == null)
            {
                return CommandResult.Fail("Node not found");
            }

            // Cache the instance for this harvest session
            _activeHarvestSessions[command.NodeInstanceId] = node;
        }

        // Find the character
        ICharacter? character = characterRepository.GetById(command.HarvesterId);
        if (character == null)
        {
            return CommandResult.Fail("Character not found");
        }

        // Check if character has the required tool
        ItemSnapshot? tool = character.GetEquipment()[EquipmentSlots.RightHand];
        if (node.Definition.Requirement.RequiredItemType != JobSystemItemType.None &&
            tool?.Type != node.Definition.Requirement.RequiredItemType)
        {
            return CommandResult.Fail("Required tool not equipped");
        }

        // Calculate harvest progress modifications
        int harvestProgressMod = 0;
        foreach (KnowledgeHarvestEffect effect in character.KnowledgeEffectsForResource(node.Definition.Tag)
                     .Where(r => r.StepModified == HarvestStep.HarvestStepRate))
        {
            switch (effect.Operation)
            {
                case EffectOperation.Additive:
                    harvestProgressMod += (int)effect.Value;
                    break;
            }
        }

        // Update harvest progress
        int currentProgress = node.IncrementHarvestProgress(1 + harvestProgressMod);

        if (currentProgress < node.Definition.BaseHarvestRounds)
        {
            // Still harvesting - keep instance cached for next round
            return CommandResult.OkWith("status", "InProgress");
        }

        // Harvest is complete - calculate outputs
        List<HarvestedItem> harvestedItems = CalculateHarvestOutputs(node, character);

        // Publish harvest event
        await eventBus.PublishAsync(new ResourceHarvestedEvent(
            command.HarvesterId,
            node.Id,
            node.Definition.Tag,
            harvestedItems.ToArray(),
            node.Uses - 1,
            DateTime.UtcNow), cancellationToken);

        // Decrement uses and reset progress
        node.DecrementUses();
        node.ResetHarvestProgress();

        // Check if node is depleted
        if (node.Uses <= 0)
        {
            await eventBus.PublishAsync(new NodeDepletedEvent(
                node.Id,
                node.Area,
                node.Definition.Tag,
                command.HarvesterId,
                DateTime.UtcNow), cancellationToken);

            nodeRepository.Delete(node);
            nodeRepository.SaveChanges();

            // Remove from cache - node is gone
            _activeHarvestSessions.Remove(command.NodeInstanceId);

            return CommandResult.OkWith("status", "NodeDepleted");
        }

        nodeRepository.Update(node);
        nodeRepository.SaveChanges();

        // Remove from cache - harvest complete, reset for next harvester
        _activeHarvestSessions.Remove(command.NodeInstanceId);

        return CommandResult.OkWith("status", "Completed");
    }

    private List<HarvestedItem> CalculateHarvestOutputs(ResourceNodeInstance node, ICharacter character)
    {
        List<HarvestedItem> outputs = new List<HarvestedItem>();
        List<KnowledgeHarvestEffect> applicable = character.KnowledgeEffectsForResource(node.Definition.Tag);

        foreach (HarvestOutput harvestOutput in node.Definition.Outputs)
        {
            // Check drop chance
            if (harvestOutput.Chance < 100)
            {
                int randomPercentage = Random.Shared.Next(100);
                if (randomPercentage >= harvestOutput.Chance)
                {
                    continue;
                }
            }

            // Calculate quality
            int totalQuality = (int)node.Quality;
            List<KnowledgeHarvestEffect> qualityImprovements = applicable.Where(he => he.StepModified == HarvestStep.Quality).ToList();
            foreach (KnowledgeHarvestEffect qualityImprovement in qualityImprovements)
            {
                switch (qualityImprovement.Operation)
                {
                    case EffectOperation.Additive:
                        totalQuality += (int)qualityImprovement.Value;
                        break;
                    case EffectOperation.PercentMult:
                        totalQuality += totalQuality * (int)qualityImprovement.Value;
                        break;
                }
            }

            totalQuality = Math.Max((int)IPQuality.VeryPoor,
                Math.Min((int)IPQuality.Masterwork, totalQuality));

            // Calculate quantity
            int totalQuantity = harvestOutput.Quantity;
            List<KnowledgeHarvestEffect> yieldImprovements = applicable.Where(he => he.StepModified == HarvestStep.ItemYield).ToList();
            foreach (KnowledgeHarvestEffect yieldImprovement in yieldImprovements)
            {
                switch (yieldImprovement.Operation)
                {
                    case EffectOperation.Additive:
                        totalQuantity += (int)yieldImprovement.Value;
                        break;
                    case EffectOperation.PercentMult:
                        totalQuantity += totalQuantity * (int)yieldImprovement.Value;
                        break;
                }
            }

            outputs.Add(new HarvestedItem(harvestOutput.ItemDefinitionTag, totalQuantity, (IPQuality)totalQuality));
        }

        return outputs;
    }
}


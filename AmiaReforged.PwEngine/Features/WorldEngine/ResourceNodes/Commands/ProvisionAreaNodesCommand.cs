using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.Commands;

/// <summary>
/// Command to provision resource nodes for an area based on its definition.
/// This triggers the entire flow: generate nodes, spawn in-game, and persist state.
/// </summary>
public class ProvisionAreaNodesCommand : ICommand
{
    public AreaDefinition AreaDefinition { get; }
    public bool ForceRespawn { get; }

    public ProvisionAreaNodesCommand(AreaDefinition areaDefinition, bool forceRespawn = false)
    {
        AreaDefinition = areaDefinition;
        ForceRespawn = forceRespawn;
    }
}


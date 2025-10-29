using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.Services;

/// <summary>
/// Service that automatically provisions resource nodes when areas load.
/// Provisions all areas on server startup.
/// </summary>
[ServiceBinding(typeof(AreaProvisioningService))]
public class AreaProvisioningService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly ICommandHandler<ProvisionAreaNodesCommand> _provisionHandler;
    private readonly IRegionRepository _regionRepository;
    private readonly HashSet<string> _provisionedAreas = new();

    public AreaProvisioningService(
        ICommandHandler<ProvisionAreaNodesCommand> provisionHandler,
        IRegionRepository regionRepository)
    {
        _provisionHandler = provisionHandler;
        _regionRepository = regionRepository;

        // Schedule provisioning on the main game thread using Anvil's scheduler
        // This ensures NWN API calls work correctly
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5)); // Wait for server initialization
            await NwTask.SwitchToMainThread(); // Switch to main thread for NWN API access
            await ProvisionAllLoadedAreas();
        });

        Log.Info("AreaProvisioningService initialized - provisioning scheduled");
    }

    // ...existing ProvisionAreaIfNeeded method...


    private async Task ProvisionAreaIfNeeded(NwArea nwArea)
    {
        if (_provisionedAreas.Contains(nwArea.ResRef))
        {
            Log.Debug($"Area {nwArea.ResRef} already provisioned this session");
            return;
        }

        // Find area definition in any region
        AreaDefinition? areaDefinition = null;
        foreach (RegionDefinition region in _regionRepository.All())
        {
            areaDefinition = region.Areas.FirstOrDefault(a => a.ResRef.Value == nwArea.ResRef);
            if (areaDefinition != null)
                break;
        }

        if (areaDefinition == null)
        {
            Log.Debug($"No area definition found for {nwArea.ResRef}");
            return;
        }

        if (areaDefinition.DefinitionTags.Count == 0)
        {
            Log.Debug($"No resource definitions for area {nwArea.ResRef}");
            return;
        }

        Log.Info($"Provisioning area {nwArea.Name} ({nwArea.ResRef}) with {areaDefinition.DefinitionTags.Count} resource types");

        ProvisionAreaNodesCommand command = new ProvisionAreaNodesCommand(areaDefinition);
        CommandResult result = await _provisionHandler.HandleAsync(command);

        if (result.Success)
        {
            _provisionedAreas.Add(nwArea.ResRef);

            if (result.Data != null)
            {
                int nodeCount = (int)result.Data["provisionedCount"];
                bool skipped = (bool)result.Data["skipped"];

                if (!skipped && nodeCount > 0)
                {
                    Log.Info($"✓ Provisioned {nodeCount} nodes in area {nwArea.Name} ({nwArea.ResRef})");
                }
                else if (skipped)
                {
                    Log.Info($"✓ Area {nwArea.Name} ({nwArea.ResRef}) already has nodes");
                }
            }
        }
        else
        {
            Log.Error($"✗ Failed to provision nodes for area {nwArea.ResRef}: {result.ErrorMessage}");
        }
    }

    private async Task ProvisionAllLoadedAreas()
    {
        Log.Info("=== Starting Resource Node Provisioning ===");

        List<NwArea> areas = NwModule.Instance.Areas.ToList();
        Log.Info($"Found {areas.Count} loaded areas in module");

        foreach (NwArea area in areas)
        {
            await ProvisionAreaIfNeeded(area);
        }

        Log.Info($"=== Resource Node Provisioning Complete: {_provisionedAreas.Count} areas provisioned ===");
    }

    /// <summary>
    /// Manually trigger provisioning for a specific area (useful for DM commands).
    /// </summary>
    public async Task<CommandResult> ProvisionArea(string areaResRef, bool forceRespawn = false)
    {
        NwArea? nwArea = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == areaResRef);
        if (nwArea == null)
        {
            return CommandResult.Fail($"Area {areaResRef} not found in module");
        }

        AreaDefinition? areaDefinition = null;
        foreach (RegionDefinition region in _regionRepository.All())
        {
            areaDefinition = region.Areas.FirstOrDefault(a => a.ResRef.Value == areaResRef);
            if (areaDefinition != null)
                break;
        }

        if (areaDefinition == null)
        {
            return CommandResult.Fail($"No area definition found for {areaResRef}");
        }

        ProvisionAreaNodesCommand command = new ProvisionAreaNodesCommand(areaDefinition, forceRespawn);
        CommandResult result = await _provisionHandler.HandleAsync(command);

        if (result.Success && forceRespawn)
        {
            // Remove from provisioned set so it can be re-provisioned on next load if needed
            _provisionedAreas.Remove(areaResRef);
        }

        return result;
    }
}


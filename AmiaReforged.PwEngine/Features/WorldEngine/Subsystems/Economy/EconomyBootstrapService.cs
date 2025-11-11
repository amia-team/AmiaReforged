using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Services;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy;

/// <summary>
/// Responsible for loading previous economic states, such as the location of resource nodes.
/// </summary>
[ServiceBinding(typeof(EconomyBootstrapService))]
public class EconomyBootstrapService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly ResourceNodeService _nodeService;
    private readonly IResourceNodeInstanceRepository _nodes;
    private readonly IRegionRepository _regions;

    /// <summary>
    /// Responsible for loading previous economic states, such as the location of resource nodes.
    /// </summary>
    public EconomyBootstrapService(EconomyLoaderService loader, ResourceNodeService nodeService,
        IResourceNodeInstanceRepository nodes, IRegionRepository regions)
    {
        _nodeService = nodeService;
        _nodes = nodes;
        _regions = regions;

        loader.Startup();
        LoadFromDatabase();
    }


    private void LoadFromDatabase()
    {
        foreach (RegionDefinition reg in _regions.All())
        {
            foreach (AreaDefinition areaDefinition in reg.Areas)
            {
                Log.Info($"Spawning nodes in {areaDefinition.ResRef}");
                List<ResourceNodeInstance> persistentNodes = _nodes.GetInstancesByArea(areaDefinition.ResRef);

                persistentNodes.ForEach(n => _nodeService.SpawnInstance(n));
            }
        }
    }
}

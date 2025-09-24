using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Systems.WorldEngine.Domains;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

/// <summary>
/// Responsible for loading previous economic states, such as the location of resource nodes.
/// </summary>
[ServiceBinding(typeof(EconomyBootstrapService))]
public class EconomyBootstrapService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly ResourceNodeInstanceSetupService _setup;
    private readonly IResourceNodeInstanceRepository _nodes;
    private readonly IRegionRepository _regions;

    /// <summary>
    /// Responsible for loading previous economic states, such as the location of resource nodes.
    /// </summary>
    public EconomyBootstrapService(EconomyLoaderService loader, ResourceNodeInstanceSetupService setup,
        IResourceNodeInstanceRepository nodes, IRegionRepository regions)
    {
        _setup = setup;
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

                persistentNodes.ForEach(n => _setup.SpawnInstance(n));
            }
        }
    }
}

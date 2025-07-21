using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;
using AmiaReforged.PwEngine.Systems.WorldEngine.Economy.ResourceNodes;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

public interface ISubSystemInitializer
{
    void Init(WorldEngine worldEngine);
}

[ServiceBinding(typeof(ISubSystemInitializer))]
public class OreNodeInitializer : ISubSystemInitializer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void Init(WorldEngine worldEngine)
    {
        foreach (RegionDefinition region in worldEngine.Regions)
        {
            foreach (AreaDefinition area in region.Areas)
            {
                NwArea? gameArea = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == area.ResRef);
                if (gameArea == null)
                {
                    Log.Error($"Invalid resref for {region.Name}: {area.ResRef}");
                    continue;
                }

                IEnumerable<NwTrigger> triggers = gameArea.FindObjectsOfTypeInArea<NwTrigger>()
                    .Where(t => t.ResRef == WorldConfigConstants.ResourceNodeZoneRef);

                foreach (NwTrigger trigger in triggers)
                {
                    LocalVariableString v = trigger.GetObjectVariable<LocalVariableString>("node_tags");

                    if (v.HasNothing)
                    {
                        Log.Error($"No node tags found for {trigger.ResRef} in {area.ResRef}");
                        continue;
                    }
                    
                    string[] tags = v.Value!.Split(',');
                    
                    if(tags.All(s => s != "ore")) continue;
                    
                    
                    
                }
            }
        }
    }
}

[ServiceBinding(typeof(ISubSystemInitializer))]
public class TreeNodeInitializer : ISubSystemInitializer
{
    public void Init(WorldEngine worldEngine)
    {
    }
}
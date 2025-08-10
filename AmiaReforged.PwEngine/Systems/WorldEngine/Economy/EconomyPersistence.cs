using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy;

[ServiceBinding(typeof(EconomyPersistence))]
public class EconomyPersistence
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private EconomyContext _context;

    public EconomyPersistence(PwContextFactory factory)
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live") return;

        _context = factory.CreateEconomyContext();
    }

    public bool StoreNewNode(ResourceNodeInstance resourceNodeInstance)
    {
        try
        {
            _context.Add(resourceNodeInstance);
            _context.SaveChanges();
            Log.Info($"Node successfully persisted");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return false;
    }

    public void UpdateDefinitions(EconomyDefinitions definitions)
    {
        definitions.NodeDefinitions.ForEach(r =>
        {
            try
            {
                if (_context.NodeDefinitions.Any(e => e.Tag == r.Tag))
                {
                    _context.Update(r);
                }
                else
                {
                    _context.Add(r);
                }

                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to perform update for {r.Tag}: {ex.Message}");
            }
        });
    }

    public List<ResourceNodeDefinition> AllResourceDefinitions()
    {
        return _context.NodeDefinitions.ToList();
    }

    public List<ResourceNodeInstance> AllResourceNodes()
    {
        return _context.NodeInstances.Include(n => n.Location).ToList();
    }
}

using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy;

[ServiceBinding(typeof(EconomyPersistence))]
public class EconomyPersistence(PwContextFactory factory)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private EconomyContext _context = factory.CreateEconomyContext();

    public bool StoreNewNode(ResourceNodeInstance resourceNodeInstance)
    {
        try
        {
            _context.Add(resourceNodeInstance);
            _context.SaveChanges();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to store a new node to the DB in {resourceNodeInstance.Location.AreaResRef}");
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

    public List<ResourceNodeDefinition> GetStoredDefinitions()
    {
        return _context.NodeDefinitions.ToList();
    }
}

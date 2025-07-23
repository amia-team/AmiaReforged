using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy;

[ServiceBinding(typeof(EconomyPersistence))]
public class EconomyPersistence(PwContextFactory factory)
{
    private EconomyContext _context = factory.CreateEconomyContext();

    public void StoreNewNode(ResourceNodeInstance resourceNodeInstance)
    {
        _context.Add(resourceNodeInstance);
        _context.SaveChanges();
    }

    public void UpdateDefinitions(EconomyDefinitions definitions)
    {
        definitions.NodeDefinitions.ForEach(r =>
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
        });
    }
}

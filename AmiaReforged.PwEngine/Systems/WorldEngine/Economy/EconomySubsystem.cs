using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy;

[ServiceBinding(typeof(EconomySubsystem))]
public class EconomySubsystem
{
    private readonly List<IInitializable> _initializers;
    public EconomyDefinitions Definitions { get; }
    private EconomyPersistence Persistence { get; }

    public EconomySubsystem(EconomyDefinitions definitions, EconomyPersistence persistence, IWorldConfigProvider config, IEnumerable<IInitializable> initializers)
    {
        Definitions = definitions;
        Persistence = persistence;
        _initializers = initializers.ToList();

        bool initialized = config.GetBoolean(WorldConfigConstants.InitializedKey);

        if (!initialized)
        {
            // DoFirstTimeSetUp();
        }

        UpdateStoredDefinitions();
    }

    private void DoFirstTimeSetUp()
    {
        foreach (IInitializable subSystemInitializer in _initializers)
        {
            subSystemInitializer.Init(this);
        }
    }

    private void UpdateStoredDefinitions()
    {
        Persistence.UpdateDefinitions(Definitions);
    }


    public void PersistNode(ResourceNodeInstance resourceNodeInstance)
    {
        Persistence.StoreNewNode(resourceNodeInstance);
    }
}

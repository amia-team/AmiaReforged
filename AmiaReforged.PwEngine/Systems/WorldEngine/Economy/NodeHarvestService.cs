using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy;

[ServiceBinding(typeof(NodeHarvestService))]
public class NodeHarvestService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly WorldEngine.EconomySubsystem _engine;
    public NodeHarvestService(WorldEngine.EconomySubsystem engine)
    {
        _engine = engine;

        SetupOnHitNodes();
        SetupOnClickNodes();
    }

    private void SetupOnHitNodes()
    {
    }

    private void SetupOnClickNodes()
    {
    }
}
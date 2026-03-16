using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Strategies;

/// <summary>
/// Collects all <see cref="INodeHarvestStrategy"/> implementations via Anvil DI
/// and indexes them by <see cref="ResourceType"/> for O(1) lookup.
/// </summary>
[ServiceBinding(typeof(NodeHarvestStrategyRegistry))]
public sealed class NodeHarvestStrategyRegistry
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<ResourceType, INodeHarvestStrategy> _strategies = new();

    public NodeHarvestStrategyRegistry(IEnumerable<INodeHarvestStrategy> strategies)
    {
        foreach (INodeHarvestStrategy strategy in strategies)
        {
            foreach (ResourceType type in strategy.SupportedTypes)
            {
                if (_strategies.TryAdd(type, strategy))
                {
                    Log.Info("Registered harvest strategy '{Strategy}' for {Type}",
                        strategy.GetType().Name, type);
                }
                else
                {
                    Log.Warn("Duplicate harvest strategy for {Type}: '{Existing}' already registered, ignoring '{New}'",
                        type, _strategies[type].GetType().Name, strategy.GetType().Name);
                }
            }
        }
    }

    /// <summary>
    /// Returns the harvest strategy for the given <paramref name="type"/>, or <c>null</c> if none is registered.
    /// </summary>
    public INodeHarvestStrategy? GetStrategy(ResourceType type)
        => _strategies.GetValueOrDefault(type);
}

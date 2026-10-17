using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Combat UI selection also serves native ship magic without depending on the UI service.</summary>
[ServiceBinding(typeof(CombatTargetSelectionService))]
public sealed class CombatTargetSelectionService
{
    public ShipTargetSelection Selection { get; } = new();
}

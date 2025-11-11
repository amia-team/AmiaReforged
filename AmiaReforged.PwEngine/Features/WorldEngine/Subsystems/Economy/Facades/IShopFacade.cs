using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Facades;

/// <summary>
/// Gateway for shop and marketplace operations.
/// Provides access to player stalls and merchant activities.
/// </summary>
public interface IShopFacade
{
    // === Player Stall Management ===

    /// <summary>
    /// Claims a player stall for a persona.
    /// </summary>
    Task<CommandResult> ClaimPlayerStallAsync(ClaimPlayerStallCommand command, CancellationToken ct = default);

    /// <summary>
    /// Releases a player stall from a persona.
    /// </summary>
    Task<CommandResult> ReleasePlayerStallAsync(ReleasePlayerStallCommand command, CancellationToken ct = default);

    /// <summary>
    /// Lists a product on a player stall.
    /// </summary>
    Task<CommandResult> ListStallProductAsync(ListStallProductCommand command, CancellationToken ct = default);

    // Note: Additional methods for NPC shops, pricing, and inventory can be added as needed
}


using Anvil.API;
using AmiaReforged.PwEngine.Features.AI.Core.Models;

namespace AmiaReforged.PwEngine.Features.AI.Core.Interfaces;

/// <summary>
/// Interface for calculating party state for XP/loot purposes.
/// Extracts party analysis logic from RewardXPForKill() in inc_ds_ondeath.nss.
/// </summary>
public interface IPartyStateCalculator
{
    /// <summary>
    /// Calculates the current state of a party based on the killer.
    /// </summary>
    /// <param name="killer">The creature/player that dealt the killing blow.</param>
    /// <returns>The calculated party state.</returns>
    PartyState CalculatePartyState(NwGameObject killer);
}

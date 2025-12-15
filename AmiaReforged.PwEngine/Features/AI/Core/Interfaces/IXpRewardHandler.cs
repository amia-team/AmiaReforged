using Anvil.API;
using AmiaReforged.PwEngine.Features.AI.Core.Models;

namespace AmiaReforged.PwEngine.Features.AI.Core.Interfaces;

/// <summary>
/// Interface for calculating and distributing XP rewards on creature death.
/// Ported from RewardXPForKill() in inc_ds_ondeath.nss.
/// </summary>
public interface IXpRewardHandler
{
    /// <summary>
    /// Calculates and distributes XP/gold rewards for killing a creature.
    /// </summary>
    /// <param name="killedCreature">The creature that was killed.</param>
    /// <param name="killer">The creature/player that dealt the killing blow.</param>
    /// <returns>The result of the XP distribution.</returns>
    XpRewardResult CalculateAndDistributeRewards(NwCreature killedCreature, NwGameObject killer);
}

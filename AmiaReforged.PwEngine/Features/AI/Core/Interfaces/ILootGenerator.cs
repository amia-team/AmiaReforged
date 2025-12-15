using Anvil.API;
using AmiaReforged.PwEngine.Features.AI.Core.Models;

namespace AmiaReforged.PwEngine.Features.AI.Core.Interfaces;

/// <summary>
/// Interface for generating loot on creature death.
/// Ported from GenerateLoot() in inc_ds_ondeath.nss.
/// </summary>
public interface ILootGenerator
{
    /// <summary>
    /// Generates loot for a killed creature.
    /// </summary>
    /// <param name="killedCreature">The creature that was killed.</param>
    /// <param name="killer">The creature/player that dealt the killing blow.</param>
    /// <param name="xpResult">The XP reward result (affects loot chances).</param>
    /// <param name="isChest">Whether this is a treasure chest (not a creature).</param>
    /// <returns>The result of loot generation.</returns>
    LootGenerationResult GenerateLoot(
        NwGameObject killedCreature,
        NwGameObject killer,
        XpRewardResult xpResult,
        bool isChest = false);
}

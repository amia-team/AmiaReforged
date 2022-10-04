using Anvil.API;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Races.Races.Types.HeritageAbilities
{
    public class HalfDragonHeritageAbilities : IHeritageAbilities
    {
        public void SetupStats(NwPlayer player)
        {
            CreaturePlugin.ModifyRawAbilityScore(player.LoginCreature, NWScript.ABILITY_CONSTITUTION, 2);
            CreaturePlugin.ModifyRawAbilityScore(player.LoginCreature, NWScript.ABILITY_CHARISMA, 2);
        }
    }
}
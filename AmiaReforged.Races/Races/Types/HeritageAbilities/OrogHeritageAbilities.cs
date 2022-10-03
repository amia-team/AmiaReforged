using Anvil.API;
using NWN.Core;
using NWN.Core.NWNX;

namespace Amia.Racial.Races.Types.HeritageAbilities
{
    public class OrogHeritageAbilities : IHeritageAbilities
    {
        public void SetupStats(NwPlayer player)
        {
            CreaturePlugin.ModifyRawAbilityScore(player.LoginCreature, NWScript.ABILITY_STRENGTH, 1);
            CreaturePlugin.ModifyRawAbilityScore(player.LoginCreature, NWScript.ABILITY_CHARISMA, 1);
        }
    }
}
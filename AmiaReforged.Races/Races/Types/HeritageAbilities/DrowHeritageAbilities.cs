using Anvil.API;
using NWN.Core;
using NWN.Core.NWNX;

namespace Amia.Racial.Races.Types.HeritageAbilities
{
    public class DrowHeritageAbilities : IHeritageAbilities
    {
        public void SetupStats(NwPlayer player)
        {
            CreaturePlugin.ModifyRawAbilityScore(player.LoginCreature, NWScript.ABILITY_DEXTERITY, 2);
            CreaturePlugin.ModifyRawAbilityScore(player.LoginCreature, NWScript.ABILITY_CHARISMA, 2);
        }
    }
}
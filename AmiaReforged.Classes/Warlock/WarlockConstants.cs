using Anvil.API;
using NWN.Core;

namespace AmiaReforged.Classes.Warlock;

public static class WarlockConstants
{
    public const int WarlockClassId = 57;
    public static readonly NwClass? WarlockClass = NwClass.FromClassId(WarlockClassId);
    public static int WarlockDc(NwCreature caster)
        => 10 + NWScript.GetLevelByClass(WarlockClassId) + caster.GetAbilityModifier(Ability.Charisma);
}

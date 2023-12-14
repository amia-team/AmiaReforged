using NWN.Core.NWNX;

namespace AmiaReforged.Classes.EffectUtils;

public static class WarlockSpells
{
    private const int Warlock = 57;

    public static void ResetWarlockInvocations(uint caster)
    {
        const int firstCircle = 1;
        CreaturePlugin.SetRemainingSpellSlots(caster, Warlock, firstCircle,
            CreaturePlugin.GetMaxSpellSlots(caster, Warlock, firstCircle));
        const int secondCircle = 2;
        CreaturePlugin.SetRemainingSpellSlots(caster, Warlock, secondCircle,
            CreaturePlugin.GetMaxSpellSlots(caster, Warlock, secondCircle));
        const int thirdCircle = 3;
        CreaturePlugin.SetRemainingSpellSlots(caster, Warlock, thirdCircle,
            CreaturePlugin.GetMaxSpellSlots(caster, Warlock, thirdCircle));
        const int fourthCircle = 4;
        CreaturePlugin.SetRemainingSpellSlots(caster, Warlock, fourthCircle,
            CreaturePlugin.GetMaxSpellSlots(caster, Warlock, fourthCircle));
    }
}
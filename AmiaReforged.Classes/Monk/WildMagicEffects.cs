using Anvil.API;

namespace AmiaReforged.Classes.Monk;

public class WildMagicEffects
{
    public static void DoWildMagic(NwCreature monk, NwCreature targetCreature, int monkLevel, int dc, int wildMagicTier)
    {
        int d100Roll = Random.Shared.Roll(100);

        NwSpell randomSpell = (d100Roll switch
        {
            >= 1 and < 10 => Spell.ColorSpray,
            >= 10 and < 20 => Spell.MagicMissile,
            >= 20 and < 30 => Spell.Grease,
            >= 30 and < 40 => Spell.Balagarnsironhorn,
            >= 40 and < 50 => Spell.Combust,
            >= 50 and < 60 => Spell.TashasHideousLaughter,
            >= 60 and < 70 => Spell.GhoulTouch,
            >= 70 and < 80 => Spell.Web,

            // More rare wild magic
            >= 80 and < 85 => Spell.IsaacsLesserMissileStorm,
            >= 85 and < 90 => Spell.BigbysForcefulHand,
            >= 90 and < 95 => Spell.PrismaticSpray,
            >= 95 and < 100 => Spell.TimeStop,
            100 => Spell.EpicHellball
        })!;

        monk.ActionCastSpellAt(randomSpell, targetCreature.Location!, MetaMagic.None, true, 
            ProjectilePathType.Default, true, ClassType.Monk);
    }
}
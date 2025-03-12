using Anvil.API;

namespace AmiaReforged.Classes.Monk;

public class WildMagicEffects
{
    public static void DoWildMagic(NwCreature monk, NwCreature targetCreature, int monkLevel, int dc)
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
        
        // DON'T DELETE COMMENTED OUT CODE, it's another method for creating spell effects
        
        /*// Defenses are checked in order spell level absorption (mantles), spell immunity, then spell resistance.
        bool spellAbsorbed = 
            monk.SpellAbsorptionLimitedCheck(targetCreature, randomSpell, randomSpell.SpellSchool, randomSpell.InnateSpellLevel);
        if (spellAbsorbed) return;
        
        bool spellImmune = monk.SpellImmunityCheck(targetCreature, randomSpell);
        if (spellImmune) return;

        bool spellResisted = monk.SpellResistanceCheck(targetCreature, randomSpell, monkLevel);
        if (spellResisted) return;

        switch (randomSpell.SpellType)
        {
            case Spell.ColorSpray : DoColorSpray();
                break;
            case Spell.MagicMissile : DoMagicMissile();
                break;
            case Spell.Grease : DoGrease();
                break;
            case Spell.Balagarnsironhorn : DoBalagarnsIronHorn();
                break;
            case Spell.Combust : DoCombust();
                break;
            case Spell.TashasHideousLaughter : DoTashasHideousLaughter();
                break;
            case Spell.GhoulTouch : DoGhoulTouch();
                break;
            // AND SO ON...
        }
        
        void DoColorSpray()
        {
            throw new NotImplementedException();
        }
        
        void DoMagicMissile()
        {
            throw new NotImplementedException();
        }
        
        void DoGrease()
        {
            throw new NotImplementedException();
        }
        
        void DoBalagarnsIronHorn()
        {
            throw new NotImplementedException();
        }
       
        void DoCombust()
        {
            throw new NotImplementedException();
        }

        
        void DoTashasHideousLaughter()
        {
            throw new NotImplementedException();
        }

        void DoGhoulTouch()
        {
            throw new NotImplementedException();
        }
        */

        
    }
}
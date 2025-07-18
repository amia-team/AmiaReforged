using AmiaReforged.Classes.Monk.Types;
using Anvil.API;

namespace AmiaReforged.Classes.Monk;

public static class WildMagicEffects
{
    public static void DoWildMagic(NwCreature monk, NwCreature targetCreature)
    {
        int spellWeakChance, spellModerateChance, spellStrongChance;

        const int spellEpicChance = 1;
        
        switch (MonkUtils.GetKiFocus(monk))
        {
            default:
                spellWeakChance = 66;
                spellModerateChance = 33;
                spellStrongChance = 0;
                break;
            case KiFocus.KiFocus1 :
                spellWeakChance = 57;
                spellModerateChance = 33;
                spellStrongChance = 9;
                break;
            case KiFocus.KiFocus2:
                spellWeakChance = 44;
                spellModerateChance = 40;
                spellStrongChance = 15;
                break;
            case KiFocus.KiFocus3:
                spellWeakChance = 33;
                spellModerateChance = 33;
                spellStrongChance = 33;
                break;
        }
        
        // sanity check that all adds up to 100
        if (spellWeakChance + spellModerateChance + spellStrongChance + spellEpicChance != 100)
        {
            if (monk.IsPlayerControlled(out NwPlayer? player))
                player.SendServerMessage("DEBUG: All wild magic chances don't add up to 100!");
        }

        int d100Roll = Random.Shared.Roll(100);

        Random random = new();
        
        Spell randomSpell = (Spell)random.Next(GetRandomSpellList().Count);

        monk.ActionCastSpellAt(randomSpell!, targetCreature.Location!, MetaMagic.None, true, 
            ProjectilePathType.Default, true, ClassType.Monk);
        
        return;

        List<Spell> GetRandomSpellList()
        {
            if (d100Roll >= spellWeakChance && d100Roll < spellModerateChance)
                return WeakSpellList;
            if (d100Roll >= spellModerateChance && d100Roll < spellStrongChance)
                return ModerateSpellList;
            if (d100Roll >= spellStrongChance && d100Roll < spellEpicChance)
                return StrongSpellList;
            if (d100Roll == spellEpicChance)
                return EpicSpellList;

            return null!;
        }
    }
    
    private static readonly List<Spell> WeakSpellList = new()
    {
        Spell.Flare,
        Spell.Bane,
        Spell.GhoulTouch,
        Spell.Quillfire,
        Spell.MagicMissile,
        Spell.InfestationOfMaggots,
        Spell.Poison,
        Spell.Enervation
    };
    
    private static readonly List<Spell> ModerateSpellList = new()
    {
        Spell.Doom,
        Spell.ColorSpray,
        Spell.RayOfEnfeeblement,
        Spell.TashasHideousLaughter,
        Spell.GedleesElectricLoop,
        Spell.GustOfWind,
        Spell.MindFog,
        Spell.EnergyDrain,
        Spell.HealingSting
    };
    
    private static readonly List<Spell> StrongSpellList = new()
    {
        Spell.Web,
        Spell.Grease,
        Spell.Balagarnsironhorn,
        Spell.HoldMonster,
        Spell.NegativeEnergyBurst,
        Spell.Slow,
        Spell.BlindnessAndDeafness,
        Spell.PrismaticSpray,
        Spell.PowerWordStun,
        Spell.HammerOfTheGods,
        Spell.VampiricTouch
    };
    
    private static readonly List<Spell> EpicSpellList = new()
    {
        Spell.TimeStop,
        Spell.MeteorSwarm,
        Spell.GreatThunderclap
    };
}
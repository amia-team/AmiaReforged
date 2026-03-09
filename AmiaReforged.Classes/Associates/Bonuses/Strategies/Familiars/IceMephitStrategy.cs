using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Associates.Bonuses.Strategies.Familiars;

[ServiceBinding(typeof(IFamiliarBonusStrategy))]
public class IceMephitStrategy : IFamiliarBonusStrategy
{
    public string ResRefPrefix => "nw_fm_ice";

    public void Apply(NwCreature owner, NwCreature associate)
    {
        int baseAc = 10 + associate.Level / 2;
        associate.BaseAC = (sbyte)baseAc;

        NwFeat? uncanny = Feat.UncannyDodge1;
        if (uncanny != null)
            associate.AddFeat(uncanny);

        if (associate.Level >= 12)
        {
            byte casterLevel = (byte)associate.Level;
            if (associate.Level > 15)
            {
                casterLevel = 15;
            }

            SpecialAbility melfs = new SpecialAbility(NwSpell.FromSpellType(Spell.MelfsAcidArrow)!, casterLevel);
            associate.AddSpecialAbility(melfs);
        }

        if (associate.Level >= 21)
        {
            byte casterLevel = (byte)associate.Level;
            if (associate.Level > 15)
            {
                casterLevel = 15;
            }

            SpecialAbility auraOfCold = new SpecialAbility(NwSpell.FromSpellType(Spell.AbilityAuraCold)!, casterLevel);
            associate.AddSpecialAbility(auraOfCold);

            SpecialAbility slowBolt = new SpecialAbility(NwSpell.FromSpellType(Spell.AbilityBoltSlow)!, casterLevel);
            associate.AddSpecialAbility(slowBolt);
        }
    }
}

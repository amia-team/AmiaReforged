using Anvil.API;
using Anvil.Services;
using NWN.Core;
using NWN.Core.NWNX;
using SpecialAbility = Anvil.API.SpecialAbility;

namespace AmiaReforged.Classes.Associates.Bonuses.Strategies.Familiars;

[ServiceBinding(typeof(IFamiliarBonusStrategy))]
public class RavenFamiliarBonuses : IFamiliarBonusStrategy
{
    public string ResRefPrefix => "nw_fm_rave";

    public void Apply(NwCreature owner, NwCreature associate)
    {
        switch (associate.Level)
        {
            case >= 5 and < 10:
                NwSpell? fromSpellType = NwSpell.FromSpellType(Spell.AbilityHowlFear);
                if(fromSpellType is null) return;

                SpecialAbility specialAbility = new SpecialAbility(fromSpellType, (byte)associate.Level);

                associate.AddSpecialAbility(specialAbility);
                break;
        }
    }
}

using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Associates.Bonuses.Strategies.Familiars;

[ServiceBinding(typeof(IFamiliarBonusStrategy))]
public class FireMephitStrategy : IFamiliarBonusStrategy
{
    public string ResRefPrefix => "nw_fm_fire";

    public void Apply(NwCreature owner, NwCreature associate)
    {
        int baseAc = 10 + associate.Level / 2;
        associate.BaseAC = (sbyte)baseAc;

        NwFeat? uncanny = Feat.UncannyDodge1;
        if (uncanny != null)
            associate.AddFeat(uncanny);
    }
}

using Anvil.API;
using Anvil.API.Events;
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
        associate.OnCombatRoundEnd += RechargeRandomUsedAbility;
        switch (associate.Level)
        {
            case >= 5 and < 10:
                NwSpell? fromSpellType = NwSpell.FromSpellType(Spell.AbilityHowlFear);
                if (fromSpellType is null) return;

                SpecialAbility specialAbility = new SpecialAbility(fromSpellType, (byte)associate.Level);

                associate.AddSpecialAbility(specialAbility);
                break;
        }
    }

    private void RechargeRandomUsedAbility(CreatureEvents.OnCombatRoundEnd obj)
    {
        NwCreature creature = obj.Creature;
        if (creature != obj.Creature) return;

        List<SpecialAbility> usedAbilities = creature.SpecialAbilities.Where(ab => !ab.Ready).ToList();
        if (usedAbilities.Count == 0) return;

        SpecialAbility abilityToRecharge = usedAbilities[Random.Shared.Next(usedAbilities.Count)];

        Effect vfx = Effect.VisualEffect(VfxType.ImpPdkFinalStand);
        creature.ApplyEffect(EffectDuration.Instant, vfx);

        abilityToRecharge.Ready = true;
    }
}

using Anvil.API;
using AmiaReforged.Classes.Spells;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.DivineChampion;

[ServiceBinding(typeof(ISpell))]
public class DivineWrath : ISpell
{
    private const string DivineWrathCdTag = "divine_wrath_cd";
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "x2_s2_DivWrath";

    private record DivineWrathBonuses
    (
        DamagePower DamagePower,
        int DamageReduction,
        int AttackBonus,
        DamageBonus DamageBonus,
        int UniversalSave
    );

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature creature) return;

        Effect? divineWrathCd = creature.ActiveEffects.FirstOrDefault(effect => effect.Tag == DivineWrathCdTag);

        if (divineWrathCd != null)
        {
            if (creature.IsPlayerControlled(out NwPlayer? player))
                SpellUtils.SendRemainingCoolDown(player, eventData.Spell.Name.ToString(), divineWrathCd.DurationRemaining);

            return;
        }

        int divineChampionLevel = creature.GetClassInfo(ClassType.DivineChampion)?.Level ?? 0;

        Effect divineWrath = DivineWrathEffect(divineChampionLevel);

        int chaMod = creature.GetAbilityModifier(Ability.Charisma);
        int durationBonus = chaMod < 2 ? 0 : chaMod / 2;

        int divineWrathRounds = divineChampionLevel == 20 ? 10 + durationBonus + 5 : 10 + durationBonus;

        creature.ApplyEffect(EffectDuration.Temporary, divineWrath, NwTimeSpan.FromRounds(divineWrathRounds));
        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpGoodHelp));

        divineWrathCd = Effect.VisualEffect(VfxType.None);
        divineWrathCd.Tag = DivineWrathCdTag;
        divineWrathCd.SubType = EffectSubType.Extraordinary;

        creature.ApplyEffect(EffectDuration.Temporary, divineWrathCd, NwTimeSpan.FromTurns(5));
    }

    private static Effect DivineWrathEffect(int divineChampionLevel)
    {
        DivineWrathBonuses divineWrathBonuses = divineChampionLevel switch
        {
            >= 5 and < 10 => new DivineWrathBonuses(
                DamagePower: DamagePower.Plus2,
                DamageReduction: 5,
                AttackBonus: 3,
                DamageBonus: DamageBonus.Plus3,
                UniversalSave: 3),
            >= 10 and < 15 => new DivineWrathBonuses(
                DamagePower: DamagePower.Plus3,
                DamageReduction: 10,
                AttackBonus: 5,
                DamageBonus: DamageBonus.Plus5,
                UniversalSave: 5),
            >= 15 and < 19 => new DivineWrathBonuses(
                DamagePower: DamagePower.Plus4,
                DamageReduction: 10,
                AttackBonus: 7,
                DamageBonus: DamageBonus.Plus7,
                UniversalSave: 7),
            19 => new DivineWrathBonuses(
                DamagePower: DamagePower.Plus5,
                DamageReduction: 15,
                AttackBonus: 7,
                DamageBonus: DamageBonus.Plus7,
                UniversalSave: 7),
            >= 20 => new DivineWrathBonuses(
                DamagePower: DamagePower.Energy,
                DamageReduction: 20,
                AttackBonus: 9,
                DamageBonus: DamageBonus.Plus10,
                UniversalSave: 9),
            _ => new DivineWrathBonuses(
                DamagePower: DamagePower.Normal,
                DamageReduction: 0,
                AttackBonus: 0,
                DamageBonus: 0,
                UniversalSave: 0)
        };

        Effect divineWrath = Effect.LinkEffects(
            Effect.DamageReduction(divineWrathBonuses.DamageReduction, divineWrathBonuses.DamagePower),
            Effect.AttackIncrease(divineWrathBonuses.AttackBonus),
            Effect.DamageIncrease(divineWrathBonuses.DamageBonus, DamageType.Divine),
            Effect.SavingThrowIncrease(SavingThrow.All, divineWrathBonuses.UniversalSave),
            Effect.VisualEffect(VfxType.DurCessatePositive));

        divineWrath.SubType = EffectSubType.Extraordinary;
        return divineWrath;
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}

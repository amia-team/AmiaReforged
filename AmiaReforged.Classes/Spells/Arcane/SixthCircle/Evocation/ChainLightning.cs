using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.SixthCircle.Evocation;


[ServiceBinding(typeof(ISpell))]
public class ChainLightning(ShifterDcService shifterDcService) : ISpell
{
    public string ImpactScript => "NW_S0_ChLightn";

    private readonly Dictionary<uint, int> _spellHitsRemaining = [];
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;

        int arcs =
            caster.KnowsFeat(Feat.EpicSpellFocusEvocation!) ? 6 :
            caster.KnowsFeat(Feat.GreaterSpellFocusEvocation!) ? 5 :
            caster.KnowsFeat(Feat.SpellFocusEvocation!) ? 4 :
            3;

        int casterLevel = shifterDcService.GetShifterCasterLevel(caster, caster.CasterLevel);
        int spellDc = shifterDcService.GetShifterDc(caster, eventData.SaveDC);
        uint spellKey = caster.ObjectId;
        _spellHitsRemaining[spellKey] = casterLevel;

        int damageDice = casterLevel > 20 ? 20 : casterLevel;
        MetaMagic metaMagic = eventData.MetaMagicFeat;

        Location? spellLocation = eventData.TargetObject?.Location;
        if (spellLocation == null) return;

        foreach (NwCreature hostileCreature in spellLocation
                     .GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Colossal,true)
                     .Where(caster.IsReactionTypeHostile))
        {
            if (arcs == 0 || _spellHitsRemaining[spellKey] <= 0) break;
            if (hostileCreature.IsDead) continue;

            _ = ShootArc(caster, hostileCreature, damageDice, spellDc, metaMagic, spellKey, eventData.Spell, casterLevel);

            arcs--;
            _spellHitsRemaining[spellKey]--;
        }
    }

    private async Task ShootArc(NwCreature caster, NwCreature hostileCreature, int damageDice, int spellDc,
        MetaMagic metaMagic, uint spellKey, NwSpell spell, int casterLevel)
    {
        CreatureEvents.OnSpellCastAt.Signal(caster, hostileCreature, spell);

        hostileCreature.ApplyEffect(EffectDuration.Temporary,
            Effect.Beam(VfxType.BeamLightning, caster, BodyNode.Hand),
            TimeSpan.FromSeconds(0.5));

        await NwTask.Delay(TimeSpan.FromSeconds(0.25f));
        await caster.WaitForObjectContext();

        if (hostileCreature.IsDead || caster.SpellResistanceCheck(hostileCreature, spell, casterLevel) ||
            hostileCreature.Location == null) return;

        RollDamage(hostileCreature, caster, damageDice, spellDc, metaMagic);

        await NwTask.Delay(TimeSpan.FromSeconds(0.25f));
        await caster.WaitForObjectContext();

        damageDice /= 2;

        foreach (NwCreature secondaryCreature in hostileCreature.Location
                     .GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Large,true)
                     .Where(caster.IsReactionTypeHostile))
        {
            if (_spellHitsRemaining[spellKey] <= 0) break;

            if (secondaryCreature.IsDead || secondaryCreature == hostileCreature ||
                caster.SpellResistanceCheck(secondaryCreature, spell, casterLevel)) continue;

            CreatureEvents.OnSpellCastAt.Signal(caster, secondaryCreature, spell);

            if (hostileCreature is { IsValid: true, IsDead: false })
            {
                secondaryCreature.ApplyEffect(EffectDuration.Temporary,
                    Effect.Beam(VfxType.BeamLightning, hostileCreature, BodyNode.Chest),
                    TimeSpan.FromSeconds(0.5));
            }

            RollDamage(secondaryCreature, caster, damageDice, spellDc, metaMagic);

            _spellHitsRemaining[spellKey]--;
        }
    }

    private static void RollDamage(NwCreature targetCreature, NwCreature caster, int damageDice, int spellDc,
        MetaMagic metaMagic)
    {
        int damage = SpellUtils.MaximizeSpell(metaMagic, 6, damageDice);
        damage = SpellUtils.EmpowerSpell(metaMagic, damage);

        SavingThrowResult savingThrow =
            targetCreature.RollSavingThrow(SavingThrow.Reflex, spellDc, SavingThrowType.Electricity, caster);

        bool hasEvasion = targetCreature.KnowsFeat(NwFeat.FromFeatType(Feat.Evasion)!);
        bool hasImprovedEvasion = targetCreature.KnowsFeat(NwFeat.FromFeatType(Feat.ImprovedEvasion)!);

        if ((hasEvasion || hasImprovedEvasion) && savingThrow == SavingThrowResult.Success)
        {
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));
            return;
        }

        if (hasImprovedEvasion || savingThrow == SavingThrowResult.Success)
            damage /= 2;

        targetCreature.ApplyEffect(EffectDuration.Instant, Effect.Damage(damage, DamageType.Electrical));
        targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpLightningS));
    }

    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public void SetSpellResisted(bool result) { }
}

using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Divine.EighthCircle.Evocation;

[ServiceBinding(typeof(ISpell))]
public class Earthquake : ISpell
{
    private const VfxType EarthquakeDurVfx = (VfxType)356;
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "X0_S0_Earthquake";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;
        if (caster.Location is not { } location) return;

        NwFeat? evocationFocus = GetHighestEvocationFocus(caster);

        int damageDiceCap = evocationFocus?.FeatType switch
        {
            Feat.EpicSpellFocusEvocation    => 25,
            Feat.GreaterSpellFocusEvocation => 23,
            Feat.SpellFocusEvocation        => 21,
            _                               => 20
        };

        int damageDice = Math.Min(caster.CasterLevel, damageDiceCap);
        int dc = SpellUtils.GetSpellDc(eventData);

        caster.ApplyEffect(EffectDuration.Temporary, Effect.VisualEffect(EarthquakeDurVfx), NwTimeSpan.FromRounds(1));

        foreach (NwGameObject obj in location.GetObjectsInShape(Shape.Sphere, RadiusSize.Colossal, true,
                     ObjectTypes.Creature | ObjectTypes.Door | ObjectTypes.Placeable))
        {
            int damage = Random.Shared.Roll(8, damageDice);
            damage = SpellUtils.EmpowerSpell(eventData.MetaMagicFeat, damage);

             _ = ApplyEarthquake(obj, caster, damage, dc, location, eventData.Spell);
        }
    }

    private async Task ApplyEarthquake(NwGameObject obj, NwGameObject caster, int damage, int dc, Location location,
        NwSpell spell)
    {
        float? delay = obj.Location?.Distance(location) / 20;

        if (delay != null)
            await NwTask.Delay(TimeSpan.FromSeconds(delay.Value));

        await caster.WaitForObjectContext();

        if (obj is not NwCreature creature)
        {
            obj.ApplyEffect(EffectDuration.Instant, Effect.Damage(damage, DamageType.Bludgeoning));
            return;
        }

        CreatureEvents.OnSpellCastAt.Signal(caster, creature, spell);

        SavingThrowResult savingThrowResult =
            creature.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.All, caster);

        if (creature.Feats.Any(feat => feat.FeatType == Feat.ImprovedEvasion)
            || savingThrowResult == SavingThrowResult.Success)
            damage /= 2;

        Effect earthquakeDamage = Effect.LinkEffects(Effect.Damage(damage, DamageType.Bludgeoning, DamagePower.Plus5));

        switch (savingThrowResult)
        {
            case SavingThrowResult.Failure:
                creature.ApplyEffect(EffectDuration.Instant, earthquakeDamage);
                creature.ApplyEffect(EffectDuration.Temporary, Effect.Knockdown(), NwTimeSpan.FromRounds(1));
                break;
            case SavingThrowResult.Success:
                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));
                if (creature.Feats.Any(feat => feat.FeatType is Feat.ImprovedEvasion or Feat.Evasion)) return;
                creature.ApplyEffect(EffectDuration.Instant, earthquakeDamage);
                break;
            case SavingThrowResult.Immune:
                break;
        }
    }

    private NwFeat? GetHighestEvocationFocus(NwCreature caster)
    {
        int[] evocationFocusFeats =
        [
            (int)Feat.EpicSpellFocusEvocation,
            (int)Feat.GreaterSpellFocusEvocation,
            (int)Feat.SpellFocusEvocation
        ];

        return caster.Feats
            .Where(feat => evocationFocusFeats.Contains(feat.Id))
            .MaxBy(feat => feat.Id);
    }

    public void SetSpellResisted(bool result) { }
}

using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Arcane.FirstCircle.Transmutation;

/// <summary>
/// A thin sheet of searing flame shoots from your outspread fingertips.
/// Any creature in the area of the flames suffers 1d4 points of fire damage
/// per 2 caster levels (minimum 1d4).
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class BurningHands : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_BurnHand";

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;

        uint casterObjectId = caster.ObjectId;
        IntPtr targetLocation = GetSpellTargetLocation();

        int casterLevel = caster.CasterLevel;
        int spellDc = SpellUtils.GetSpellDc(eventData);
        MetaMagic metaMagic = eventData.MetaMagicFeat;

        // Calculate damage dice: 1d4 per 2 caster levels, minimum 1d4
        int damageDice = Math.Max(1, casterLevel / 2);

        const int validObjectTypes = OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR | OBJECT_TYPE_PLACEABLE;
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPELLCONE, 10.0f, targetLocation, TRUE, validObjectTypes);

        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            // Skip the caster
            if (currentTarget == casterObjectId)
            {
                currentTarget = GetNextObjectInShape(SHAPE_SPELLCONE, 10.0f, targetLocation, TRUE, validObjectTypes);
                continue;
            }

            // Handle doors and placeables - just apply damage
            if (GetObjectType(currentTarget) == OBJECT_TYPE_DOOR ||
                GetObjectType(currentTarget) == OBJECT_TYPE_PLACEABLE)
            {
                int placeableDamage = CalculateDamage(damageDice, metaMagic);
                float placeableDelay = GetDistanceBetween(casterObjectId, currentTarget) / 20.0f;
                uint placeableTarget = currentTarget; // Capture for closure

                DelayCommand(placeableDelay, () =>
                {
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(placeableDamage, DAMAGE_TYPE_FIRE), placeableTarget);
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_FLAME_S), placeableTarget);
                });

                currentTarget = GetNextObjectInShape(SHAPE_SPELLCONE, 10.0f, targetLocation, TRUE, validObjectTypes);
                continue;
            }

            // Check if valid hostile target
            if (NwEffects.IsValidSpellTarget(currentTarget, 2, casterObjectId))
            {
                SignalEvent(currentTarget, EventSpellCastAt(casterObjectId, SPELL_BURNING_HANDS));

                float delay = GetDistanceBetween(casterObjectId, currentTarget) / 20.0f;

                // Check spell resistance
                if (!NwEffects.ResistSpell(casterObjectId, currentTarget))
                {
                    int damage = CalculateDamage(damageDice, metaMagic);

                    // Roll reflex save and handle evasion
                    bool passedSave = ReflexSave(currentTarget, spellDc, SAVING_THROW_TYPE_FIRE, casterObjectId) == TRUE;
                    bool hasEvasion = GetHasFeat(FEAT_EVASION, currentTarget) == TRUE;
                    bool hasImprovedEvasion = GetHasFeat(FEAT_IMPROVED_EVASION, currentTarget) == TRUE;

                    if (passedSave)
                    {
                        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_REFLEX_SAVE_THROW_USE), currentTarget);

                        // Evasion: no damage on successful save
                        if (hasEvasion || hasImprovedEvasion)
                        {
                            currentTarget = GetNextObjectInShape(SHAPE_SPELLCONE, 10.0f, targetLocation, TRUE, validObjectTypes);
                            continue;
                        }

                        // Half damage on successful save
                        damage /= 2;
                    }
                    else if (hasImprovedEvasion)
                    {
                        // Improved Evasion: half damage on failed save
                        damage /= 2;
                    }

                    if (damage > 0)
                    {
                        uint target = currentTarget; // Capture for closure
                        DelayCommand(delay, () =>
                        {
                            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_FIRE), target);
                            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_FLAME_S), target);
                        });
                    }
                }
            }

            currentTarget = GetNextObjectInShape(SHAPE_SPELLCONE, 10.0f, targetLocation, TRUE, validObjectTypes);
        }
    }

    private static int CalculateDamage(int damageDice, MetaMagic metaMagic)
    {
        int damage = SpellUtils.MaximizeSpell(metaMagic, 4, damageDice);
        damage = SpellUtils.EmpowerSpell(metaMagic, damage);
        return damage;
    }
}


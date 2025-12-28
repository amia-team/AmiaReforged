using Anvil.API;
using Anvil.API.Events;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Arcane.FirstCircle.Transmutation;

/// <summary>
/// Decorator for Burning Hands that applies Transmutation focus bonuses:
/// - 5% bonus fire damage per spell focus rank to greased targets
/// - Epic Spell Focus causes greased targets to combust for 4 rounds (2d6 fire damage per round)
/// </summary>
[DecoratesSpell(typeof(BurningHands))]
public class BurningHandsTransmutationFocus : SpellDecorator
{
    private const string GreaseFireVulnTag = "GreaseFireVuln";
    private const string CombustionTag = "BurningHandsCombustion";
    private const int CombustionDurationRounds = 4;

    public BurningHandsTransmutationFocus(ISpell spell) : base(spell)
    {
        Spell = spell;
    }

    public override void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        // Run the base spell first
        Spell.OnSpellImpact(eventData);

        if (eventData.Caster is not NwCreature caster) return;

        // Check for Transmutation spell focus feats
        bool hasBasicFocus = caster.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusTransmutation);
        bool hasGreaterFocus = caster.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusTransmutation);
        bool hasEpicFocus = caster.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusTransmutation);

        // If no transmutation focus, nothing to do
        if (!hasBasicFocus && !hasGreaterFocus && !hasEpicFocus) return;

        // Calculate focus rank (1 for basic, 2 for greater, 3 for epic)
        int focusRank = (hasBasicFocus ? 1 : 0) + (hasGreaterFocus ? 1 : 0) + (hasEpicFocus ? 1 : 0);

        uint casterObjectId = caster.ObjectId;
        IntPtr targetLocation = GetSpellTargetLocation();
        MetaMagic metaMagic = eventData.MetaMagicFeat;
        int casterLevel = caster.CasterLevel;
        int damageDice = Math.Max(1, casterLevel / 2);

        const int validObjectTypes = OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR | OBJECT_TYPE_PLACEABLE;
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPELLCONE, 10.0f, targetLocation, TRUE, validObjectTypes);

        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            // Skip caster and non-creatures
            if (currentTarget == casterObjectId || GetObjectType(currentTarget) != OBJECT_TYPE_CREATURE)
            {
                currentTarget = GetNextObjectInShape(SHAPE_SPELLCONE, 10.0f, targetLocation, TRUE, validObjectTypes);
                continue;
            }

            NwCreature? targetCreature = currentTarget.ToNwObject<NwCreature>();
            if (targetCreature == null)
            {
                currentTarget = GetNextObjectInShape(SHAPE_SPELLCONE, 10.0f, targetLocation, TRUE, validObjectTypes);
                continue;
            }

            // Check if target is affected by grease (has the GreaseFireVuln effect)
            bool isGreased = targetCreature.ActiveEffects.Any(e => e.Tag == GreaseFireVulnTag);

            if (isGreased)
            {
                float delay = GetDistanceBetween(casterObjectId, currentTarget) / 20.0f;

                // Calculate bonus damage: 5% per focus rank
                int baseDamage = CalculateDamage(damageDice, metaMagic);
                int bonusDamage = (int)(baseDamage * (focusRank * 0.05));

                if (bonusDamage > 0)
                {
                    uint target = currentTarget; // Capture for closure
                    DelayCommand(delay,
                        () =>
                        {
                            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(bonusDamage, DAMAGE_TYPE_FIRE),
                                target);
                        });
                }

                // Epic Spell Focus: Apply combustion effect
                if (hasEpicFocus)
                {
                    ApplyCombustion(targetCreature, focusRank, metaMagic);
                }
            }

            currentTarget = GetNextObjectInShape(SHAPE_SPELLCONE, 10.0f, targetLocation, TRUE, validObjectTypes);
        }
    }

    private void ApplyCombustion(NwCreature target, int focusRank, MetaMagic metaMagic)
    {
        // Remove existing combustion effect if present
        Effect? existingCombustion = target.ActiveEffects.FirstOrDefault(e => e.Tag == CombustionTag);
        if (existingCombustion != null)
        {
            target.RemoveEffect(existingCombustion);
        }

        // Apply visual effect for combustion
        Effect combustionVfx = Effect.VisualEffect(VfxType.DurAuraFire);
        combustionVfx.Tag = CombustionTag;
        target.ApplyEffect(EffectDuration.Temporary, combustionVfx, TimeSpan.FromSeconds(CombustionDurationRounds * 6));

        // Schedule damage ticks for each round
        for (int round = 1; round <= CombustionDurationRounds; round++)
        {
            float tickDelay = round * 6.0f; // 6 seconds per round

            uint targetId = target.ObjectId;

            DelayCommand(tickDelay, () => { ApplyCombustionDamage(targetId, focusRank, metaMagic); });
        }
    }

    private static void ApplyCombustionDamage(uint targetId, int focusRank, MetaMagic metaMagic)
    {
        // Validate target is still valid
        if (GetIsObjectValid(targetId) != TRUE) return;
        if (GetIsDead(targetId) == TRUE) return;

        NwCreature? target = targetId.ToNwObject<NwCreature>();
        if (target == null) return;

        // Check if target still has combustion effect
        if (target.ActiveEffects.All(e => e.Tag != CombustionTag)) return;

        // Check if target is still greased for the bonus damage
        bool isStillGreased = target.ActiveEffects.Any(e => e.Tag == GreaseFireVulnTag);

        // Calculate combustion damage: 2d6 fire
        int combustionDamage = metaMagic == MetaMagic.Maximize ? 12 : d6(2);
        combustionDamage = SpellUtils.EmpowerSpell(metaMagic, combustionDamage);

        // Apply 5% bonus per focus rank if still greased
        if (isStillGreased)
        {
            int bonusDamage = (int)(combustionDamage * (focusRank * 0.05));
            combustionDamage += bonusDamage;
        }

        // Apply damage and visual
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(combustionDamage, DAMAGE_TYPE_FIRE), targetId);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_FLAME_S), targetId);
    }

    private static int CalculateDamage(int damageDice, MetaMagic metaMagic)
    {
        int damage = SpellUtils.MaximizeSpell(metaMagic, 4, damageDice);
        damage = SpellUtils.EmpowerSpell(metaMagic, damage);
        return damage;
    }
}

using AmiaReforged.Classes.Spells;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Palemaster;

/**
 * Supernatural Ability: Palemaster - Undead Graft: Deathless Mastery Touch
 *
 * This ability allows the Palemaster to instantly slay foes if they fail a Fortitude save.
 * DC: 10 + Palemaster Level + INT modifier
 *
 * Restrictions:
 * - Cannot be used on plot creatures
 * - Cannot be used on creatures larger than Large
 * - Cannot be used on constructs, oozes, or undead
 * - Cannot be used in areas with NoSpecialAbilities flag
 * - Requires successful melee touch attack
 *
 * Amia: On a successful save, targets take 40% of their max health as negative energy damage.
 * Undead are healed instead. Death immunity negates the effect. The "Deathtouch_Immune" local
 * variable on a creature also grants immunity.
 */
[ServiceBinding(typeof(ISpell))]
public class DeathlessMasterTouch(DeathSpellService deathSpellService) : ISpell
{
    private const string NoSpecialAbilitiesVar = "NoSpecialAbilities";
    private const string DeathtouchImmuneVar = "Deathtouch_Immune";
    private const int PaleMasterClassId = 24;

    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "x2_s2_dthmsttch";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;
        if (eventData.TargetObject is not NwCreature target) return;

        NwPlayer? player = caster.ControllingPlayer;

        // Check for NoSpecialAbilities zone
        if (caster.Area != null && NWScript.GetLocalInt(caster.Area, NoSpecialAbilitiesVar) == NWScript.TRUE)
        {
            player?.SendServerMessage("- You may not use this Special Ability in this area! -");
            return;
        }

        // Only affects hostile targets
        if (!caster.IsReactionTypeHostile(target))
        {
            player?.SendServerMessage("- You can only use this ability on hostile creatures! -");
            return;
        }

        // Check for Deathtouch_Immune local variable
        if (NWScript.GetLocalInt(target, DeathtouchImmuneVar) == NWScript.TRUE)
        {
            player?.SendServerMessage("- Target is immune! -");
            return;
        }

        // Can't target self
        if (target == caster)
        {
            player?.SendServerMessage("- Palemasters don't suicide! -");
            return;
        }

        // Signal the spell event
        SpellUtils.SignalSpell(caster, target, eventData.Spell);

        // Filter: Plot, Creature Size greater than Large, Non-living
        if (target.PlotFlag ||
            target.Race.RacialType == RacialType.Construct ||
            target.Race.RacialType == RacialType.Ooze ||
            target.Race.RacialType == RacialType.Undead)
        {
            player?.SendServerMessage("<c€þ>- Undead Graft: Deathless Mastery Touch won't affect this creature. -");
            return;
        }

        // Perform melee touch attack
        TouchAttackResult touchResult = caster.TouchAttackMelee(target);

        if (touchResult == TouchAttackResult.Miss)
        {
            // Refund the feat use on miss
            NWScript.IncrementRemainingFeatUses(caster, NWScript.FEAT_DEATHLESS_MASTER_TOUCH);
            return;
        }

        // Calculate DC: 10 + Palemaster Level + INT modifier
        int paleMasterLevel = GetPaleMasterLevel(caster);
        int intMod = caster.GetAbilityModifier(Ability.Intelligence);
        int dc = 10 + paleMasterLevel + intMod;

        Effect deathVfx = Effect.VisualEffect(VfxType.ImpDeath);

        // Make fortitude save
        SavingThrowResult saveResult = target.RollSavingThrow(
            SavingThrow.Fortitude,
            dc,
            SavingThrowType.Death,
            caster);

        if (saveResult != SavingThrowResult.Success)
        {
            // Failed save - instant death
            int overkillDamage = target.HP + 10; // +10 ensures death
            Effect deathDamage = Effect.Damage(overkillDamage, DamageType.Magical, DamagePower.Energy);

            target.ApplyEffect(EffectDuration.Instant, deathVfx);
            target.ApplyEffect(EffectDuration.Instant, deathDamage);
        }
        else
        {
            // Passed save - apply 2% max HP per Palemaster level as negative energy damage
            int damagePercent = paleMasterLevel * DeathSpellService.DeathlessMasterTouchPercentPerLevel;
            deathSpellService.ApplyFlatPercentDamage(caster, target, damagePercent, applyVfx: true);
        }
    }

    private static int GetPaleMasterLevel(NwCreature creature)
    {
        foreach (CreatureClassInfo classInfo in creature.Classes)
        {
            if (classInfo.Class.Id == PaleMasterClassId)
                return classInfo.Level;
        }
        return 0;
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}

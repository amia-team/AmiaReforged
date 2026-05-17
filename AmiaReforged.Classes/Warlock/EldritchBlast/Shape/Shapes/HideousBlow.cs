using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Spells;
using AmiaReforged.Classes.Warlock.EldritchBlast.Essence;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static AmiaReforged.Classes.Warlock.EldritchBlast.BlastMechanics;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Shape.Shapes;

[ServiceBinding(typeof(IShape))]
public class HideousBlow(CooldownService cooldownService) : IShape
{
    private const string HideousBlowTag = "hideous_blow";
    private const int CooldownRounds = 2;
    public ShapeType ShapeType => ShapeType.HideousBlow;

    public void CastEldritchShape(NwCreature warlock, int invocationCl, int invocationDc, EssenceData essence,
        SpellEvents.OnSpellCast eventData)
    {
        if (eventData.TargetObject is not { } target || eventData.Spell is not { } spell
            || cooldownService.IsOnCooldown(warlock, HideousBlowTag, broadcastCd: false)
            || !CheckSpellFail(warlock, spell, essence, target, invocationCl))
            return;

        (int FlatBonus, double Multiplier) damageModifiers = GetEldritchDamageModifiers(warlock, invocationCl);

        int damage = RollEldritchDamage(damageModifiers, invocationCl) / 2;

        target.ApplyEldritchBlast(warlock, damage, invocationDc, essence);

        cooldownService.ApplyCooldown(warlock, HideousBlowTag, duration: NwTimeSpan.FromRounds(CooldownRounds), broadcastCd: false);
    }

    /// <summary>
    /// Hideous Blow's HideousBlowHandler bypasses normal spell checks, so do it here instead
    /// </summary>
    /// <returns>True if pass, false if fail</returns>
    private bool CheckSpellFail(NwCreature warlock, NwSpell spell, EssenceData essence, NwGameObject target, int invocationCl)
    {
        // 1. Verbal component check & polymorph check (polymorphed characters can't cast normal spells)
        if (!warlock.ActiveEffects.Any(e => e.EffectType is EffectType.Silence or EffectType.Polymorph)
            // 2. ASF check
            && warlock.CheckArcaneSpellFailure(spell)
            // 3. Spell resist check
            && (essence.BypassSpellResistance || !warlock.InvocationResistCheck(target, invocationCl, isEldritchBlast: true)))
            return true;

        cooldownService.ApplyCooldown(warlock, HideousBlowTag, duration: NwTimeSpan.FromRounds(CooldownRounds), broadcastCd: false);

        return false;
    }
}

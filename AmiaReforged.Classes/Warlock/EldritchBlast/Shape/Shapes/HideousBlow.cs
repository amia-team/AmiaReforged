using AmiaReforged.Classes.Warlock.EldritchBlast.Essence;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using static AmiaReforged.Classes.Warlock.EldritchBlast.BlastMechanics;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Shape.Shapes;

[ServiceBinding(typeof(IShape))]
public class HideousBlow(ScriptHandleFactory scriptHandleFactory, EssenceFactory essenceFactory) : IShape
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const string HideousBlowTag = "hideous_blow";
    private const int CooldownRounds = 2;
    public ShapeType ShapeType => ShapeType.HideousBlow;

    public void CastEldritchShape(NwCreature warlock, int invocationCl, int invocationDc, EssenceData essence,
        SpellEvents.OnSpellCast eventData)
    {
        if (eventData.TargetObject == warlock)
        {
            NwItem? weapon = warlock.GetItemInSlot(InventorySlot.RightHand);
            if (weapon is null || weapon.IsRangedWeapon)
            {
                warlock.ControllingPlayer?.SendServerMessage("You must have a melee weapon equipped for Hideous Blow.");
                return;
            }

            ApplyHideousProperties(weapon, essence);
            warlock.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfPwstun));

            return;
        }

        if (eventData.TargetObject is not { } target
            || eventData.Spell is not { } spell
            || warlock.GetItemInSlot(InventorySlot.RightHand) is not { } equippedWeapon
            || equippedWeapon.IsRangedWeapon
            || equippedWeapon.ItemProperties.All(ip => ip.Tag != HideousBlowTag))
            return;

        Effect hideousCooldown = HideousCooldown(equippedWeapon, warlock);

        if (!CheckSpellFail(warlock, spell, essence, target, invocationCl))
        {
            warlock.ApplyEffect(EffectDuration.Temporary, hideousCooldown, NwTimeSpan.FromRounds(CooldownRounds));
            return;
        }

        (int FlatBonus, double Multiplier) damageModifiers = GetEldritchDamageModifiers(warlock, invocationCl);

        int damage = RollEldritchDamage(damageModifiers, invocationCl) / 2;

        target.ApplyEldritchBlast(warlock, damage, invocationDc, essence);
        warlock.ApplyEffect(EffectDuration.Temporary, hideousCooldown, NwTimeSpan.FromRounds(CooldownRounds));
    }

    /// <summary>
    /// Hideous Blow's HideousBlowHandler bypasses normal spell checks, so do it here instead
    /// </summary>
    /// <returns>True if pass, false if fail</returns>
    private static bool CheckSpellFail(NwCreature warlock, NwSpell spell, EssenceData essence, NwGameObject target,
        int invocationCl)
    {
        // 1. Verbal component check & polymorph check (polymorphed characters can't cast normal spells)
        if (!warlock.ActiveEffects.Any(e => e.EffectType is EffectType.Silence or EffectType.Polymorph)
            // 2. ASF check
            && warlock.CheckArcaneSpellFailure(spell)
            // 3. Spell resist check
            && (essence.BypassSpellResistance ||
                !warlock.InvocationResistCheck(target, invocationCl, isEldritchBlast: true)))
            return true;

        return false;
    }

    private Effect HideousCooldown(NwItem equippedItem, NwCreature warlock)
    {
        ScriptCallbackHandle onApply = scriptHandleFactory.CreateUniqueHandler(_ =>
        {
            RemoveHideousProperties(equippedItem);

            return ScriptHandleResult.Handled;
        });

        ScriptCallbackHandle onRemove = scriptHandleFactory.CreateUniqueHandler(_ =>
        {
            if (warlock.IsDead
                || !warlock.IsValid
                || warlock.GetItemInSlot(InventorySlot.RightHand) is not { } currentWeapon
                || currentWeapon.IsRangedWeapon)
                return ScriptHandleResult.Handled;

            EssenceData essence = essenceFactory.GetEssenceData(warlock, warlock.GetInvocationCasterLevel());
            ApplyHideousProperties(currentWeapon, essence);

            return ScriptHandleResult.Handled;
        });

        return Effect.RunAction(onApply, onRemove);
    }

    private static void ApplyHideousProperties(NwItem weapon, EssenceData essence)
    {
        ItemProperty hideousBlowIp = ItemProperty.OnHitCastSpell((IPCastSpell)141, 1);
        ItemProperty weaponVfx = ItemProperty.VisualEffect(essence.HideousBlowVfx);
        ItemProperty[] hideousProperties = [hideousBlowIp, weaponVfx];
        foreach (ItemProperty ip in hideousProperties)
        {
            ip.Tag = HideousBlowTag;
            weapon.AddItemProperty(ip, EffectDuration.Temporary, TimeSpan.FromHours(8));
        }
    }

    private static void RemoveHideousProperties(NwItem item)
    {
        foreach (ItemProperty ip in item.ItemProperties)
        {
            if (ip.Tag == HideousBlowTag)
                item.RemoveItemProperty(ip);
        }
    }
}

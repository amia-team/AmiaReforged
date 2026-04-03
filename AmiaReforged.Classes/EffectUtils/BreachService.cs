using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.EffectUtils;

/// <summary>
/// A public breach util class you can use; based on nw_i0_spells.nss
/// </summary>
[ServiceBinding(typeof(BreachService))]
public class BreachService(ScriptHandleFactory scriptHandleFactory)
{
    /// <summary>
    /// A spell and the effects that can be breached.
    /// </summary>
    /// <param name="SpellType">The spell type of the spell valid for breaching</param>
    /// <param name="Effects">The effects associated with that spell</param>
    private record BreachableSpell(Spell SpellType, Effect[] Effects);

    /// <summary>
    /// Information about a single successful breach to be reported back to the caster and target.
    /// </summary>
    /// <param name="Target">The target of the breach.</param>
    /// <param name="SpellName">The name of the breached spell.</param>
    private record BreachFeedbackEntry(NwGameObject Target, string SpellName);

    private readonly Dictionary<Guid, List<BreachFeedbackEntry>> _breachFeedbackCache = [];

    private const Spell GreaterSanctuary = (Spell)443;
    private const Spell MorgensElectrifier = (Spell)859;
    /// <summary>
    /// Spells that can be breached by the BreachMagic method.
    /// </summary>
    public readonly Spell[] BreachSpells =
    [
        Spell.GreaterSpellMantle,
        Spell.Premonition,
        Spell.ShadowShield,
        Spell.GreaterStoneskin,
        Spell.GlobeOfInvulnerability,
        Spell.EnergyBuffer,
        GreaterSanctuary,
        Spell.MinorGlobeOfInvulnerability,
        Spell.SpellResistance,
        Spell.Stoneskin,
        Spell.LesserSpellMantle,
        Spell.MestilsAcidSheath,
        Spell.MindBlank,
        Spell.ElementalShield,
        Spell.ProtectionFromSpells,
        Spell.ResistElements,
        MorgensElectrifier,
        Spell.DeathArmor,
        Spell.GhostlyVisage,
        Spell.ShadowShield,
        Spell.ShadowConjurationMageArmor,
        Spell.NegativeEnergyProtection,
        Spell.Sanctuary,
        Spell.MageArmor,
        Spell.StoneBones,
        Spell.Shield,
        Spell.ShieldOfFaith,
        Spell.LesserMindBlank,
        Spell.Ironguts,
        Spell.Resistance
    ];

    /// <summary>
    /// An effect that breaches defensive spells according to the BreachSpells. Doesn't include a spell resistance
    /// decrease. Apply the spell resistance decrease separately.
    /// </summary>
    /// <param name="breachAmount">The amount of defensive spells to breach, must be 1 or higher</param>
    /// <param name="caster">Caster of the breach effect</param>
    public Effect BreachMagic(int breachAmount, NwCreature caster)
    {
        if (breachAmount < 1)
            caster.ControllingPlayer?.SendServerMessage
            ("Breach amount must be 1 or higher for this spell. Please file a bug report on Amia Discord.");

        ScriptCallbackHandle onBreachApply = scriptHandleFactory.CreateUniqueHandler(info
            => OnBreachApply(info, breachAmount, caster));

        Effect breachMagic = Effect.RunAction(onAppliedHandle: onBreachApply);
        breachMagic.SubType = EffectSubType.Magical;

        return breachMagic;
    }

    private ScriptHandleResult OnBreachApply(CallInfo info, int breachAmount, NwCreature caster)
    {
        if (info.ObjectSelf is not NwGameObject target || breachAmount < 1)
            return ScriptHandleResult.Handled;

        if (!_breachFeedbackCache.TryGetValue(caster.UUID, out List<BreachFeedbackEntry>? breachFeedback))
        {
            breachFeedback = [];
            _breachFeedbackCache[caster.UUID] = breachFeedback;
        }

        List<BreachableSpell> breachableSpells = GetBreachableSpells(target);
        if (breachableSpells.Count == 0) return ScriptHandleResult.Handled;

        int remainingBreaches = breachAmount;
        foreach (BreachableSpell breachableSpell in breachableSpells)
        {
            if (remainingBreaches <= 0) break;

            foreach (Effect effect in breachableSpell.Effects)
                target.RemoveEffect(effect);

            string spellName = NwSpell.FromSpellType(breachableSpell.SpellType)?.Name.ToString() ?? "Unknown Spell";
            breachFeedback.Add(new BreachFeedbackEntry(target, spellName));

            remainingBreaches--;
        }

        _breachFeedbackCache[caster.UUID] = breachFeedback;

        return ScriptHandleResult.Handled;
    }

    /// <summary>
    /// Sends a summary of breached spells to the caster and the target(s).
    /// This must be called only after the breach magic effect has been applied to all desired game objects.
    /// A delay should not be used with this, unless you apply the breach magic effects with a delay.
    /// This also flushes the breach feedback cache, so use this at the end of every breach magic spell script.
    /// </summary>
    /// <param name="caster">The caster of the dispel magic.</param>
    /// <returns>The total number of spells breached (if you want that count for something).</returns>
    public int FlushBreachFeedback(NwCreature caster)
    {
        int breachCount = 0;
        if (!_breachFeedbackCache.Remove(caster.UUID, out List<BreachFeedbackEntry>? breachFeedback))
            return breachCount;

        breachCount = breachFeedback.Count;
        if (breachFeedback.Count == 0) return breachCount;

        List<BreachFeedbackEntry[]> feedbackByTarget = breachFeedback
            .GroupBy(entry => entry.Target)
            .Select(entry => entry.ToArray())
            .ToList();

        if (caster.IsPlayerControlled(out NwPlayer? casterPlayer))
        {
            string casterMessage = $"{caster.Name} breached magic from:".ColorString(ColorConstants.Magenta);

            foreach (BreachFeedbackEntry[] breachFeedbackEntries in feedbackByTarget)
            {
                NwGameObject targetObject = breachFeedbackEntries[0].Target;

                casterMessage += $"\n {targetObject.Name}:".ColorString(ColorConstants.Cyan);

                foreach (BreachFeedbackEntry entry in breachFeedbackEntries)
                    casterMessage += $"\n - {entry.SpellName}".ColorString(ColorConstants.Gray);
            }

            casterPlayer.SendServerMessage(casterMessage);
        }

        foreach (BreachFeedbackEntry[] breachFeedbackEntries in feedbackByTarget)
        {
            NwGameObject targetObject = breachFeedbackEntries[0].Target;

            if (!targetObject.IsPlayerControlled(out NwPlayer? targetPlayer))
                continue;

            string targetMessage = $"{caster.Name} dispelled magic from you:".ColorString(ColorConstants.Magenta);

            foreach (BreachFeedbackEntry entry in breachFeedbackEntries)
                targetMessage += $"\n - {entry.SpellName}".ColorString(ColorConstants.Gray);

            targetPlayer.SendServerMessage(targetMessage);
        }

        return breachFeedback.Count;
    }

    /// <summary>
    /// Return a list of all effects on a target contained in the Breach Spells list, grouped by spell and creator.
    /// </summary>
    private List<BreachableSpell> GetBreachableSpells(NwGameObject target) => target.ActiveEffects
        .Where(effect => effect.Spell is not null && BreachSpells.Contains(effect.Spell.SpellType))
        .GroupBy(effect => new { effect.Spell!.SpellType, effect.Creator })
        .Select(group => new BreachableSpell(
            group.Key.SpellType,
            group.ToArray()))
        .ToList();
}

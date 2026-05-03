using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.EffectUtils.ChangeAppearance;

[ServiceBinding(typeof(ChangeAppearanceService))]
public class ChangeAppearanceService(ScriptHandleFactory scriptHandleFactory)
{
    public Effect? EffectChangeAppearance(
        NwCreature creature,
        ChangeAppearanceData newAppearance,
        Effect? vfxApply,
        Effect? vfxRemove)
    {
        if (creature.ActiveEffects.Any(effect => effect.EffectType == EffectType.Polymorph))
        {
            creature.ControllingPlayer?.SendServerMessage
                ("Cannot change appearance while polymorphed.");
            return null;
        }

        if (!ChangeAppearanceUtils.StoreOriginalAppearance(creature))
            return null;

        ChangeAppearanceData? originalAppearance = ChangeAppearanceUtils.GetOriginalAppearance(creature);
        if (originalAppearance == null) return null;

        ScriptCallbackHandle changeShapeApply = scriptHandleFactory.CreateUniqueHandler(_ =>
        {
            if (vfxApply != null) creature.ApplyEffect(EffectDuration.Instant, vfxApply);
            ChangeAppearanceUtils.SetChangeAppearanceActive(creature);
            ChangeAppearanceUtils.SetAppearance(creature, newAppearance);
            return ScriptHandleResult.Handled;
        });

        ScriptCallbackHandle changeShapeRemove = scriptHandleFactory.CreateUniqueHandler(_ =>
        {
            if (vfxRemove != null) creature.ApplyEffect(EffectDuration.Instant, vfxRemove);
            ChangeAppearanceUtils.SetAppearance(creature, originalAppearance);
            ChangeAppearanceUtils.SetChangeAppearanceInactive(creature);
            ChangeAppearanceUtils.ClearOriginalAppearance(creature);
            return ScriptHandleResult.Handled;
        });

        return Effect.RunAction(onAppliedHandle: changeShapeApply, onRemovedHandle: changeShapeRemove);
    }
}

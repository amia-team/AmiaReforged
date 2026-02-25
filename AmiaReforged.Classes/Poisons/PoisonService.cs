using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Poisons;

[ServiceBinding(typeof(PoisonService))]
public class PoisonService(ScriptHandleFactory scriptHandleFactory)
{
    public void ApplyPoisonEffect(PoisonType poisonType, NwCreature targetCreature, NwGameObject poisoner, int dc)
    {
        PoisonData.PoisonValues? poisonValues = PoisonData.GetPoisonValues(poisonType);

        if (poisonValues == null)
            return;

        SavingThrowResult savingThrowResult
            = targetCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Poison, poisoner);

        Effect fortSaveVfx = Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse);
        Effect poisonVfx = Effect.VisualEffect(VfxType.ImpPoisonL);

        switch (savingThrowResult)
        {
            case SavingThrowResult.Immune:
                return;

            case SavingThrowResult.Success:
                targetCreature.ApplyEffect(EffectDuration.Instant, fortSaveVfx);
                return;

            case SavingThrowResult.Failure:
                targetCreature.ApplyEffect(EffectDuration.Instant, poisonVfx);

                ApplyPrimaryPoisonEffect(targetCreature, poisonValues);

                if (targetCreature.ActiveEffects.Any(e => e.Tag == poisonValues.Name)) return;

                Effect secondaryPoisonEffect = CreateSecondaryPoisonEffect(poisonValues, dc, poisoner);
                targetCreature.ApplyEffect(EffectDuration.Temporary, secondaryPoisonEffect, NwTimeSpan.FromTurns(1));

                break;
        }
    }

    private static void ApplyPrimaryPoisonEffect(NwCreature targetCreature, PoisonData.PoisonValues poisonValues)
    {
        switch (poisonValues)
        {
            case { PrimaryDieSides: not null, PrimaryDiceAmount: not null, PrimaryAbilityDamage: not null }:
                int damageRoll = Random.Shared.Roll(poisonValues.PrimaryDieSides.Value, poisonValues.PrimaryDiceAmount.Value);
                Effect abilityDecrease = Effect.AbilityDecrease(poisonValues.PrimaryAbilityDamage.Value, damageRoll);
                abilityDecrease.SubType = EffectSubType.Extraordinary;

                targetCreature.ApplyEffect(EffectDuration.Permanent, abilityDecrease);

                break;

            case { PrimaryScript: not null }:
                Effect? primaryScriptEffect = NWScript.EffectRunScript(poisonValues.PrimaryScript);

                if (primaryScriptEffect != null)
                    targetCreature.ApplyEffect(EffectDuration.Instant, primaryScriptEffect);

                break;
        }
    }

    private Effect CreateSecondaryPoisonEffect(PoisonData.PoisonValues poisonValues, int dc, NwGameObject poisoner)
    {
        ScriptCallbackHandle removeHandle
            = scriptHandleFactory.CreateUniqueHandler(info => OnSecondaryPoisonTrigger(info, poisonValues, dc, poisoner));

        Effect secondaryPoisonEffect = Effect.LinkEffects(Effect.RunAction(onRemovedHandle: removeHandle),
            Effect.Icon(EffectIcon.Poison!));

        secondaryPoisonEffect.SubType = EffectSubType.Extraordinary;
        secondaryPoisonEffect.Tag = poisonValues.Name;

        return secondaryPoisonEffect;
    }

    private static ScriptHandleResult OnSecondaryPoisonTrigger(CallInfo info, PoisonData.PoisonValues poisonValues, int dc,
        NwGameObject poisoner)
    {
        if (info.ObjectSelf is not NwCreature targetCreature)
        {
            return ScriptHandleResult.Handled;
        }

        SavingThrowResult savingThrowResult =
            targetCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Poison, poisoner);

        Effect fortSaveVfx = Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse);
        Effect poisonVfx = Effect.VisualEffect(VfxType.ImpPoisonL);

        switch (savingThrowResult)
        {
            case SavingThrowResult.Immune:
                break;

            case SavingThrowResult.Success:
                targetCreature.ApplyEffect(EffectDuration.Instant, fortSaveVfx);
                break;

            case SavingThrowResult.Failure:
                targetCreature.ApplyEffect(EffectDuration.Instant, poisonVfx);

                switch (poisonValues)
                {
                    case { SecondaryDieSides: not null, SecondaryDiceAmount: not null, SecondaryAbilityDamage: not null }:
                        int damageRoll = Random.Shared.Roll(poisonValues.SecondaryDieSides.Value, poisonValues.SecondaryDiceAmount.Value);
                        Effect abilityDecrease = Effect.AbilityDecrease(poisonValues.SecondaryAbilityDamage.Value, damageRoll);
                        abilityDecrease.SubType = EffectSubType.Extraordinary;

                        targetCreature.ApplyEffect(EffectDuration.Permanent, abilityDecrease);

                        break;

                    case { SecondaryScript: not null }:
                        Effect? secondaryScriptEffect = NWScript.EffectRunScript(poisonValues.SecondaryScript);

                        if (secondaryScriptEffect != null)
                            targetCreature.ApplyEffect(EffectDuration.Instant, secondaryScriptEffect);

                        break;
                }
                break;
        }

        return ScriptHandleResult.Handled;
    }
}

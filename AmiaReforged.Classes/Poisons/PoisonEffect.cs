using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Poisons;

public class PoisonEffect(ScriptHandleFactory scriptHandleFactory)
{
    public void ApplyPoison(PoisonType poisonType, NwCreature targetCreature, NwCreature poisoner, int dc)
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

                Effect poisonRunActionEffect = CreateSecondaryPoisonEffect(poisonValues, dc, poisoner);
                targetCreature.ApplyEffect(EffectDuration.Temporary, poisonRunActionEffect, NwTimeSpan.FromTurns(1));

                break;
        }
    }

    private void ApplyPrimaryPoisonEffect(NwCreature targetCreature, PoisonData.PoisonValues poisonValues)
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

    private Effect CreateSecondaryPoisonEffect(PoisonData.PoisonValues poisonValues, int dc, NwCreature poisoner)
    {
        ScriptCallbackHandle removeHandle
            = scriptHandleFactory.CreateUniqueHandler(info => OnSecondaryPoisonTrigger(info, poisonValues, dc, poisoner));

        Effect runAction = Effect.RunAction(onRemovedHandle: removeHandle);
        runAction.SubType = EffectSubType.Extraordinary;

        Effect poisonIcon = Effect.Icon(EffectIcon.Poison!);
        return Effect.LinkEffects(runAction, poisonIcon);
    }

    private ScriptHandleResult OnSecondaryPoisonTrigger(CallInfo info, PoisonData.PoisonValues poisonValues, int dc,
        NwCreature poisoner)
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
                return ScriptHandleResult.Handled;

            case SavingThrowResult.Success:
                targetCreature.ApplyEffect(EffectDuration.Instant, fortSaveVfx);
                return ScriptHandleResult.Handled;

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

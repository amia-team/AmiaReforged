using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Shadowdancer.Shadow;

[ServiceBinding(typeof(ShadowClaw))]
public class ShadowClaw
{
    private static readonly Dictionary<int, (int ConDrainDie, int StrDrainDie, int Dc)> ShadowClawMap = new()
    {
        { 13, (4, 0, 36) },
        { 14, (4, 0, 36) },
        { 15, (4, 0, 38) },
        { 16, (6, 0, 38) },
        { 17, (6, 0, 40) },
        { 18, (6, 0, 40) },
        { 19, (6, 2, 40) },
        { 20, (8, 2, 42) }
    };

    private const int KillValue = 3;

    public ShadowClaw()
    {
        NwModule.Instance.OnCreatureDamage += DoShadowClaw;
    }

    private void DoShadowClaw(OnCreatureDamage attackData)
    {
        if (attackData.DamagedBy.ResRef is not "sd_shadow_4" || attackData.DamagedBy is not NwCreature shadow ||
            attackData.Target is not NwCreature targetCreature || targetCreature.IsImmuneTo(ImmunityType.AbilityDecrease))
            return;

        int sdLevel = shadow.GetObjectVariable<LocalVariableInt>("sd_level").Value;

        if (!ShadowClawMap.TryGetValue(sdLevel, out (int ConDrainDie, int StrDrainDie, int Dc) shadowClaw)) return;

        SavingThrowResult savingThrowResult =
            targetCreature.RollSavingThrow(SavingThrow.Fortitude, shadowClaw.Dc, SavingThrowType.Negative, shadow);

        switch (savingThrowResult)
        {
            case SavingThrowResult.Failure:
                ApplyShadowClaw(shadowClaw.ConDrainDie, shadowClaw.StrDrainDie, targetCreature, shadow);
                break;
            case SavingThrowResult.Success:
                targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
                break;
            case SavingThrowResult.Immune:
                break;
        }
    }

    private void ApplyShadowClaw(int conDrainDie, int strDrainDie, NwCreature targetCreature, NwCreature shadow)
    {
        if (strDrainDie > 0)
        {
            int strDrain = Random.Shared.Roll(strDrainDie);
            int targetStr = targetCreature.GetAbilityScore(Ability.Strength);

            if (targetStr - strDrain <= KillValue)
            {
                _= ShadowClawKill(targetCreature, shadow);
                return;
            }

            targetCreature.ApplyEffect(EffectDuration.Permanent, Effect.AbilityDecrease(Ability.Strength, strDrain));
        }

        int conDrain = Random.Shared.Roll(conDrainDie);
        int targetCon = targetCreature.GetAbilityScore(Ability.Constitution);

        if (targetCon - conDrain <= KillValue)
        {
            _= ShadowClawKill(targetCreature, shadow);
            return;
        }

        targetCreature.ApplyEffect(EffectDuration.Permanent, Effect.AbilityDecrease(Ability.Constitution, conDrain));
        targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReduceAbilityScore));
    }

    private async Task ShadowClawKill(NwCreature targetCreature, NwCreature shadow)
    {
        await shadow.WaitForObjectContext();
        Effect shadowClawKill = Effect.Death();
        shadowClawKill.IgnoreImmunity = true;

        targetCreature.ApplyEffect(EffectDuration.Instant, shadowClawKill);
        targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDeath));
    }
}

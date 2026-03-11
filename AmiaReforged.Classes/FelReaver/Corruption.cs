using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.FelReaver;

[ServiceBinding(typeof(CorruptionStance))]
public class CorruptionStance
{
    private const string CorruptionStanceEffectTag = "CORRUPTION_STANCE";

    public CorruptionStance()
    {
        NwModule.Instance.OnClientEnter += ClearCorruptionStance;
        NwModule.Instance.OnCombatModeToggle += NewCorruptionStance;
    }
    private void ClearCorruptionStance(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.LoginCreature == null)
        {
            LogManager.GetCurrentClassLogger().Info(message: "Could not find login creature.");
            return;
        }

        List<Effect> activeCorrStanceEffects
            = obj.Player.LoginCreature.ActiveEffects.Where(e => e.Tag == CorruptionStanceEffectTag).ToList();

        if (activeCorrStanceEffects.Count == 0) return;

        foreach (Effect effect in activeCorrStanceEffects)
        {
            obj.Player.LoginCreature.RemoveEffect(effect);
        }
    }
    private void NewCorruptionStance(OnCombatModeToggle obj)
    {
     // if (obj.NewMode != CombatMode.CorruptionStance) return;
        if (obj.Creature.IsPlayerControlled(out NwPlayer? player))
        {
            return;
        }

        obj.PreventToggle = true;

        NwCreature character = obj.Creature;

        List<Effect> activeCorrStanceEffects
            = character.ActiveEffects.Where(e => e.Tag == CorruptionStanceEffectTag).ToList();
        if (activeCorrStanceEffects.Count > 0)
        {
            foreach (Effect effect in activeCorrStanceEffects)
            {
                character.RemoveEffect(effect);
            }

            character.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.DurAuraPulseBrownBlack));
            return;
        }

//Defining Damage over time based on CON score
            character.OnCombatRoundEnd += SelfDoT;
        }

    private void SelfDoT(CreatureEvents.OnCombatRoundEnd obj)
    {
        // Define how much damage to apply depending on CON score
        int nDamage;
        int nConScore = obj.Creature.GetAbilityScore(Ability.Constitution, true);

        if (nConScore <= 15)
        {
            nDamage = NWScript.d12(1);
            // Optional: Send a message to the log to confirm
            obj.Creature.SpeakString("15 or less Constitution detected. Damage set to 1d12.");
        }

        else if (nConScore <= 17)
        {
            nDamage = NWScript.d10(1);
        }

        else if (nConScore <= 19)
        {
            nDamage = NWScript.d8(1);
        }

        else if (nConScore >= 20)
        {
            nDamage = NWScript.d6(1);
        }

        else
        {
            nDamage = NWScript.d20(1);
        }

        Effect damage = Effect.Damage(nDamage, DamageType.Divine);
        obj.Creature.ApplyEffect(EffectDuration.Instant, damage);
        obj.Creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDestruction));
    }
}

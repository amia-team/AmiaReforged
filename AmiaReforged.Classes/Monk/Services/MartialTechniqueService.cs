// A service for martial monk techniques

using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;
using static AmiaReforged.Classes.Monk.Constants.MonkTechnique;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(MartialTechniqueService))]
public class MartialTechniqueService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Effect _martialEffect = Effect.VisualEffect(VfxType.None);

    public MartialTechniqueService()
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live") return;

        // Register methods to listen for the events.
        NwModule.Instance.OnUseFeat += MartialTechniqueUseFeat;
        NwModule.Instance.OnCombatRoundStart += EnterMartialTechnique;
        NwModule.Instance.OnCreatureAttack += OnHitApplyTechnique;
        NwModule.Instance.OnEffectApply += CueMartialTechniqueActivated;
        NwModule.Instance.OnEffectRemove += CueMartialTechniqueDeactivated;
        Log.Info(message: "Monk Martial Technique Service initialized.");
    }

    /// <summary>
    ///     Sets a martial technique for activation on next combat round, or instantly if out of combat
    /// </summary>
    private async void MartialTechniqueUseFeat(OnUseFeat eventData)
    {
        if (eventData.Feat.Id is not (MonkFeat.StunningStrike or MonkFeat.EagleStrike or MonkFeat.AxiomaticStrike))
            return;

        NwFeat technique = eventData.Feat;
        NwCreature monk = eventData.Creature;

        bool techniquePrevented = PreventMartialTechnique(monk, technique.Name.ToString());

        if (techniquePrevented) return;

        // If monk is in combat, queue martial technique change for next combat round
        if (monk.IsInCombat)
        {
            LocalVariableInt queuedTechnique = monk.GetObjectVariable<LocalVariableInt>(MartialTechnique);

            queuedTechnique.Value = technique.Id switch
            {
                MonkFeat.StunningStrike => StunningValue,
                MonkFeat.EagleStrike => EagleValue,
                MonkFeat.AxiomaticStrike => AxiomaticValue,
                _ => -1
            };
        }

        // Else activate martial technique straight away
        if (monk.IsInCombat) return;

        _martialEffect.Tag = technique.Id switch
        {
            MonkFeat.StunningStrike => StunningTag,
            MonkFeat.EagleStrike => EagleTag,
            MonkFeat.AxiomaticStrike => AxiomaticTag,
            _ => ""
        };

        // If the same technique is being activated, just deactivate it and return; otherwise activate the new technique
        foreach (Effect effect in monk.ActiveEffects)
        {
            if (effect.Tag == _martialEffect.Tag)
            {
                monk.RemoveEffect(effect);
                return;
            }

            if (effect.Tag is not (StunningTag or EagleTag or AxiomaticTag)) continue;

            monk.RemoveEffect(effect);
            break;
        }

        await NwTask.Delay(TimeSpan.FromMilliseconds(1));
        _martialEffect.SubType = EffectSubType.Unyielding;
        monk.ApplyEffect(EffectDuration.Permanent, _martialEffect);
    }

    /// <summary>
    ///     On combat round start switches into the active martial technique
    /// </summary>
    private async void EnterMartialTechnique(OnCombatRoundStart eventData)
    {
        // Creature must be monk
        if (eventData.Creature.GetClassInfo(NwClass.FromClassType(ClassType.Monk)) is null) return;

        NwCreature monk = eventData.Creature;
        LocalVariableInt queuedTechnique = monk.GetObjectVariable<LocalVariableInt>(MartialTechnique);

        //  If technique is queued up, activate it
        if (queuedTechnique.HasValue)
        {
            _martialEffect.Tag = queuedTechnique.Value switch
            {
                StunningValue => StunningTag,
                EagleValue => EagleTag,
                AxiomaticValue => AxiomaticTag,
                _ => ""
            };

            // If the same technique is being activated, just deactivate it and return; otherwise activate the new technique
            foreach (Effect effect in monk.ActiveEffects)
            {
                if (effect.Tag == _martialEffect.Tag)
                {
                    monk.RemoveEffect(effect);
                    queuedTechnique.Delete();
                    return;
                }

                if (effect.Tag is not (StunningTag or EagleTag or AxiomaticTag)) continue;

                monk.RemoveEffect(effect);
                break;
            }

            await NwTask.Delay(TimeSpan.FromMilliseconds(1));
            queuedTechnique.Delete();
            _martialEffect.SubType = EffectSubType.Unyielding;
            monk.ApplyEffect(EffectDuration.Permanent, _martialEffect);
        }

        Effect? technique =
            monk.ActiveEffects.FirstOrDefault(effect => effect.Tag is StunningTag or EagleTag or AxiomaticTag);

        if (technique is null) return;
        
        // Check if gear restricts technique use
        string techniqueName = technique.Tag switch
        {
            StunningTag => "Stunning Strike",
            EagleTag => "Eagle Strike",
            AxiomaticTag => "Axiomatic Strike",
            _ => ""
        };
        
        bool techniquePrevented = PreventMartialTechnique(monk, techniqueName);
        
        if (techniquePrevented) return;

        // Remove martial technique from cooldown to allow hits to proc again
        foreach (Effect effect in monk.ActiveEffects)
        {
            if (effect.Tag is not MartialCooldownTag) continue;
            
            monk.RemoveEffect(effect);
            break;
        }

        // Remove eagle strike counter
        if (monk.GetObjectVariable<LocalVariableInt>(EagleStrikesCounter).HasValue)
            monk.GetObjectVariable<LocalVariableInt>(EagleStrikesCounter).Delete();
    }
    
    /// <summary>
    ///     Applies the martial technique effects and cooldown on hit
    /// </summary>
    private static void OnHitApplyTechnique(OnCreatureAttack attackData)
    {
        // Creature must be monk
        if (attackData.Attacker.GetClassInfo(NwClass.FromClassType(ClassType.Monk)) is null) return;
        NwCreature monk = attackData.Attacker;

        // Can't have the cooldown active for martial technique procs
        if (monk.ActiveEffects.Any(effect => effect.Tag is MartialCooldownTag)) return;

        Effect? technique =
            monk.ActiveEffects.FirstOrDefault(effect => effect.Tag is StunningTag or EagleTag or AxiomaticTag);
        
        if (technique is null) return;

        // On hit, apply technique effects and cooldown
        bool isHit =
            attackData.AttackResult is AttackResult.Hit or AttackResult.AutomaticHit or AttackResult.CriticalHit;
        
        if (!isHit) return;

        // Apply technique effects
        switch (technique.Tag)
        {
            case StunningTag:
                StunningStrike.ApplyStunningStrike(attackData);
                break;
            case EagleTag:
                EagleStrike.ApplyEagleStrike(attackData);
                break;
            case AxiomaticTag:
                AxiomaticStrike.ApplyAxiomaticStrike(attackData);
                break;
        }

        LocalVariableInt eagleCounter = monk.GetObjectVariable<LocalVariableInt>(EagleStrikesCounter);

        Effect martialCooldownEffect = Effect.VisualEffect(VfxType.None);
        martialCooldownEffect.SubType = EffectSubType.Unyielding;
        martialCooldownEffect.Tag = MartialCooldownTag;

        if (technique.Tag is StunningTag)
            monk.ApplyEffect(EffectDuration.Permanent, martialCooldownEffect);

        if (technique.Tag is not EagleTag) return;

        eagleCounter.Value++;

        if (eagleCounter == 2)
            monk.ApplyEffect(EffectDuration.Permanent, martialCooldownEffect);
    }
    
    /// <summary>
    ///     Cues the activation of the martial technique with a floaty text
    /// </summary>
    private static void CueMartialTechniqueActivated(OnEffectApply eventData)
    {
        if (eventData.Effect.Tag is not (StunningTag or EagleTag or AxiomaticTag)) return;

        if (!eventData.Object.IsPlayerControlled(out NwPlayer? player)) return;
        
        string techniqueName = eventData.Effect.Tag switch
        {
            StunningTag => "Stunning Strike",
            EagleTag => "Eagle Strike",
            AxiomaticTag => "Axiomatic Strike",
            _ => ""
        };

        player.FloatingTextString($"*{techniqueName} Activated*", false, false);
    }

    /// <summary>
    ///     Cues the deactivation of the martial technique with a floaty text
    /// </summary>
    private static void CueMartialTechniqueDeactivated(OnEffectRemove eventData)
    {
        if (!eventData.Object.IsPlayerControlled(out NwPlayer? player)) return;
        if (eventData.Effect.Tag is not (StunningTag or EagleTag or AxiomaticTag)) return;

        string techniqueName = eventData.Effect.Tag switch
        {
            StunningTag => "Stunning Strike",
            EagleTag => "Eagle Strike",
            AxiomaticTag => "Axiomatic Strike",
            _ => ""
        };

        player.FloatingTextString($"*{techniqueName} Deactivated*", false, false);
    }

    
    private static bool PreventMartialTechnique(NwCreature monk, string techniqueName)
    {
        bool hasArmor = monk.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;
        bool hasShield = monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category is BaseItemCategory.Shield;
        bool hasFocusWithoutUnarmed = monk.GetItemInSlot(InventorySlot.RightHand) is not null
                                      && monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category is
                                          BaseItemCategory.Torches;

        bool isTechniquePrevented = hasArmor || hasShield || hasFocusWithoutUnarmed;

        if (!monk.IsPlayerControlled(out NwPlayer? player)) return isTechniquePrevented;

        if (hasArmor)
            player.SendServerMessage($"Wearing an armor has prevented your {techniqueName}.");
        if (hasShield)
            player.SendServerMessage($"Wielding a shield has prevented your {techniqueName}.");
        if (hasFocusWithoutUnarmed) 
            player.SendServerMessage($"Wielding a focus without being unarmed has prevented your {techniqueName}.");

        return isTechniquePrevented;
    }
}
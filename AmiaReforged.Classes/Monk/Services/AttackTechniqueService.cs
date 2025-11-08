using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(AttackTechniqueService))]
public class AttackTechniqueService
{
    private readonly TechniqueFactory _techniqueFactory;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly Effect _attackCooldownEffect = Effect.VisualEffect(VfxType.None);

    private static readonly NwFeat? StunningStrikeFeat = NwFeat.FromFeatId(MonkFeat.StunningStrike);

    private const string AttackCooldownTag = "attack_technique_cooldown";
    private const string AttackTechnique = "attack_technique";
    private const string StunningTag = nameof(TechniqueType.StunningStrike);
    private const string EagleTag = nameof(TechniqueType.EagleStrike);
    private const string AxiomaticTag = nameof(TechniqueType.AxiomaticStrike);
    private const string EagleStrikeCounter = "eagle_strike_counter";

    public AttackTechniqueService(TechniqueFactory techniqueFactory)
    {
        _techniqueFactory = techniqueFactory;

        _attackCooldownEffect.SubType = EffectSubType.Unyielding;
        _attackCooldownEffect.Tag = AttackCooldownTag;

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");
        if (environment == "live") return;

        NwModule.Instance.OnUseFeat += AttackTechniqueUseFeat;
        NwModule.Instance.OnCombatRoundStart += EnterAttackTechnique;
        NwModule.Instance.OnCreatureAttack += OnHitApplyAxiomatic;
        NwModule.Instance.OnCreatureDamage += OnDamageApplyTechnique;
        NwModule.Instance.OnEffectApply += CueAttackTechniqueActivated;
        NwModule.Instance.OnEffectRemove += CueAttackTechniqueDeactivated;
        Log.Info(message: "Monk Attack Technique Service initialized.");
    }

    /// <summary>
    ///     Sets a attack technique for activation on next combat round, or instantly if out of combat
    /// </summary>
    private void AttackTechniqueUseFeat(OnUseFeat eventData)
    {
        if (eventData.Feat.Id is not (MonkFeat.StunningStrike or MonkFeat.EagleStrike or MonkFeat.AxiomaticStrike))
            return;
        if (eventData.Creature.GetClassInfo(ClassType.Monk) is null) return;

        NwFeat feat = eventData.Feat;
        NwCreature monk = eventData.Creature;

        if (PreventAttackTechnique(monk, feat.Name.ToString())) return;

        // If monk is in combat, queue attack technique change for next combat round
        if (monk.IsInCombat)
        {
            QueueTechnique(monk, feat.Id);
            return;
        }
        ActivateTechnique(monk, feat.Id);
    }

    private static void QueueTechnique(NwCreature monk, int featId)
    {
        LocalVariableInt queuedTechnique = monk.GetObjectVariable<LocalVariableInt>(AttackTechnique);

        queuedTechnique.Value = featId;
    }

    private void ActivateTechnique(NwCreature monk, int techniqueId)
    {
        string? newTechniqueTag = Enum.GetName((TechniqueType)techniqueId);

        Effect? activeTechnique = GetActiveTechniqueEffect(monk);

        // If the old technique was the same as the new one, we're just toggling it off.
        if (activeTechnique is { Tag: { } tag } && tag == newTechniqueTag)
        {
            monk.RemoveEffect(activeTechnique);
            return;
        }

        if (activeTechnique != null)
            monk.RemoveEffect(activeTechnique);

        Effect newTechnique = Effect.VisualEffect(VfxType.None);
        newTechnique.SubType = EffectSubType.Unyielding;
        newTechnique.Tag = newTechniqueTag;

        monk.ApplyEffect(EffectDuration.Permanent, newTechnique);
    }

    /// <summary>
    ///     On combat round start switches into the active attack technique
    /// </summary>
    private void EnterAttackTechnique(OnCombatRoundStart eventData)
    {
        if (!eventData.Creature.KnowsFeat(StunningStrikeFeat!)) return;

        NwCreature monk = eventData.Creature;
        LocalVariableInt queuedTechnique = monk.GetObjectVariable<LocalVariableInt>(AttackTechnique);

        Effect? activeTechnique = GetActiveTechniqueEffect(monk);

        // Exit early if nothing is queued and no active technique exists
        if (queuedTechnique.HasNothing && activeTechnique == null)
            return;

        if (queuedTechnique.HasValue)
        {
            ActivateTechnique(monk, queuedTechnique.Value);
            queuedTechnique.Delete();
        }
        else if (activeTechnique != null && PreventAttackTechnique(monk, activeTechnique.Tag))
        {
            monk.RemoveEffect(activeTechnique);
            return;
        }

        ResetTechniqueCooldownAndCounter(monk);
    }

    /// <summary>
    ///     This is only used for Axiomatic
    /// </summary>
    private void OnHitApplyAxiomatic(OnCreatureAttack attackData)
    {
        if (!attackData.Attacker.KnowsFeat(StunningStrikeFeat!)) return;

        if (attackData.AttackResult is not (AttackResult.Hit or AttackResult.AutomaticHit or AttackResult.CriticalHit
            or AttackResult.DevastatingCritical)) return;

        NwCreature monk = attackData.Attacker;

        foreach (Effect effect in monk.ActiveEffects)
        {
            if (effect.Tag is AttackCooldownTag) return;
            if (effect.Tag is not AxiomaticTag) continue;

            ITechnique? axiomaticTechniqueHandler = _techniqueFactory.GetTechnique(TechniqueType.AxiomaticStrike);
            axiomaticTechniqueHandler?.HandleAttackTechnique(monk, attackData);
            return;
        }
    }

    private void OnDamageApplyTechnique(OnCreatureDamage damageData)
    {
        if (damageData.DamagedBy is not NwCreature monk || !monk.KnowsFeat(StunningStrikeFeat!)) return;
        if (!monk.ActiveEffects.Any(effect => effect.Tag is StunningTag or EagleTag))
            return;
        if (monk.ActiveEffects.Any(effect => effect.Tag is AttackCooldownTag))
            return;

        string? techniqueTag = GetActiveTechniqueEffect(monk)?.Tag;

        if (techniqueTag == null || !Enum.TryParse(techniqueTag, out TechniqueType techniqueType))
            return;

        ITechnique? techniqueHandler = _techniqueFactory.GetTechnique(techniqueType);
        techniqueHandler?.HandleDamageTechnique(monk, damageData);

        ApplyTechniqueCooldown(monk, techniqueTag);
    }

    private void ApplyTechniqueCooldown(NwCreature monk, string? techniqueTag)
    {
        switch (techniqueTag)
        {
            case StunningTag:
                monk.ApplyEffect(EffectDuration.Permanent, _attackCooldownEffect);
                break;

            case EagleTag:
                LocalVariableInt eagleStrikeCounter = monk.GetObjectVariable<LocalVariableInt>(EagleStrikeCounter);
                eagleStrikeCounter.Value++;

                if (eagleStrikeCounter.Value == 2)
                    monk.ApplyEffect(EffectDuration.Permanent, _attackCooldownEffect);
                break;
        }
    }

    /// <summary>
    ///     Cues the activation of the attack technique with a floaty text
    /// </summary>
    private static void CueAttackTechniqueActivated(OnEffectApply eventData)
    {
        if (!eventData.Object.IsPlayerControlled(out NwPlayer? player) ||
            eventData.Effect.Tag is not (StunningTag or EagleTag or AxiomaticTag)) return;

        player.FloatingTextString($"*{GetSpacedTag(eventData.Effect.Tag)} Activated*", false, false);
    }

    /// <summary>
    ///     Cues the deactivation of the attack technique with a floaty text
    /// </summary>
    private static void CueAttackTechniqueDeactivated(OnEffectRemove eventData)
    {
        if (!eventData.Object.IsPlayerControlled(out NwPlayer? player) ||
            eventData.Effect.Tag is not (StunningTag or EagleTag or AxiomaticTag)) return;

        player.FloatingTextString($"*{GetSpacedTag(eventData.Effect.Tag)} Deactivated*", false, false);
    }

    private static string GetSpacedTag(string tag)
    {
        return tag switch
        {
            StunningTag => "Stunning Strike",
            EagleTag    => "Eagle Strike",
            AxiomaticTag => "Axiomatic Strike",
            _ => tag
        };
    }

    private static bool PreventAttackTechnique(NwCreature monk, string? techniqueName)
    {
        bool hasArmor = monk.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;
        bool hasShield = monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category == BaseItemCategory.Shield;
        bool hasFocusWithoutUnarmed =
            monk.GetItemInSlot(InventorySlot.RightHand) != null
            && monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category == BaseItemCategory.Torches;
        bool hasRangedWeapon = MonkUtils.GetMonkPath(monk) != PathType.FloatingLeaf && monk.IsRangedWeaponEquipped;

        if (!monk.IsPlayerControlled(out NwPlayer? player))
            return hasArmor || hasShield || hasFocusWithoutUnarmed || hasRangedWeapon;

        if (hasArmor || hasShield || hasFocusWithoutUnarmed || hasRangedWeapon)
        {
            string reason = hasArmor
                ? "Wearing an armor"
                : hasShield
                    ? "Wielding a shield"
                    : hasFocusWithoutUnarmed
                        ? "Wielding a focus without being unarmed"
                        : "Wielding a ranged weapon";
            player.SendServerMessage($"{reason} has prevented your {techniqueName}.");
            return true;
        }

        return false;
    }

    private static Effect? GetActiveTechniqueEffect(NwCreature monk) =>
        monk.ActiveEffects.FirstOrDefault(effect => effect.Tag is StunningTag or EagleTag or AxiomaticTag);

    private static void ResetTechniqueCooldownAndCounter(NwCreature monk)
    {
        Effect? techniqueCdEffect = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag is AttackCooldownTag);

        if (techniqueCdEffect != null)
            monk.RemoveEffect(techniqueCdEffect);

        LocalVariableInt eagleStrikeCounter = monk.GetObjectVariable<LocalVariableInt>(EagleStrikeCounter);
        if (eagleStrikeCounter.HasValue)
            eagleStrikeCounter.Delete();
    }
}

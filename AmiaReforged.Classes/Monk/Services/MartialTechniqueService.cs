using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(MartialTechniqueService))]
public class MartialTechniqueService
{
    private readonly TechniqueFactory _techniqueFactory;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly Effect _martialCooldownEffect = Effect.VisualEffect(VfxType.None);

    private const string MartialCooldownTag = "martialtechnique_cd";
    private const string MartialTechnique = "martial_technique";
    private const string StunningTag = nameof(TechniqueType.StunningStrike);
    private const string EagleTag = nameof(TechniqueType.EagleStrike);
    private const string AxiomaticTag = nameof(TechniqueType.AxiomaticStrike);
    private const string EagleStrikeCounter = "eagle_strike_counter";

    public MartialTechniqueService(TechniqueFactory techniqueFactory)
    {
        _techniqueFactory = techniqueFactory;

        _martialCooldownEffect.SubType = EffectSubType.Unyielding;
        _martialCooldownEffect.Tag = MartialCooldownTag;

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");
        if (environment == "live") return;

        NwModule.Instance.OnUseFeat += MartialTechniqueUseFeat;
        NwModule.Instance.OnCombatRoundStart += EnterMartialTechnique;
        NwModule.Instance.OnCreatureAttack += OnHitApplyAxiomatic;
        NwModule.Instance.OnCreatureDamage += OnDamageApplyTechnique;
        NwModule.Instance.OnEffectApply += CueMartialTechniqueActivated;
        NwModule.Instance.OnEffectRemove += CueMartialTechniqueDeactivated;
        Log.Info(message: "Monk Martial Technique Service initialized.");
    }

    /// <summary>
    ///     Sets a martial technique for activation on next combat round, or instantly if out of combat
    /// </summary>
    private void MartialTechniqueUseFeat(OnUseFeat eventData)
    {
        if (eventData.Feat.Id is not (MonkFeat.StunningStrike or MonkFeat.EagleStrike or MonkFeat.AxiomaticStrike))
            return;
        if (eventData.Creature.GetClassInfo(ClassType.Monk) is null) return;

        NwFeat feat = eventData.Feat;
        NwCreature monk = eventData.Creature;

        if (PreventMartialTechnique(monk, feat.Name.ToString())) return;

        // If monk is in combat, queue martial technique change for next combat round
        if (monk.IsInCombat)
        {
            QueueTechnique(monk, feat.Id);
            return;
        }
        ActivateTechnique(monk, feat.Id);
    }

    private static void QueueTechnique(NwCreature monk, int featId)
    {
        LocalVariableInt queuedTechnique = monk.GetObjectVariable<LocalVariableInt>(MartialTechnique);

        queuedTechnique.Value = featId;
    }

    private void ActivateTechnique(NwCreature monk, int techniqueId)
    {
        string? newTechniqueTag = Enum.GetName((TechniqueType)techniqueId);

        Effect? activeTechnique = GetActiveTechniqueEffect(monk);

        // If the old technique was the same as the new one, we're just toggling it off.
        if (activeTechnique is { Tag: { } tag } && tag == newTechniqueTag)
        {
            monk.RemoveEffect(activeTechnique!);
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
    ///     On combat round start switches into the active martial technique
    /// </summary>
    private void EnterMartialTechnique(OnCombatRoundStart eventData)
    {
        NwCreature monk = eventData.Creature;
        LocalVariableInt queuedTechnique = monk.GetObjectVariable<LocalVariableInt>(MartialTechnique);

        Effect? activeTechnique = GetActiveTechniqueEffect(monk);

        // Exit early if nothing is queued and no active technique exists
        if (queuedTechnique.HasNothing && activeTechnique == null)
            return;

        if (queuedTechnique.HasValue)
        {
            ActivateTechnique(monk, queuedTechnique.Value);
            queuedTechnique.Delete();
        }
        else if (activeTechnique != null && PreventMartialTechnique(monk, activeTechnique.Tag))
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
        if (attackData.AttackResult is not (AttackResult.Hit or AttackResult.AutomaticHit or AttackResult.CriticalHit
            or AttackResult.DevastatingCritical)) return;

        NwCreature monk = attackData.Attacker;

        foreach (Effect effect in monk.ActiveEffects)
        {
            if (effect.Tag is MartialCooldownTag) return;
            if (effect.Tag is not AxiomaticTag) continue;

            ITechnique? axiomaticTechniqueHandler = _techniqueFactory.GetTechnique(TechniqueType.AxiomaticStrike);
            axiomaticTechniqueHandler?.HandleAttackTechnique(monk, attackData);
            return;
        }
    }

    private void OnDamageApplyTechnique(OnCreatureDamage damageData)
    {
        if (damageData.DamagedBy is not NwCreature monk) return;
        if (!monk.ActiveEffects.Any(effect => effect.Tag is StunningTag or EagleTag))
            return;
        if (monk.ActiveEffects.Any(effect => effect.Tag is MartialCooldownTag))
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
                monk.ApplyEffect(EffectDuration.Permanent, _martialCooldownEffect);
                break;

            case EagleTag:
                LocalVariableInt eagleStrikeCounter = monk.GetObjectVariable<LocalVariableInt>(EagleStrikeCounter);
                eagleStrikeCounter.Value++;

                if (eagleStrikeCounter.Value == 2)
                    monk.ApplyEffect(EffectDuration.Permanent, _martialCooldownEffect);
                break;
        }
    }

    /// <summary>
    ///     Cues the activation of the martial technique with a floaty text
    /// </summary>
    private static void CueMartialTechniqueActivated(OnEffectApply eventData)
    {
        if (!eventData.Object.IsPlayerControlled(out NwPlayer? player) ||
            eventData.Effect.Tag is not (StunningTag or EagleTag or AxiomaticTag)) return;

        player.FloatingTextString($"*{GetSpacedTag(eventData.Effect.Tag)} Activated*", false, false);
    }

    /// <summary>
    ///     Cues the deactivation of the martial technique with a floaty text
    /// </summary>
    private static void CueMartialTechniqueDeactivated(OnEffectRemove eventData)
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

    private static bool PreventMartialTechnique(NwCreature monk, string? techniqueName)
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
        Effect? techniqueCdEffect = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag is MartialCooldownTag);

        if (techniqueCdEffect != null)
            monk.RemoveEffect(techniqueCdEffect);

        LocalVariableInt eagleStrikeCounter = monk.GetObjectVariable<LocalVariableInt>(EagleStrikeCounter);
        if (eagleStrikeCounter.HasValue)
            eagleStrikeCounter.Delete();
    }
}

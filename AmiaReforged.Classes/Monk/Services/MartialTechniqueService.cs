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

    private readonly Effect _martialEffect = Effect.VisualEffect(VfxType.None);
    private readonly Effect _martialCooldownEffect = Effect.VisualEffect(VfxType.None);

    private const string MartialCooldownTag = "martialtechnique_cd";
    private const string MartialTechnique = "martial_technique";
    private const string StunningTag = "martial_technique_stunning";
    private const string EagleTag = "martial_technique_eagle";
    private const string AxiomaticTag = "martial_technique_axiomatic";
    private const string EagleStrikeCounter = "eagle_strike_counter";

    public MartialTechniqueService(TechniqueFactory techniqueFactory)
    {
        _techniqueFactory = techniqueFactory;

        _martialEffect.SubType = EffectSubType.Unyielding;
        _martialCooldownEffect.SubType = EffectSubType.Unyielding;
        _martialCooldownEffect.Tag = MartialCooldownTag;

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");
        if (environment == "live") return;

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
    private void MartialTechniqueUseFeat(OnUseFeat eventData)
    {
        if (eventData.Feat.Id is not (MonkFeat.StunningStrike or MonkFeat.EagleStrike or MonkFeat.AxiomaticStrike))
            return;

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
        string newTechniqueTag = techniqueId switch
        {
            MonkFeat.StunningStrike => StunningTag,
            MonkFeat.EagleStrike => EagleTag,
            MonkFeat.AxiomaticStrike => AxiomaticTag,
            _ => ""
        };

        Effect? activeTechnique = GetActiveTechniqueEffect(monk);

        if (activeTechnique != null)
            monk.RemoveEffect(activeTechnique);

        // If the old technique was the same as the new one, we're just toggling it off.
        if (activeTechnique?.Tag == newTechniqueTag)
            return;

        _martialEffect.Tag = newTechniqueTag;
        monk.ApplyEffect(EffectDuration.Permanent, _martialEffect);
    }

    /// <summary>
    ///     On combat round start switches into the active martial technique
    /// </summary>
    private void EnterMartialTechnique(OnCombatRoundStart eventData)
    {
        NwCreature monk = eventData.Creature;
        if (monk.GetClassInfo(ClassType.Monk) is null) return;

        LocalVariableInt queuedTechnique = monk.GetObjectVariable<LocalVariableInt>(MartialTechnique);

        if (queuedTechnique.HasValue)
        {
            ActivateTechnique(monk, queuedTechnique.Value);
            queuedTechnique.Delete();
        }

        Effect? activeTechnique = GetActiveTechniqueEffect(monk);

        if (activeTechnique is null) return;

        // Check if gear restricts technique use
        string techniqueName = GetTechniqueNameByTag(activeTechnique.Tag);

        if (PreventMartialTechnique(monk, techniqueName)) return;

        ResetTechniqueCooldownAndCounter(monk);
    }

    /// <summary>
    ///     Applies the martial technique effects and cooldown on hit
    /// </summary>
    private void OnHitApplyTechnique(OnCreatureAttack attackData)
    {
        NwCreature monk = attackData.Attacker;
        if (attackData.Attacker.GetClassInfo(ClassType.Monk) is null) return;

        if (monk.ActiveEffects.Any(effect => effect.Tag is MartialCooldownTag)) return;

        bool isHit = attackData.AttackResult is AttackResult.Hit or AttackResult.AutomaticHit
            or AttackResult.CriticalHit or AttackResult.DevastatingCritical;

        if (!isHit) return;

        string? techniqueTag = monk.ActiveEffects
            .Where(effect => effect.Tag is StunningTag or EagleTag or AxiomaticTag)
            .Select(effect => effect.Tag)
            .FirstOrDefault();

        TechniqueType? techniqueType = GetTechniqueByTag(techniqueTag);
        if (techniqueType is null) return;

        ITechnique? techniqueHandler = _techniqueFactory.GetTechnique(techniqueType.Value);
        techniqueHandler?.HandleAttackTechnique(monk, attackData);

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
        if (!eventData.Object.IsPlayerControlled(out NwPlayer? player)) return;
        if (eventData.Effect.Tag is not (StunningTag or EagleTag or AxiomaticTag)) return;

        string techniqueName = GetTechniqueNameByTag(eventData.Effect.Tag);

        player.FloatingTextString($"*{techniqueName} Activated*", false, false);
    }

    /// <summary>
    ///     Cues the deactivation of the martial technique with a floaty text
    /// </summary>
    private static void CueMartialTechniqueDeactivated(OnEffectRemove eventData)
    {
        if (!eventData.Object.IsPlayerControlled(out NwPlayer? player)) return;
        if (eventData.Effect.Tag is not (StunningTag or EagleTag or AxiomaticTag)) return;

        string techniqueName = GetTechniqueNameByTag(eventData.Effect.Tag);

        player.FloatingTextString($"*{techniqueName} Deactivated*", false, false);
    }

    private static bool PreventMartialTechnique(NwCreature monk, string techniqueName)
    {
        bool hasRangedWeapon = false;
        bool hasArmor = monk.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;
        bool hasShield = monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category == BaseItemCategory.Shield;
        bool hasFocusWithoutUnarmed =
            monk.GetItemInSlot(InventorySlot.RightHand) != null
            && monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category == BaseItemCategory.Torches;


        if (MonkUtils.GetMonkPath(monk) != PathType.HiddenSpring && monk.IsRangedWeaponEquipped)
            hasRangedWeapon = true;

        if (!monk.IsPlayerControlled(out NwPlayer? player))
            return hasArmor || hasShield || hasFocusWithoutUnarmed || hasRangedWeapon;

        if (hasArmor)
        {
            player.SendServerMessage($"Wearing an armor has prevented your {techniqueName}.");
            return true;
        }

        if (hasShield)
        {
            player.SendServerMessage($"Wielding a shield has prevented your {techniqueName}.");
            return true;
        }

        if (hasFocusWithoutUnarmed)
        {
            player.SendServerMessage($"Wielding a focus without being unarmed has prevented your {techniqueName}.");
            return true;
        }

        if (hasRangedWeapon)
        {
            player.SendServerMessage($"Wielding a ranged weapon has prevented your {techniqueName}.");
            return true;
        }
        
        return false;
    }

    private static TechniqueType? GetTechniqueByTag(string? techniqueTag)
        => techniqueTag switch
        {
            StunningTag => TechniqueType.Stunning,
            EagleTag => TechniqueType.Eagle,
            AxiomaticTag => TechniqueType.Axiomatic,
            _ => null
        };

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

    private static string GetTechniqueNameByTag(string? techniqueTag)
        => techniqueTag switch
        {
            StunningTag => "Stunning Strike",
            EagleTag => "Eagle Strike",
            AxiomaticTag => "Axiomatic Strike",
            _ => ""
        };
}

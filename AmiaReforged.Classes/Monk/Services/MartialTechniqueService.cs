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
    private const string StunningTag = "Stunning Strike";
    private const string EagleTag = "Eagle Strike";
    private const string AxiomaticTag = "Axiomatic Strike";
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
        NwModule.Instance.OnCreatureAttack += OnHitApplyTechnique;
        NwModule.Instance.OnEffectApply += CueMartialTechniqueActivated;
        NwModule.Instance.OnEffectRemove += CueMartialTechniqueDeactivated;
        Log.Info(message: "Monk Martial Technique Service initialized.");
    }

    private record TechniqueInfo(int FeatId, string Tag, TechniqueType Type);

    private static readonly List<TechniqueInfo> Techniques =
    [
        new(MonkFeat.StunningStrike, StunningTag, TechniqueType.StunningStrike),
        new(MonkFeat.EagleStrike, EagleTag, TechniqueType.EagleStrike),
        new(MonkFeat.AxiomaticStrike, AxiomaticTag, TechniqueType.AxiomaticStrike)
    ];

    private static readonly Dictionary<int, TechniqueInfo> FeatIdToTechnique;
    private static readonly Dictionary<string, TechniqueInfo> TagToTechnique;

    static MartialTechniqueService()
    {
        FeatIdToTechnique = Techniques.ToDictionary(tech => tech.FeatId);
        TagToTechnique = Techniques.ToDictionary(tech => tech.Tag);
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
        if (!FeatIdToTechnique.TryGetValue(techniqueId, out TechniqueInfo? techInfo))
            return;

        string newTechniqueTag = techInfo.Tag;

        Effect? activeTechnique = GetActiveTechniqueEffect(monk);

        // If the old technique was the same as the new one, we're just toggling it off.
        if (activeTechnique?.Tag == newTechniqueTag)
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

        string? activeTechniqueTag = GetActiveTechniqueEffect(monk)?.Tag;

        if (activeTechniqueTag == null) return;

        if (PreventMartialTechnique(monk, activeTechniqueTag)) return;

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

        string? techniqueTag = GetActiveTechniqueEffect(monk)?.Tag;

        if (techniqueTag == null || !TagToTechnique.TryGetValue(techniqueTag, out TechniqueInfo? techInfo))
            return;

        ITechnique? techniqueHandler = _techniqueFactory.GetTechnique(techInfo.Type);
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

        player.FloatingTextString($"*{eventData.Effect.Tag} Activated*", false, false);
    }

    /// <summary>
    ///     Cues the deactivation of the martial technique with a floaty text
    /// </summary>
    private static void CueMartialTechniqueDeactivated(OnEffectRemove eventData)
    {
        if (!eventData.Object.IsPlayerControlled(out NwPlayer? player)) return;
        if (eventData.Effect.Tag is not (StunningTag or EagleTag or AxiomaticTag)) return;

        player.FloatingTextString($"*{eventData.Effect.Tag} Deactivated*", false, false);
    }

    private static bool PreventMartialTechnique(NwCreature monk, string techniqueName)
    {
        bool hasArmor = monk.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;
        bool hasShield = monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category == BaseItemCategory.Shield;
        bool hasFocusWithoutUnarmed =
            monk.GetItemInSlot(InventorySlot.RightHand) != null
            && monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category == BaseItemCategory.Torches;
        bool hasRangedWeapon = MonkUtils.GetMonkPath(monk) != PathType.FloatingLeaf && monk.IsRangedWeaponEquipped;

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

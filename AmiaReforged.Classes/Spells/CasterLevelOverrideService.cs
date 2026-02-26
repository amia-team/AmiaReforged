using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;
using NLog;

namespace AmiaReforged.Classes.Spells;

[ServiceBinding(typeof(CasterLevelOverrideService))]
public class CasterLevelOverrideService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly ShifterDcService _shifterDcService;

    // Epic Caster feat ID - granted when caster level override reaches 20+
    private const ushort EpicCasterFeatId = 1370;

    // Classes eligible to receive the Epic Caster feat when their CL reaches 20+
    private static readonly HashSet<ClassType> EpicCasterEligibleClasses = new()
    {
        ClassType.Bard, ClassType.Cleric, ClassType.Druid,
        ClassType.Paladin, ClassType.Ranger, ClassType.Sorcerer, ClassType.Wizard
    };

    private readonly Dictionary<NwCreature, bool> _casterLevelOverridesApplied = new();


    public CasterLevelOverrideService(ShifterDcService shifterDcService)
    {
        _shifterDcService = shifterDcService;

        NwModule.Instance.OnClientLeave += RemoveSetup;
        NwModule.Instance.OnClientEnter += FixCasterLevelOnClientEnter;

        NwModule.Instance.OnLevelUp += FixCasterLevelOnLevelUp;
        NwModule.Instance.OnLevelDown += FixCasterLevelOnLevelDown;
        NwModule.Instance.OnSpellCast += FixCasterLevelOverride;
    }

    /// <summary>
    /// Gets the effective caster level for a creature, accounting for Shifter forms.
    /// Use this method when calculating spell durations or effects for polymorphed Shifters.
    /// </summary>
    /// <param name="creature">The creature to check</param>
    /// <param name="fallbackCasterLevel">Optional fallback (0 means use creature's normal caster level)</param>
    /// <returns>The effective caster level</returns>
    public int GetEffectiveCasterLevel(NwCreature creature, int fallbackCasterLevel = 0)
    {
        return _shifterDcService.GetShifterCasterLevel(creature, fallbackCasterLevel);
    }

    private void RemoveSetup(ModuleEvents.OnClientLeave obj)
    {
        NwCreature? playerLoginCreature = obj.Player.LoginCreature;
        if (playerLoginCreature is null) return;

        _casterLevelOverridesApplied.Remove(playerLoginCreature);
    }

    private void FixCasterLevelOverride(OnSpellCast obj)
    {
        if (!obj.Caster.IsPlayerControlled(out NwPlayer? player)) return;
        if (player.LoginCreature is null) return;
        DoCasterLevelOverride(player.LoginCreature);
    }

    private void FixCasterLevelOnLevelDown(OnLevelDown obj)
    {
        DoCasterLevelOverride(obj.Creature);
    }

    private void FixCasterLevelOnLevelUp(OnLevelUp obj)
    {
        DoCasterLevelOverride(obj.Creature);
    }

    private void FixCasterLevelOnClientEnter(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.LoginCreature is null) return;
        DoCasterLevelOverride(obj.Player.LoginCreature);
    }

    private void DoCasterLevelOverride(NwCreature casterCreature)
    {
        // Use the shared calculator to get all effective caster levels
        var effectiveLevels = EffectiveCasterLevelCalculator.CalculateAllEffectiveCasterLevels(casterCreature);

        // Build a map of actual class levels for comparison
        Dictionary<ClassType, int> actualLevels = new();
        foreach (CreatureClassInfo charClass in casterCreature.Classes)
        {
            actualLevels[charClass.Class.ClassType] = charClass.Level;
        }

        // Check if any eligible base caster class has 20+ levels (qualifies for Epic Caster without PRC stacking)
        bool qualifiesForEpicCaster = actualLevels
            .Any(kvp => EpicCasterEligibleClasses.Contains(kvp.Key) && kvp.Value >= 20);

        // Apply caster level overrides and check for Epic Caster qualification
        foreach ((ClassType targetClass, int effectiveLevel) in effectiveLevels)
        {
            int actualLevel = actualLevels.TryGetValue(targetClass, out int al) ? al : 0;

            // Only set override if effective level is different from actual
            if (effectiveLevel != actualLevel)
            {
                int finalCasterLevel = Math.Max(1, effectiveLevel);

                Log.Info($"{casterCreature.Name}: Setting caster level override for {targetClass} = {finalCasterLevel} (actual {actualLevel})");

                CreaturePlugin.SetCasterLevelOverride(casterCreature, (int)targetClass, finalCasterLevel);
            }

            // Check if this class qualifies for Epic Caster feat
            if (effectiveLevel >= 20 && EpicCasterEligibleClasses.Contains(targetClass))
            {
                qualifiesForEpicCaster = true;
            }
        }

        // Handle Epic Caster feat (granted for 20+ base class levels OR 20+ CL via PRC stacking)
        bool hasEpicCasterFeat = CreaturePlugin.GetKnowsFeat(casterCreature, EpicCasterFeatId) == 1;

        if (qualifiesForEpicCaster && !hasEpicCasterFeat)
        {
            // Add the feat at the current level
            // NWN will automatically remove it if they delevel below this level
            int currentLevel = casterCreature.Level;
            CreaturePlugin.AddFeatByLevel(casterCreature, EpicCasterFeatId, currentLevel);
            Log.Info($"{casterCreature.Name}: Added Epic Caster feat (ID {EpicCasterFeatId}) at level {currentLevel} (CL >= 20)");
        }

        _casterLevelOverridesApplied[casterCreature] = true;
    }
}

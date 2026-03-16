using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

/// <summary>
/// Manages knowledge point progression through the economy system.
/// 
/// Core flow:
/// 1. Character crafts/harvests → awarded progression points
/// 2. Points accumulate toward a threshold (cost for next economy KP)
/// 3. When threshold is met, economy KP increments and leftover points carry over
/// 4. Cost escalates per the configured curve; soft cap makes it tedious; hard cap blocks
/// </summary>
[ServiceBinding(typeof(IKnowledgeProgressionService))]
public class KnowledgeProgressionService : IKnowledgeProgressionService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IKnowledgeProgressionRepository _progressionRepository;
    private readonly IKnowledgeCapProfileRepository _capProfileRepository;
    private readonly IWorldConfigProvider _configProvider;
    private readonly IEventBus _eventBus;

    public KnowledgeProgressionService(
        IKnowledgeProgressionRepository progressionRepository,
        IKnowledgeCapProfileRepository capProfileRepository,
        IWorldConfigProvider configProvider,
        IEventBus eventBus)
    {
        _progressionRepository = progressionRepository;
        _capProfileRepository = capProfileRepository;
        _configProvider = configProvider;
        _eventBus = eventBus;
    }

    public KnowledgeProgression GetProgression(CharacterId characterId)
    {
        return _progressionRepository.GetOrCreate(characterId.Value);
    }

    public ProgressionResult AwardProgressionPoints(CharacterId characterId, int points)
    {
        if (points <= 0)
        {
            return new ProgressionResult
            {
                Success = true,
                KnowledgePointsEarned = 0,
                Message = "No progression points to award."
            };
        }

        KnowledgeProgression progression = _progressionRepository.GetOrCreate(characterId.Value);
        int effectiveSoftCap = GetEffectiveSoftCap(progression);
        int effectiveHardCap = GetEffectiveHardCap(progression);

        // Already at hard cap
        if (progression.EconomyEarnedKnowledgePoints >= effectiveHardCap)
        {
            return ProgressionResult.Blocked(
                "You have reached the knowledge point hard cap. No more economy knowledge points can be earned.");
        }

        ProgressionCurveConfig curve = GetCurveConfig();
        int knowledgePointsEarned = 0;

        progression.AccumulatedProgressionPoints += points;

        // Roll over accumulated points into economy KP as long as threshold is met
        while (progression.EconomyEarnedKnowledgePoints < effectiveHardCap)
        {
            int nextKpNumber = progression.EconomyEarnedKnowledgePoints + 1;
            int costForNext = curve.CostForNthPoint(nextKpNumber, effectiveSoftCap, effectiveHardCap);

            if (costForNext == int.MaxValue)
                break; // Hard-capped

            if (progression.AccumulatedProgressionPoints < costForNext)
                break; // Not enough accumulated points yet

            // Award the KP
            progression.AccumulatedProgressionPoints -= costForNext;
            progression.EconomyEarnedKnowledgePoints++;
            knowledgePointsEarned++;

            Log.Info(
                $"Character {characterId.Value} earned economy KP #{progression.EconomyEarnedKnowledgePoints} " +
                $"(cost: {costForNext} progression points)");

            // Publish event
            PublishKnowledgePointEarned(characterId, progression, effectiveSoftCap, effectiveHardCap);
        }

        _progressionRepository.Update(progression);

        bool isAtSoftCap = progression.EconomyEarnedKnowledgePoints >= effectiveSoftCap;
        bool isAtHardCap = progression.EconomyEarnedKnowledgePoints >= effectiveHardCap;

        int nextCost = isAtHardCap
            ? int.MaxValue
            : curve.CostForNthPoint(progression.EconomyEarnedKnowledgePoints + 1, effectiveSoftCap, effectiveHardCap);

        string? message = null;
        if (knowledgePointsEarned > 0)
        {
            message = $"Earned {knowledgePointsEarned} knowledge point{(knowledgePointsEarned > 1 ? "s" : "")}!";
            if (isAtHardCap)
                message += " You have reached the knowledge point cap.";
            else if (isAtSoftCap)
                message += " Further progress will be significantly slower.";
        }

        return new ProgressionResult
        {
            Success = true,
            KnowledgePointsEarned = knowledgePointsEarned,
            NewEconomyKnowledgePointTotal = progression.EconomyEarnedKnowledgePoints,
            NewTotalKnowledgePoints = progression.TotalKnowledgePoints,
            ProgressionPointsRemaining = progression.AccumulatedProgressionPoints,
            ProgressionPointsRequired = nextCost,
            IsAtSoftCap = isAtSoftCap,
            IsAtHardCap = isAtHardCap,
            Message = message
        };
    }

    public void GrantLevelUpKnowledgePoint(CharacterId characterId)
    {
        KnowledgeProgression progression = _progressionRepository.GetOrCreate(characterId.Value);

        if (progression.LevelUpKnowledgePoints >= 30)
        {
            Log.Warn($"Character {characterId.Value} already has max level-up KP (30). Ignoring grant.");
            return;
        }

        progression.LevelUpKnowledgePoints++;
        _progressionRepository.Update(progression);

        Log.Info($"Character {characterId.Value} granted level-up KP. " +
                 $"Total level-up KP: {progression.LevelUpKnowledgePoints}, " +
                 $"Total KP: {progression.TotalKnowledgePoints}");
    }

    public int GetEffectiveSoftCap(CharacterId characterId)
    {
        KnowledgeProgression progression = _progressionRepository.GetOrCreate(characterId.Value);
        return GetEffectiveSoftCap(progression);
    }

    public int GetEffectiveHardCap(CharacterId characterId)
    {
        KnowledgeProgression progression = _progressionRepository.GetOrCreate(characterId.Value);
        return GetEffectiveHardCap(progression);
    }

    public int GetProgressionCostForNextPoint(CharacterId characterId)
    {
        KnowledgeProgression progression = _progressionRepository.GetOrCreate(characterId.Value);
        int effectiveSoftCap = GetEffectiveSoftCap(progression);
        int effectiveHardCap = GetEffectiveHardCap(progression);

        if (progression.EconomyEarnedKnowledgePoints >= effectiveHardCap)
            return int.MaxValue;

        ProgressionCurveConfig curve = GetCurveConfig();
        return curve.CostForNthPoint(progression.EconomyEarnedKnowledgePoints + 1, effectiveSoftCap, effectiveHardCap);
    }

    public ProgressionCurveConfig GetCurveConfig()
    {
        ProgressionCurveConfig config = new();

        int? baseCost = _configProvider.GetInt(WorldConstants.KnowledgeProgressionBaseCost);
        if (baseCost.HasValue) config.BaseCost = baseCost.Value;

        float? scaling = _configProvider.GetFloat(WorldConstants.KnowledgeProgressionScalingFactor);
        if (scaling.HasValue) config.ScalingFactor = scaling.Value;

        string? curveType = _configProvider.GetString(WorldConstants.KnowledgeProgressionCurveType);
        if (curveType != null && Enum.TryParse<ProgressionCurveType>(curveType, true, out ProgressionCurveType parsed))
            config.CurveType = parsed;

        int? softCap = _configProvider.GetInt(WorldConstants.KnowledgePointDefaultSoftCap);
        if (softCap.HasValue) config.SoftCap = softCap.Value;

        int? hardCap = _configProvider.GetInt(WorldConstants.KnowledgePointDefaultHardCap);
        if (hardCap.HasValue) config.HardCap = hardCap.Value;

        float? penalty = _configProvider.GetFloat(WorldConstants.KnowledgeSoftCapPenaltyMultiplier);
        if (penalty.HasValue) config.SoftCapPenaltyMultiplier = penalty.Value;

        return config;
    }

    // === Private helpers ===

    private int GetEffectiveSoftCap(KnowledgeProgression progression)
    {
        if (!string.IsNullOrEmpty(progression.CapProfileTag))
        {
            KnowledgeCapProfile? profile = _capProfileRepository.GetByTag(progression.CapProfileTag);
            if (profile != null) return profile.SoftCap;
        }

        ProgressionCurveConfig config = GetCurveConfig();
        return config.SoftCap;
    }

    private int GetEffectiveHardCap(KnowledgeProgression progression)
    {
        if (!string.IsNullOrEmpty(progression.CapProfileTag))
        {
            KnowledgeCapProfile? profile = _capProfileRepository.GetByTag(progression.CapProfileTag);
            if (profile != null) return profile.HardCap;
        }

        ProgressionCurveConfig config = GetCurveConfig();
        return config.HardCap;
    }

    private void PublishKnowledgePointEarned(
        CharacterId characterId,
        KnowledgeProgression progression,
        int effectiveSoftCap,
        int effectiveHardCap)
    {
        KnowledgePointEarnedEvent evt = new(
            characterId,
            progression.EconomyEarnedKnowledgePoints,
            progression.TotalKnowledgePoints,
            progression.EconomyEarnedKnowledgePoints >= effectiveSoftCap,
            progression.EconomyEarnedKnowledgePoints >= effectiveHardCap,
            DateTime.UtcNow);

        _eventBus.PublishAsync(evt).GetAwaiter().GetResult();
    }
}

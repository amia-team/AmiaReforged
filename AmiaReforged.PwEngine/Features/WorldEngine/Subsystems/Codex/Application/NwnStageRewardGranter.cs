using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;

/// <summary>
/// Concrete implementation of <see cref="IStageRewardGranter"/> that applies quest-stage
/// rewards to a live NWN character via Anvil APIs and WorldEngine subsystem services.
/// </summary>
[ServiceBinding(typeof(IStageRewardGranter))]
public class NwnStageRewardGranter : IStageRewardGranter
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly RuntimeCharacterService _runtimeCharacterService;
    private readonly ICharacterRepository _characterRepository;
    private readonly IIndustryMembershipService _membershipService;
    private readonly IProficiencyProgressionService _proficiencyService;

    public NwnStageRewardGranter(
        RuntimeCharacterService runtimeCharacterService,
        ICharacterRepository characterRepository,
        IIndustryMembershipService membershipService,
        IProficiencyProgressionService proficiencyService)
    {
        _runtimeCharacterService = runtimeCharacterService;
        _characterRepository = characterRepository;
        _membershipService = membershipService;
        _proficiencyService = proficiencyService;
    }

    /// <inheritdoc />
    public Task GrantRewardsAsync(CharacterId characterId, QuestId questId, int completedStageId, RewardMix rewards)
    {
        if (rewards.IsEmpty) return Task.CompletedTask;

        // Resolve the NWN player — if offline, rewards are lost (quest stage events
        // are only emitted while the player is connected).
        if (!_runtimeCharacterService.TryGetPlayer(characterId.Value, out NwPlayer? player)
            || player?.LoginCreature is null)
        {
            Log.Warn("Cannot grant stage {StageId} rewards for quest '{QuestId}': " +
                      "character {CharacterId} is not online.",
                completedStageId, questId.Value, characterId.Value);
            return Task.CompletedTask;
        }

        NwCreature creature = player.LoginCreature;

        GrantXp(creature, rewards.Xp, questId, completedStageId);
        GrantGold(creature, rewards.Gold, questId, completedStageId);
        GrantKnowledgePoints(characterId, rewards.KnowledgePoints, questId, completedStageId);
        GrantProficiencyXp(characterId, rewards.Proficiencies, questId, completedStageId);

        Log.Info("Granted stage {StageId} rewards for quest '{QuestId}' to {CharacterId}: " +
                 "XP={Xp}, Gold={Gold}, KP={KP}, Proficiencies={ProfCount}",
            completedStageId, questId.Value, characterId.Value,
            rewards.Xp, rewards.Gold, rewards.KnowledgePoints, rewards.Proficiencies.Count);

        return Task.CompletedTask;
    }

    private static void GrantXp(NwCreature creature, int xp, QuestId questId, int stageId)
    {
        if (xp <= 0) return;
        creature.Xp += xp;
    }

    private static void GrantGold(NwCreature creature, int gold, QuestId questId, int stageId)
    {
        if (gold <= 0) return;
        creature.GiveGold(gold);
    }

    private void GrantKnowledgePoints(CharacterId characterId, int points, QuestId questId, int stageId)
    {
        if (points <= 0) return;

        ICharacter? character = _characterRepository.GetById(characterId.Value);
        if (character is null)
        {
            Log.Warn("Cannot grant knowledge points for quest '{QuestId}' stage {StageId}: " +
                      "character {CharacterId} not found in repository.",
                questId.Value, stageId, characterId.Value);
            return;
        }

        character.AddKnowledgePoints(points);
    }

    private void GrantProficiencyXp(
        CharacterId characterId,
        List<ProficiencyReward> proficiencies,
        QuestId questId,
        int stageId)
    {
        if (proficiencies.Count == 0) return;

        List<IndustryMembership> memberships = _membershipService.GetMemberships(characterId.Value);

        foreach (ProficiencyReward profReward in proficiencies)
        {
            IndustryTag tag = new(profReward.IndustryTag);
            IndustryMembership? membership = memberships.FirstOrDefault(m => m.IndustryTag == tag);

            if (membership is null)
            {
                Log.Warn("Cannot grant proficiency XP for industry '{IndustryTag}' " +
                          "(quest '{QuestId}' stage {StageId}): " +
                          "character {CharacterId} has no membership in that industry.",
                    profReward.IndustryTag, questId.Value, stageId, characterId.Value);
                continue;
            }

            ProficiencyXpResult result = _proficiencyService.AwardProficiencyXp(membership, profReward.ProficiencyXp);

            if (result.LevelsGained > 0)
            {
                Log.Info("Character {CharacterId} gained {Levels} proficiency level(s) in '{IndustryTag}' " +
                         "from quest '{QuestId}' stage {StageId} reward.",
                    characterId.Value, result.LevelsGained, profReward.IndustryTag,
                    questId.Value, stageId);
            }
        }
    }
}

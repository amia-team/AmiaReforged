using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;

/// <summary>
/// Concrete implementation of <see cref="IStageRewardGranter"/> that applies quest-stage
/// rewards to a live NWN character via Anvil APIs and WorldEngine subsystem services.
/// Proficiency XP is awarded through the <see cref="AwardProficiencyCommand"/> dispatch
/// boundary (which loads the membership, calls the calculator, persists, and publishes
/// <see cref="ProficiencyXpAwardedEvent"/>); the granter itself only needs the repository
/// to resolve memberships and the dispatcher to route the award.
/// </summary>
[ServiceBinding(typeof(IStageRewardGranter))]
public class NwnStageRewardGranter : IStageRewardGranter
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly RuntimeCharacterService _runtimeCharacterService;
    private readonly ICharacterRepository _characterRepository;
    private readonly IIndustryMembershipRepository _membershipRepository;
    private readonly ICommandDispatcher _commandDispatcher;

    public NwnStageRewardGranter(
        RuntimeCharacterService runtimeCharacterService,
        ICharacterRepository characterRepository,
        IIndustryMembershipRepository membershipRepository,
        ICommandDispatcher commandDispatcher)
    {
        _runtimeCharacterService = runtimeCharacterService;
        _characterRepository = characterRepository;
        _membershipRepository = membershipRepository;
        _commandDispatcher = commandDispatcher;
    }

    /// <inheritdoc />
    public async Task GrantRewardsAsync(CharacterId characterId, QuestId questId, int completedStageId, RewardMix rewards)
    {
        if (rewards.IsEmpty) return;

        // NWN VM calls (creature.Xp, GiveGold, etc.) MUST run on the server main
        // thread.  Callers like CodexSubsystem.SetQuestStageAsync and the
        // CodexEventProcessor channel loop are executed on thread-pool threads after
        // async DB work, so we must hop back first.
        await NwTask.SwitchToMainThread();

        // Resolve the NWN player — if offline, rewards are lost (quest stage events
        // are only emitted while the player is connected).
        if (!_runtimeCharacterService.TryGetPlayer(characterId.Value, out NwPlayer? player)
            || player?.LoginCreature is null)
        {
            Log.Warn("Cannot grant stage {StageId} rewards for quest '{QuestId}': " +
                      "character {CharacterId} is not online.",
                completedStageId, questId.Value, characterId.Value);
            return;
        }

        NwCreature creature = player.LoginCreature;

        GrantXp(creature, rewards.Xp, questId, completedStageId);
        GrantGold(creature, rewards.Gold, questId, completedStageId);
        GrantKnowledgePoints(characterId, rewards.KnowledgePoints, questId, completedStageId);
        await GrantProficiencyXp(characterId, rewards.Proficiencies, questId, completedStageId);

        Log.Info("Granted stage {StageId} rewards for quest '{QuestId}' to {CharacterId}: " +
                 "XP={Xp}, Gold={Gold}, KP={KP}, Proficiencies={ProfCount}",
            completedStageId, questId.Value, characterId.Value,
            rewards.Xp, rewards.Gold, rewards.KnowledgePoints, rewards.Proficiencies.Count);
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

    private async Task GrantProficiencyXp(
        CharacterId characterId,
        List<ProficiencyReward> proficiencies,
        QuestId questId,
        int stageId)
    {
        if (proficiencies.Count == 0) return;

        // Route each award through the dispatch boundary so it gets logging, the
        // generic CommandExecutedEvent, and the ProficiencyXpAwardedEvent for free.
        // The handler loads the membership and fails with an explicit result if the
        // character has no membership in the industry.
        foreach (ProficiencyReward profReward in proficiencies)
        {
            await _commandDispatcher.DispatchAsync(new AwardProficiencyCommand
            {
                CharacterId = characterId,
                IndustryTag = new(profReward.IndustryTag),
                Points = profReward.ProficiencyXp
            }, CancellationToken.None);
        }
    }
}

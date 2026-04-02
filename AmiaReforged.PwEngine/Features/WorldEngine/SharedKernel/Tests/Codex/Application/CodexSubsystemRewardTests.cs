using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Application;

/// <summary>
/// Tests for <see cref="CodexSubsystem.GrantFromStageRewardsAsync"/>, verifying that
/// the SetQuestStageAsync code path correctly grants from-stage rewards.
/// </summary>
[TestFixture]
public class CodexSubsystemRewardTests
{
    private StubStageRewardGranter _granter;
    private CharacterId _characterId;
    private QuestId _questId;

    [SetUp]
    public void SetUp()
    {
        _granter = new StubStageRewardGranter();
        _characterId = CharacterId.New();
        _questId = QuestId.NewId();
    }

    [Test]
    public async Task Advance_with_rewards_on_from_stage_grants_them()
    {
        // Arrange
        CodexQuestEntry entry = BuildEntry(
            new QuestStage { StageId = 10, Rewards = new RewardMix { Xp = 500 } },
            new QuestStage { StageId = 20 });

        // Act
        await CodexSubsystem.GrantFromStageRewardsAsync(
            _granter, _characterId, _questId, fromStageId: 10, toStageId: 20, entry);

        // Assert
        Assert.That(_granter.Calls, Has.Count.EqualTo(1));
        StubStageRewardGranter.GrantCall call = _granter.Calls[0];
        Assert.That(call.CharacterId, Is.EqualTo(_characterId));
        Assert.That(call.QuestId, Is.EqualTo(_questId));
        Assert.That(call.CompletedStageId, Is.EqualTo(10));
        Assert.That(call.Rewards.Xp, Is.EqualTo(500));
    }

    [Test]
    public async Task Advance_with_empty_rewards_does_not_call_granter()
    {
        // Arrange — from stage has no rewards
        CodexQuestEntry entry = BuildEntry(
            new QuestStage { StageId = 10 },
            new QuestStage { StageId = 20 });

        // Act
        await CodexSubsystem.GrantFromStageRewardsAsync(
            _granter, _characterId, _questId, fromStageId: 10, toStageId: 20, entry);

        // Assert
        Assert.That(_granter.Calls, Is.Empty);
    }

    [Test]
    public async Task Idempotent_advance_same_stage_does_not_grant()
    {
        // Arrange
        CodexQuestEntry entry = BuildEntry(
            new QuestStage { StageId = 10, Rewards = new RewardMix { Xp = 100 } });

        // Act — fromStageId == toStageId (idempotent case)
        await CodexSubsystem.GrantFromStageRewardsAsync(
            _granter, _characterId, _questId, fromStageId: 10, toStageId: 10, entry);

        // Assert
        Assert.That(_granter.Calls, Is.Empty);
    }

    [Test]
    public async Task Null_granter_does_not_throw()
    {
        // Arrange
        CodexQuestEntry entry = BuildEntry(
            new QuestStage { StageId = 10, Rewards = new RewardMix { Xp = 100 } },
            new QuestStage { StageId = 20 });

        // Act & Assert — null granter should no-op
        Assert.DoesNotThrowAsync(async () =>
            await CodexSubsystem.GrantFromStageRewardsAsync(
                null, _characterId, _questId, fromStageId: 10, toStageId: 20, entry));
    }

    [Test]
    public async Task From_stage_not_found_does_not_call_granter()
    {
        // Arrange — only stage 20 exists, from stage 10 is missing
        CodexQuestEntry entry = BuildEntry(
            new QuestStage { StageId = 20 });

        // Act
        await CodexSubsystem.GrantFromStageRewardsAsync(
            _granter, _characterId, _questId, fromStageId: 10, toStageId: 20, entry);

        // Assert
        Assert.That(_granter.Calls, Is.Empty);
    }

    [Test]
    public async Task Gold_and_xp_rewards_are_forwarded()
    {
        // Arrange
        CodexQuestEntry entry = BuildEntry(
            new QuestStage { StageId = 10, Rewards = new RewardMix { Xp = 200, Gold = 50 } },
            new QuestStage { StageId = 20 });

        // Act
        await CodexSubsystem.GrantFromStageRewardsAsync(
            _granter, _characterId, _questId, fromStageId: 10, toStageId: 20, entry);

        // Assert
        Assert.That(_granter.Calls, Has.Count.EqualTo(1));
        RewardMix rewards = _granter.Calls[0].Rewards;
        Assert.That(rewards.Xp, Is.EqualTo(200));
        Assert.That(rewards.Gold, Is.EqualTo(50));
    }

    [Test]
    public async Task Proficiency_rewards_are_forwarded()
    {
        // Arrange
        List<ProficiencyReward> profRewards =
        [
            new ProficiencyReward { IndustryTag = "smithing", ProficiencyXp = 100 }
        ];

        CodexQuestEntry entry = BuildEntry(
            new QuestStage { StageId = 10, Rewards = new RewardMix { Proficiencies = profRewards } },
            new QuestStage { StageId = 20 });

        // Act
        await CodexSubsystem.GrantFromStageRewardsAsync(
            _granter, _characterId, _questId, fromStageId: 10, toStageId: 20, entry);

        // Assert
        Assert.That(_granter.Calls, Has.Count.EqualTo(1));
        Assert.That(_granter.Calls[0].Rewards.Proficiencies, Has.Count.EqualTo(1));
        Assert.That(_granter.Calls[0].Rewards.Proficiencies[0].IndustryTag, Is.EqualTo("smithing"));
        Assert.That(_granter.Calls[0].Rewards.Proficiencies[0].ProficiencyXp, Is.EqualTo(100));
    }

    #region Helpers

    private CodexQuestEntry BuildEntry(params QuestStage[] stages)
    {
        return new CodexQuestEntry
        {
            QuestId = _questId,
            Title = "Test Quest",
            Description = "A test quest",
            DateStarted = DateTime.UtcNow,
            Keywords = new List<Keyword>(),
            Stages = stages.ToList()
        };
    }

    #endregion
}

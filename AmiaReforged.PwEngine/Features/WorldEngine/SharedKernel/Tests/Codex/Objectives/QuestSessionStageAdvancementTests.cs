using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Objectives;

[TestFixture]
public class QuestSessionStageAdvancementTests
{
    private ObjectiveEvaluatorRegistry _registry = null!;
    private CharacterId _characterId;
    private QuestId _questId;
    private DateTime _testDate;

    [SetUp]
    public void SetUp()
    {
        _registry = new ObjectiveEvaluatorRegistry(new IObjectiveEvaluator[]
        {
            new KillObjectiveEvaluator(),
            new CollectObjectiveEvaluator(),
            new ReachLocationObjectiveEvaluator()
        });

        _characterId = CharacterId.New();
        _questId = new QuestId("stage_adv_quest");
        _testDate = new DateTime(2026, 3, 25, 12, 0, 0);
    }

    #region Backward Compatibility

    [Test]
    public void No_stage_context_does_not_emit_QuestStageAdvancedEvent()
    {
        // Given a session WITHOUT stage context (backward compatible)
        QuestObjectiveGroup group = new()
        {
            DisplayName = "Kill Boss",
            Objectives = [CreateKillDef("boss", 1)]
        };

        QuestSession session = new(_questId, _characterId, [group], _registry, _testDate);

        // When the objective completes (fully completing the group)
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.CreatureKilled, "boss"));

        // Then a GroupCompletedEvent is emitted but NO QuestStageAdvancedEvent
        Assert.That(events.OfType<QuestObjectiveGroupCompletedEvent>().Count(), Is.EqualTo(1));
        Assert.That(events.OfType<QuestStageAdvancedEvent>().Count(), Is.EqualTo(0));
    }

    #endregion

    #region Numeric Fallback Advancement

    [Test]
    public void All_groups_done_advances_to_next_numeric_stage_when_no_explicit_override()
    {
        // Given stages 10, 20, 30 and session on stage 10 with a single objective
        List<QuestStage> stages = CreateThreeStages();
        QuestObjectiveGroup group = stages[0].ObjectiveGroups[0];

        StageContext ctx = new(stages, 10);
        QuestSession session = new(_questId, _characterId,
            stages[0].ObjectiveGroups, _registry, _testDate, stageContext: ctx);

        // When the objective completes
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.CreatureKilled, "goblin"));

        // Then the session advances to stage 20
        QuestStageAdvancedEvent adv = events.OfType<QuestStageAdvancedEvent>().Single();
        Assert.That(adv.FromStage, Is.EqualTo(10));
        Assert.That(adv.ToStage, Is.EqualTo(20));
        Assert.That(session.CurrentStageId, Is.EqualTo(20));
    }

    [Test]
    public void Last_stage_does_not_advance_when_no_further_stages_exist()
    {
        // Given only one stage (stage 10)
        QuestStage singleStage = new()
        {
            StageId = 10,
            ObjectiveGroups = [new QuestObjectiveGroup
            {
                DisplayName = "Final Task",
                Objectives = [CreateKillDef("boss", 1)]
            }]
        };

        StageContext ctx = new([singleStage], 10);
        QuestSession session = new(_questId, _characterId,
            singleStage.ObjectiveGroups, _registry, _testDate, stageContext: ctx);

        // When the objective completes
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.CreatureKilled, "boss"));

        // Then no stage advancement event
        Assert.That(events.OfType<QuestStageAdvancedEvent>().Count(), Is.EqualTo(0));
        Assert.That(session.CurrentStageId, Is.EqualTo(10));
    }

    #endregion

    #region Explicit NextStageId

    [Test]
    public void Stage_NextStageId_overrides_numeric_fallback()
    {
        // Given stages 10 (NextStageId=30), 20, 30
        List<QuestStage> stages = CreateThreeStages();
        // Override stage 10 to explicitly point to stage 30, skipping 20
        QuestStage stage10 = new()
        {
            StageId = 10,
            NextStageId = 30,
            ObjectiveGroups = stages[0].ObjectiveGroups
        };
        List<QuestStage> modified = [stage10, stages[1], stages[2]];

        StageContext ctx = new(modified, 10);
        QuestSession session = new(_questId, _characterId,
            stage10.ObjectiveGroups, _registry, _testDate, stageContext: ctx);

        // When the objective completes
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.CreatureKilled, "goblin"));

        // Then it advances to 30, skipping 20
        QuestStageAdvancedEvent adv = events.OfType<QuestStageAdvancedEvent>().Single();
        Assert.That(adv.FromStage, Is.EqualTo(10));
        Assert.That(adv.ToStage, Is.EqualTo(30));
        Assert.That(session.CurrentStageId, Is.EqualTo(30));
    }

    #endregion

    #region Group CompletionStageId (Branching)

    [Test]
    public void Group_CompletionStageId_triggers_immediate_branch_on_that_group_completing()
    {
        // Given stage 10 with a group that has CompletionStageId=30
        QuestStage stage10 = new()
        {
            StageId = 10,
            ObjectiveGroups =
            [
                new QuestObjectiveGroup
                {
                    DisplayName = "Branching Group",
                    CompletionStageId = 30,
                    Objectives = [CreateCollectDef("gem", 1)]
                },
                new QuestObjectiveGroup
                {
                    DisplayName = "Normal Group",
                    Objectives = [CreateKillDef("goblin", 1)]
                }
            ]
        };
        QuestStage stage20 = new() { StageId = 20, ObjectiveGroups = [] };
        QuestStage stage30 = new() { StageId = 30, ObjectiveGroups = [] };

        List<QuestStage> stages = [stage10, stage20, stage30];
        StageContext ctx = new(stages, 10);
        QuestSession session = new(_questId, _characterId,
            stage10.ObjectiveGroups, _registry, _testDate, stageContext: ctx);

        // When only the branching group completes (normal group still incomplete)
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.ItemAcquired, "gem"));

        // Then it branches to stage 30 immediately, not waiting for all groups
        QuestStageAdvancedEvent adv = events.OfType<QuestStageAdvancedEvent>().Single();
        Assert.That(adv.FromStage, Is.EqualTo(10));
        Assert.That(adv.ToStage, Is.EqualTo(30));
        Assert.That(session.CurrentStageId, Is.EqualTo(30));
    }

    [Test]
    public void Group_CompletionStageId_takes_priority_over_stage_NextStageId()
    {
        // Given stage 10 with NextStageId=20 but a group with CompletionStageId=30
        QuestStage stage10 = new()
        {
            StageId = 10,
            NextStageId = 20,
            ObjectiveGroups =
            [
                new QuestObjectiveGroup
                {
                    DisplayName = "Branch Path",
                    CompletionStageId = 30,
                    Objectives = [CreateKillDef("boss", 1)]
                }
            ]
        };
        QuestStage stage20 = new() { StageId = 20, ObjectiveGroups = [] };
        QuestStage stage30 = new() { StageId = 30, ObjectiveGroups = [] };

        List<QuestStage> stages = [stage10, stage20, stage30];
        StageContext ctx = new(stages, 10);
        QuestSession session = new(_questId, _characterId,
            stage10.ObjectiveGroups, _registry, _testDate, stageContext: ctx);

        // When the group completes
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.CreatureKilled, "boss"));

        // Then it goes to 30 (group CompletionStageId), not 20 (stage NextStageId)
        QuestStageAdvancedEvent adv = events.OfType<QuestStageAdvancedEvent>().Single();
        Assert.That(adv.ToStage, Is.EqualTo(30));
    }

    #endregion

    #region Session Reinitializes After Advancement

    [Test]
    public void After_advancement_session_tracks_new_stage_objectives()
    {
        // Given stages 10 (kill goblin) and 20 (collect gem)
        ObjectiveDefinition killDef = CreateKillDef("goblin", 1);
        ObjectiveDefinition collectDef = CreateCollectDef("gem", 1);

        QuestStage stage10 = new()
        {
            StageId = 10,
            ObjectiveGroups = [new QuestObjectiveGroup
            {
                DisplayName = "Phase 1",
                Objectives = [killDef]
            }]
        };
        QuestStage stage20 = new()
        {
            StageId = 20,
            ObjectiveGroups = [new QuestObjectiveGroup
            {
                DisplayName = "Phase 2",
                Objectives = [collectDef]
            }]
        };

        List<QuestStage> stages = [stage10, stage20];
        StageContext ctx = new(stages, 10);
        QuestSession session = new(_questId, _characterId,
            stage10.ObjectiveGroups, _registry, _testDate, stageContext: ctx);

        // When stage 10 completes → auto-advances to stage 20
        session.ProcessSignal(new QuestSignal(SignalType.CreatureKilled, "goblin"));
        Assert.That(session.CurrentStageId, Is.EqualTo(20));

        // Then the session now tracks stage 20's collect objective
        ObjectiveState? collectState = session.GetObjectiveState(collectDef.ObjectiveId);
        Assert.That(collectState, Is.Not.Null);
        Assert.That(collectState!.IsActive, Is.True);

        // And the old kill objective is no longer tracked
        ObjectiveState? killState = session.GetObjectiveState(killDef.ObjectiveId);
        Assert.That(killState, Is.Null);
    }

    [Test]
    public void After_advancement_new_stage_signals_produce_events()
    {
        // Given stages 10 (kill goblin) and 20 (collect gem)
        ObjectiveDefinition collectDef = CreateCollectDef("gem", 1);

        QuestStage stage10 = new()
        {
            StageId = 10,
            ObjectiveGroups = [new QuestObjectiveGroup
            {
                DisplayName = "Phase 1",
                Objectives = [CreateKillDef("goblin", 1)]
            }]
        };
        QuestStage stage20 = new()
        {
            StageId = 20,
            ObjectiveGroups = [new QuestObjectiveGroup
            {
                DisplayName = "Phase 2",
                Objectives = [collectDef]
            }]
        };

        List<QuestStage> stages = [stage10, stage20];
        StageContext ctx = new(stages, 10);
        QuestSession session = new(_questId, _characterId,
            stage10.ObjectiveGroups, _registry, _testDate, stageContext: ctx);

        // Advance to stage 20
        session.ProcessSignal(new QuestSignal(SignalType.CreatureKilled, "goblin"));

        // When a collect signal arrives on the new stage
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.ItemAcquired, "gem"));

        // Then the collect objective completes
        Assert.That(events.OfType<ObjectiveCompletedEvent>().Count(), Is.EqualTo(1));
    }

    #endregion

    #region GroupCompletedEvent Carries CompletionStageId

    [Test]
    public void GroupCompletedEvent_includes_CompletionStageId_when_set()
    {
        QuestObjectiveGroup group = new()
        {
            DisplayName = "Branch Group",
            CompletionStageId = 30,
            Objectives = [CreateKillDef("boss", 1)]
        };

        QuestSession session = new(_questId, _characterId, [group], _registry, _testDate);

        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.CreatureKilled, "boss"));

        QuestObjectiveGroupCompletedEvent grpEvt = events.OfType<QuestObjectiveGroupCompletedEvent>().Single();
        Assert.That(grpEvt.CompletionStageId, Is.EqualTo(30));
    }

    [Test]
    public void GroupCompletedEvent_has_null_CompletionStageId_when_not_set()
    {
        QuestObjectiveGroup group = new()
        {
            DisplayName = "Normal Group",
            Objectives = [CreateKillDef("boss", 1)]
        };

        QuestSession session = new(_questId, _characterId, [group], _registry, _testDate);

        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.CreatureKilled, "boss"));

        QuestObjectiveGroupCompletedEvent grpEvt = events.OfType<QuestObjectiveGroupCompletedEvent>().Single();
        Assert.That(grpEvt.CompletionStageId, Is.Null);
    }

    #endregion

    #region Stage Rewards

    [Test]
    public void Completing_stage_with_rewards_emits_StageRewardsGrantedEvent()
    {
        // Given stage 10 has rewards, quest advances from 10 → 20
        RewardMix rewards = new() { Xp = 500, Gold = 200 };
        QuestStage stage10 = new()
        {
            StageId = 10,
            Rewards = rewards,
            ObjectiveGroups = [new QuestObjectiveGroup
            {
                DisplayName = "Kill Goblins",
                Objectives = [CreateKillDef("goblin", 1)]
            }]
        };
        QuestStage stage20 = new() { StageId = 20, ObjectiveGroups = [] };

        StageContext ctx = new([stage10, stage20], 10);
        QuestSession session = new(_questId, _characterId,
            stage10.ObjectiveGroups, _registry, _testDate, stageContext: ctx);

        // When the objective completes, advancing from stage 10 → 20
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.CreatureKilled, "goblin"));

        // Then a StageRewardsGrantedEvent is emitted for the completed stage (10)
        StageRewardsGrantedEvent? rewardEvent = events.OfType<StageRewardsGrantedEvent>().SingleOrDefault();
        Assert.That(rewardEvent, Is.Not.Null, "Expected StageRewardsGrantedEvent");
        Assert.That(rewardEvent!.CompletedStageId, Is.EqualTo(10));
        Assert.That(rewardEvent.Rewards.Xp, Is.EqualTo(500));
        Assert.That(rewardEvent.Rewards.Gold, Is.EqualTo(200));
        Assert.That(rewardEvent.QuestId, Is.EqualTo(_questId));
        Assert.That(rewardEvent.CharacterId, Is.EqualTo(_characterId));
    }

    [Test]
    public void Completing_stage_without_rewards_does_not_emit_StageRewardsGrantedEvent()
    {
        // Given stage 10 has empty rewards
        QuestStage stage10 = new()
        {
            StageId = 10,
            Rewards = RewardMix.Empty,
            ObjectiveGroups = [new QuestObjectiveGroup
            {
                DisplayName = "Kill Goblins",
                Objectives = [CreateKillDef("goblin", 1)]
            }]
        };
        QuestStage stage20 = new() { StageId = 20, ObjectiveGroups = [] };

        StageContext ctx = new([stage10, stage20], 10);
        QuestSession session = new(_questId, _characterId,
            stage10.ObjectiveGroups, _registry, _testDate, stageContext: ctx);

        // When
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.CreatureKilled, "goblin"));

        // Then no reward event
        Assert.That(events.OfType<StageRewardsGrantedEvent>().Count(), Is.EqualTo(0));
    }

    [Test]
    public void Reward_event_includes_proficiency_rewards()
    {
        // Given stage 10 has proficiency rewards
        RewardMix rewards = new()
        {
            Xp = 100,
            KnowledgePoints = 3,
            Proficiencies = [new ProficiencyReward { IndustryTag = "alchemy", ProficiencyXp = 50 }]
        };
        QuestStage stage10 = new()
        {
            StageId = 10,
            Rewards = rewards,
            ObjectiveGroups = [new QuestObjectiveGroup
            {
                DisplayName = "Gather Herbs",
                Objectives = [CreateCollectDef("herb", 1)]
            }]
        };
        QuestStage stage20 = new() { StageId = 20, ObjectiveGroups = [] };

        StageContext ctx = new([stage10, stage20], 10);
        QuestSession session = new(_questId, _characterId,
            stage10.ObjectiveGroups, _registry, _testDate, stageContext: ctx);

        // When
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.ItemAcquired, "herb"));

        // Then
        StageRewardsGrantedEvent rewardEvent = events.OfType<StageRewardsGrantedEvent>().Single();
        Assert.That(rewardEvent.Rewards.KnowledgePoints, Is.EqualTo(3));
        Assert.That(rewardEvent.Rewards.Proficiencies, Has.Count.EqualTo(1));
        Assert.That(rewardEvent.Rewards.Proficiencies[0].IndustryTag, Is.EqualTo("alchemy"));
        Assert.That(rewardEvent.Rewards.Proficiencies[0].ProficiencyXp, Is.EqualTo(50));
    }

    [Test]
    public void Multi_stage_advancement_emits_reward_for_each_completed_stage_with_rewards()
    {
        // Given stages 10 (rewards) → 20 (rewards) → 30 (no objectives, completion)
        // Stage 10 completes → advances to 20; if 20 has no objectives it auto-completes
        RewardMix rewards10 = new() { Xp = 100 };
        RewardMix rewards20 = new() { Gold = 50 };

        QuestStage stage10 = new()
        {
            StageId = 10,
            Rewards = rewards10,
            ObjectiveGroups = [new QuestObjectiveGroup
            {
                DisplayName = "Phase 1",
                Objectives = [CreateKillDef("goblin", 1)]
            }]
        };
        QuestStage stage20 = new()
        {
            StageId = 20,
            Rewards = rewards20,
            ObjectiveGroups = [new QuestObjectiveGroup
            {
                DisplayName = "Phase 2",
                Objectives = [CreateCollectDef("gem", 1)]
            }]
        };
        QuestStage stage30 = new() { StageId = 30, IsCompletionStage = true, ObjectiveGroups = [] };

        List<QuestStage> stages = [stage10, stage20, stage30];
        StageContext ctx = new(stages, 10);
        QuestSession session = new(_questId, _characterId,
            stage10.ObjectiveGroups, _registry, _testDate, stageContext: ctx);

        // When stage 10 completes
        IReadOnlyList<CodexDomainEvent> events1 = session.ProcessSignal(
            new QuestSignal(SignalType.CreatureKilled, "goblin"));

        // Then reward for stage 10
        StageRewardsGrantedEvent reward1 = events1.OfType<StageRewardsGrantedEvent>().Single();
        Assert.That(reward1.CompletedStageId, Is.EqualTo(10));
        Assert.That(reward1.Rewards.Xp, Is.EqualTo(100));

        // When stage 20 completes
        IReadOnlyList<CodexDomainEvent> events2 = session.ProcessSignal(
            new QuestSignal(SignalType.ItemAcquired, "gem"));

        // Then reward for stage 20
        StageRewardsGrantedEvent reward2 = events2.OfType<StageRewardsGrantedEvent>().Single();
        Assert.That(reward2.CompletedStageId, Is.EqualTo(20));
        Assert.That(reward2.Rewards.Gold, Is.EqualTo(50));
    }

    [Test]
    public void No_stage_context_does_not_emit_rewards_event()
    {
        // Given a session without stage context (backward compatible, no stages)
        QuestObjectiveGroup group = new()
        {
            DisplayName = "Kill Boss",
            Objectives = [CreateKillDef("boss", 1)]
        };

        QuestSession session = new(_questId, _characterId, [group], _registry, _testDate);

        // When
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.CreatureKilled, "boss"));

        // Then — no stage context means no reward events
        Assert.That(events.OfType<StageRewardsGrantedEvent>().Count(), Is.EqualTo(0));
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates 3 stages: 10 (kill goblin ×1), 20 (collect gem ×1), 30 (completion, no objectives).
    /// </summary>
    private List<QuestStage> CreateThreeStages()
    {
        return
        [
            new QuestStage
            {
                StageId = 10,
                ObjectiveGroups = [new QuestObjectiveGroup
                {
                    DisplayName = "Stage 10 Objectives",
                    Objectives = [CreateKillDef("goblin", 1)]
                }]
            },
            new QuestStage
            {
                StageId = 20,
                ObjectiveGroups = [new QuestObjectiveGroup
                {
                    DisplayName = "Stage 20 Objectives",
                    Objectives = [CreateCollectDef("gem", 1)]
                }]
            },
            new QuestStage
            {
                StageId = 30,
                IsCompletionStage = true,
                ObjectiveGroups = []
            }
        ];
    }

    private static ObjectiveDefinition CreateKillDef(string targetTag, int count) => new()
    {
        ObjectiveId = ObjectiveId.NewId(),
        TypeTag = "kill",
        DisplayText = $"Kill {count} {targetTag}",
        TargetTag = targetTag,
        RequiredCount = count
    };

    private static ObjectiveDefinition CreateCollectDef(string targetTag, int count) => new()
    {
        ObjectiveId = ObjectiveId.NewId(),
        TypeTag = "collect",
        DisplayText = $"Collect {count} {targetTag}",
        TargetTag = targetTag,
        RequiredCount = count
    };

    #endregion
}

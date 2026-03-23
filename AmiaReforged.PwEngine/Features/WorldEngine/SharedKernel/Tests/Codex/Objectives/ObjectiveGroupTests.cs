using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Models;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Objectives;

[TestFixture]
public class ObjectiveGroupTests
{
    #region QuestObjectiveGroup Structure

    [Test]
    public void Group_with_All_mode_defaults_correctly()
    {
        QuestObjectiveGroup group = new()
        {
            DisplayName = "Test Group",
            CompletionMode = CompletionMode.All,
            Objectives =
            [
                new ObjectiveDefinition
                {
                    ObjectiveId = ObjectiveId.NewId(),
                    TypeTag = "kill",
                    DisplayText = "Kill goblins",
                    TargetTag = "goblin",
                    RequiredCount = 3
                }
            ]
        };

        Assert.That(group.CompletionMode, Is.EqualTo(CompletionMode.All));
        Assert.That(group.Objectives, Has.Count.EqualTo(1));
        Assert.That(group.CompletionStageId, Is.Null);
    }

    [Test]
    public void Group_can_have_completion_stage_id()
    {
        QuestObjectiveGroup group = new()
        {
            DisplayName = "Phase 1",
            CompletionStageId = 20,
            Objectives =
            [
                new ObjectiveDefinition
                {
                    ObjectiveId = ObjectiveId.NewId(),
                    TypeTag = "collect",
                    DisplayText = "Collect gems",
                    TargetTag = "gem",
                    RequiredCount = 5
                }
            ]
        };

        Assert.That(group.CompletionStageId, Is.EqualTo(20));
    }

    #endregion

    #region ClueGraph Validation

    [Test]
    public void Valid_clue_graph_passes_validation()
    {
        ClueGraph graph = new()
        {
            Clues =
            [
                new Clue { ClueId = "a", Name = "A", TriggerTag = "tag_a" },
                new Clue { ClueId = "b", Name = "B", TriggerTag = "tag_b" }
            ],
            Deductions =
            [
                new Deduction
                {
                    DeductionId = "conclusion",
                    Description = "The answer",
                    RequiredClueIds = ["a", "b"]
                }
            ],
            ConclusionDeductionId = "conclusion"
        };

        ValidationResult result = graph.Validate();
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Clue_graph_with_missing_conclusion_fails_validation()
    {
        ClueGraph graph = new()
        {
            Clues = [new Clue { ClueId = "a", Name = "A", TriggerTag = "tag_a" }],
            Deductions =
            [
                new Deduction
                {
                    DeductionId = "d1", Description = "D1", RequiredClueIds = ["a"]
                }
            ],
            ConclusionDeductionId = "nonexistent"
        };

        ValidationResult result = graph.Validate();
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("nonexistent"));
    }

    [Test]
    public void Clue_graph_with_unknown_required_clue_fails_validation()
    {
        ClueGraph graph = new()
        {
            Clues = [new Clue { ClueId = "a", Name = "A", TriggerTag = "tag_a" }],
            Deductions =
            [
                new Deduction
                {
                    DeductionId = "conclusion",
                    Description = "Conclusion",
                    RequiredClueIds = ["a", "missing_clue"]
                }
            ],
            ConclusionDeductionId = "conclusion"
        };

        ValidationResult result = graph.Validate();
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("missing_clue"));
    }

    #endregion

    #region StateMachine Validation

    [Test]
    public void Valid_state_machine_passes_validation()
    {
        StateMachineDefinition sm = new()
        {
            States =
            [
                new NarrativeState { StateId = "start", Description = "Start" },
                new NarrativeState { StateId = "end", Description = "End", IsTerminalSuccess = true }
            ],
            Transitions =
            [
                new NarrativeTransition
                {
                    FromStateId = "start", ToStateId = "end",
                    SignalType = SignalType.DialogChoice, TargetTag = "finish"
                }
            ],
            InitialStateId = "start"
        };

        ValidationResult result = sm.Validate();
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void State_machine_without_terminal_states_fails_validation()
    {
        StateMachineDefinition sm = new()
        {
            States =
            [
                new NarrativeState { StateId = "start", Description = "Start" },
                new NarrativeState { StateId = "middle", Description = "Middle" }
            ],
            Transitions =
            [
                new NarrativeTransition
                {
                    FromStateId = "start", ToStateId = "middle",
                    SignalType = SignalType.DialogChoice, TargetTag = "go"
                }
            ],
            InitialStateId = "start"
        };

        ValidationResult result = sm.Validate();
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("terminal"));
    }

    [Test]
    public void State_machine_with_invalid_initial_state_fails_validation()
    {
        StateMachineDefinition sm = new()
        {
            States =
            [
                new NarrativeState { StateId = "start", Description = "Start" },
                new NarrativeState { StateId = "end", Description = "End", IsTerminalSuccess = true }
            ],
            Transitions = [],
            InitialStateId = "nonexistent"
        };

        ValidationResult result = sm.Validate();
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contain("nonexistent"));
    }

    #endregion

    #region QuestSessionManager

    [Test]
    public void SessionManager_creates_and_retrieves_sessions()
    {
        // Given a session manager
        ObjectiveEvaluatorRegistry registry = new([new KillObjectiveEvaluator()]);
        QuestSessionManager manager = new(registry);
        CharacterId charId = CharacterId.New();
        QuestId questId = new("quest_1");

        // When creating a session
        manager.CreateSession(charId, questId, [
            new QuestObjectiveGroup
            {
                DisplayName = "Kill Goblins",
                Objectives =
                [
                    new ObjectiveDefinition
                    {
                        ObjectiveId = ObjectiveId.NewId(),
                        TypeTag = "kill",
                        DisplayText = "Kill 3 goblins",
                        TargetTag = "goblin",
                        RequiredCount = 3
                    }
                ]
            }
        ]);

        // Then it can be retrieved
        Assert.That(manager.HasSession(charId, questId), Is.True);
        Assert.That(manager.GetSession(charId, questId), Is.Not.Null);
    }

    [Test]
    public void SessionManager_processes_signal_across_all_character_sessions()
    {
        // Given a manager with 2 quests for the same character
        ObjectiveEvaluatorRegistry registry = new([
            new KillObjectiveEvaluator(), new CollectObjectiveEvaluator()
        ]);
        QuestSessionManager manager = new(registry);
        CharacterId charId = CharacterId.New();

        manager.CreateSession(charId, new QuestId("quest_a"), [
            new QuestObjectiveGroup
            {
                DisplayName = "Kill Goblins",
                Objectives =
                [
                    new ObjectiveDefinition
                    {
                        ObjectiveId = ObjectiveId.NewId(),
                        TypeTag = "kill",
                        DisplayText = "Kill goblin",
                        TargetTag = "goblin",
                        RequiredCount = 1
                    }
                ]
            }
        ]);

        manager.CreateSession(charId, new QuestId("quest_b"), [
            new QuestObjectiveGroup
            {
                DisplayName = "Also Kill Goblins",
                Objectives =
                [
                    new ObjectiveDefinition
                    {
                        ObjectiveId = ObjectiveId.NewId(),
                        TypeTag = "kill",
                        DisplayText = "Kill goblin",
                        TargetTag = "goblin",
                        RequiredCount = 1
                    }
                ]
            }
        ]);

        // When a goblin kill signal is processed
        IReadOnlyList<CodexDomainEvent> events = manager.ProcessSignal(
            charId, new QuestSignal(SignalType.CreatureKilled, "goblin"));

        // Then both quests progress (each gets a completed event since both need 1 kill)
        Assert.That(events.OfType<ObjectiveCompletedEvent>().Count(), Is.EqualTo(2));
    }

    [Test]
    public void SessionManager_removes_sessions()
    {
        ObjectiveEvaluatorRegistry registry = new([new KillObjectiveEvaluator()]);
        QuestSessionManager manager = new(registry);
        CharacterId charId = CharacterId.New();
        QuestId questId = new("quest_1");

        manager.CreateSession(charId, questId, [
            new QuestObjectiveGroup
            {
                DisplayName = "Test",
                Objectives =
                [
                    new ObjectiveDefinition
                    {
                        ObjectiveId = ObjectiveId.NewId(),
                        TypeTag = "kill",
                        DisplayText = "Kill something",
                        TargetTag = "goblin",
                        RequiredCount = 1
                    }
                ]
            }
        ]);

        // When removing
        bool removed = manager.RemoveSession(charId, questId);

        // Then it's gone
        Assert.That(removed, Is.True);
        Assert.That(manager.HasSession(charId, questId), Is.False);
    }

    [Test]
    public void SessionManager_returns_empty_for_unknown_character()
    {
        ObjectiveEvaluatorRegistry registry = new([]);
        QuestSessionManager manager = new(registry);

        IReadOnlyList<CodexDomainEvent> events = manager.ProcessSignal(
            CharacterId.New(), new QuestSignal(SignalType.CreatureKilled, "goblin"));

        Assert.That(events, Is.Empty);
    }

    #endregion
}

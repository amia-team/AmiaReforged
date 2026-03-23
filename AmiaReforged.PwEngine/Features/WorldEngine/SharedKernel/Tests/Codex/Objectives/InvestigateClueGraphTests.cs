using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Models;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Objectives;

[TestFixture]
public class InvestigateClueGraphTests
{
    private InvestigateObjectiveEvaluator _evaluator = null!;

    [SetUp]
    public void SetUp()
    {
        _evaluator = new InvestigateObjectiveEvaluator();
    }

    #region Clue Discovery

    [Test]
    public void Discovering_a_clue_progresses_the_investigation()
    {
        // Given an investigation with 3 clues
        (ObjectiveDefinition definition, ObjectiveState state) = CreateClueGraphScenario();

        // When the first clue is found
        QuestSignal signal = new(SignalType.ClueFound, "bloody_knife");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then progress is reported
        Assert.That(result.StateChanged, Is.True);
        Assert.That(result.IsCompleted, Is.False);
        Assert.That(result.Message, Does.Contain("Clue discovered"));
    }

    [Test]
    public void Discovering_same_clue_twice_is_idempotent()
    {
        // Given an investigation with a discovered clue
        (ObjectiveDefinition definition, ObjectiveState state) = CreateClueGraphScenario();
        _evaluator.Evaluate(new QuestSignal(SignalType.ClueFound, "bloody_knife"), definition, state);

        // When the same clue signal fires again
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.ClueFound, "bloody_knife"), definition, state);

        // Then it's a no-op
        Assert.That(result.StateChanged, Is.False);
    }

    [Test]
    public void Unknown_clue_tag_is_ignored()
    {
        // Given an investigation
        (ObjectiveDefinition definition, ObjectiveState state) = CreateClueGraphScenario();

        // When an unknown clue signal arrives
        QuestSignal signal = new(SignalType.ClueFound, "nonexistent_clue");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then nothing happens
        Assert.That(result.StateChanged, Is.False);
    }

    [Test]
    public void Non_clue_signals_are_ignored()
    {
        // Given an investigation
        (ObjectiveDefinition definition, ObjectiveState state) = CreateClueGraphScenario();

        // When a creature killed signal arrives
        QuestSignal signal = new(SignalType.CreatureKilled, "bloody_knife");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then nothing happens
        Assert.That(result.StateChanged, Is.False);
    }

    #endregion

    #region Deduction Unlocking

    [Test]
    public void Discovering_all_prerequisite_clues_unlocks_deduction()
    {
        // Given an investigation where the conclusion needs 2 clues
        (ObjectiveDefinition definition, ObjectiveState state) = CreateSimpleTwoClueScenario();

        // When the first clue is found
        _evaluator.Evaluate(new QuestSignal(SignalType.ClueFound, "clue_a"), definition, state);

        // And the second clue is found (completing all prerequisites)
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.ClueFound, "clue_b"), definition, state);

        // Then the deduction (conclusion) is unlocked and objective completes
        Assert.That(result.IsCompleted, Is.True);
        Assert.That(state.IsCompleted, Is.True);
    }

    [Test]
    public void Partial_prerequisites_do_not_unlock_deduction()
    {
        // Given an investigation needing 2 clues for conclusion
        (ObjectiveDefinition definition, ObjectiveState state) = CreateSimpleTwoClueScenario();

        // When only 1 clue is found
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.ClueFound, "clue_a"), definition, state);

        // Then the investigation is in progress, not complete
        Assert.That(result.StateChanged, Is.True);
        Assert.That(result.IsCompleted, Is.False);
    }

    #endregion

    #region Cascading Deductions

    [Test]
    public void Deduction_can_unlock_new_clues_that_cascade_to_further_deductions()
    {
        // Given an investigation with cascading deductions:
        // clue_a + clue_b → deduction_1 (unlocks clue_c)
        // clue_c → conclusion
        ClueGraph graph = new()
        {
            Clues =
            [
                new Clue { ClueId = "c_a", Name = "Clue A", TriggerTag = "clue_a" },
                new Clue { ClueId = "c_b", Name = "Clue B", TriggerTag = "clue_b" },
                new Clue { ClueId = "c_c", Name = "Clue C (unlocked)", TriggerTag = "clue_c_manual" }
            ],
            Deductions =
            [
                new Deduction
                {
                    DeductionId = "d1",
                    Description = "First deduction",
                    RequiredClueIds = ["c_a", "c_b"],
                    UnlocksClueIds = ["c_c"]
                },
                new Deduction
                {
                    DeductionId = "conclusion",
                    Description = "The butler did it",
                    RequiredClueIds = ["c_c"]
                }
            ],
            ConclusionDeductionId = "conclusion"
        };

        ObjectiveDefinition definition = CreateDefinitionWithGraph(graph);
        ObjectiveState state = CreateState(definition);

        // When clue A is found
        _evaluator.Evaluate(new QuestSignal(SignalType.ClueFound, "clue_a"), definition, state);

        // When clue B is found (should cascade: d1 → unlock c_c → conclusion)
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.ClueFound, "clue_b"), definition, state);

        // Then the investigation completes via cascade
        Assert.That(result.IsCompleted, Is.True);
        Assert.That(state.IsCompleted, Is.True);
    }

    #endregion

    #region Terminal State

    [Test]
    public void Signals_after_completion_are_ignored()
    {
        // Given a completed investigation
        (ObjectiveDefinition definition, ObjectiveState state) = CreateSimpleTwoClueScenario();
        _evaluator.Evaluate(new QuestSignal(SignalType.ClueFound, "clue_a"), definition, state);
        _evaluator.Evaluate(new QuestSignal(SignalType.ClueFound, "clue_b"), definition, state);
        Assert.That(state.IsCompleted, Is.True);

        // When another clue signal arrives
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.ClueFound, "clue_a"), definition, state);

        // Then it's ignored
        Assert.That(result.StateChanged, Is.False);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates a 3-clue whodunnit scenario:
    /// bloody_knife + witness_testimony → identify_suspect → conclusion
    /// </summary>
    private (ObjectiveDefinition, ObjectiveState) CreateClueGraphScenario()
    {
        ClueGraph graph = new()
        {
            Clues =
            [
                new Clue { ClueId = "knife", Name = "Bloody Knife", TriggerTag = "bloody_knife" },
                new Clue { ClueId = "testimony", Name = "Witness Testimony", TriggerTag = "witness_testimony" },
                new Clue { ClueId = "motive", Name = "The Motive", TriggerTag = "find_motive" }
            ],
            Deductions =
            [
                new Deduction
                {
                    DeductionId = "suspect_id",
                    Description = "Identified the suspect from weapon and testimony",
                    RequiredClueIds = ["knife", "testimony"]
                },
                new Deduction
                {
                    DeductionId = "conclusion",
                    Description = "The butler did it",
                    RequiredClueIds = ["knife", "testimony", "motive"]
                }
            ],
            ConclusionDeductionId = "conclusion"
        };

        ObjectiveDefinition definition = CreateDefinitionWithGraph(graph);
        ObjectiveState state = CreateState(definition);
        return (definition, state);
    }

    /// <summary>
    /// Creates a minimal 2-clue scenario: clue_a + clue_b → conclusion.
    /// </summary>
    private (ObjectiveDefinition, ObjectiveState) CreateSimpleTwoClueScenario()
    {
        ClueGraph graph = new()
        {
            Clues =
            [
                new Clue { ClueId = "a", Name = "Clue A", TriggerTag = "clue_a" },
                new Clue { ClueId = "b", Name = "Clue B", TriggerTag = "clue_b" }
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

        ObjectiveDefinition definition = CreateDefinitionWithGraph(graph);
        ObjectiveState state = CreateState(definition);
        return (definition, state);
    }

    private static ObjectiveDefinition CreateDefinitionWithGraph(ClueGraph graph)
    {
        return new ObjectiveDefinition
        {
            ObjectiveId = ObjectiveId.NewId(),
            TypeTag = "investigate",
            DisplayText = "Investigate the crime",
            Config = new Dictionary<string, object>
            {
                [InvestigateObjectiveEvaluator.ModeKey] = "clue_graph",
                [InvestigateObjectiveEvaluator.ClueGraphKey] = graph
            }
        };
    }

    private ObjectiveState CreateState(ObjectiveDefinition definition)
    {
        ObjectiveState state = new() { ObjectiveId = definition.ObjectiveId };
        _evaluator.Initialize(definition, state);
        return state;
    }

    #endregion
}

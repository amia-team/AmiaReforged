using AmiaReforged.Shared.Quests;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Application;

[TestFixture]
public class QuestDefinitionValidationTests
{
    private static QuestDefinitionDto Quest() => new()
    {
        QuestId = "rats", Title = "Rats", Description = "Clear the cellar.", Stages =
        [
            new() { StageId = 10, NextStageId = 30, ObjectiveGroups =
                [new() { DisplayName = "Collect", Objectives =
                    [new() { ObjectiveId = "tails", TypeTag = "collect", DisplayText = "Collect tails", TargetTag = "rat_tail", RequiredCount = 5 }] }] },
            new() { StageId = 30, QuestState = "Completed" }
        ]
    };

    [Test]
    public void Valid_forward_branch_and_state_only_ending_are_allowed()
    {
        Assert.That(QuestDefinitionValidator.Validate(Quest()), Is.Empty);
    }

    [TestCase(10, "higher ID")]
    [TestCase(999, "does not exist")]
    public void Invalid_next_stage_is_rejected(int target, string message)
    {
        QuestDefinitionDto quest = Quest();
        quest.Stages[0].NextStageId = target;
        Assert.That(QuestDefinitionValidator.Validate(quest).Any(e => e.Field == "Stages[0].NextStageId" && e.Message.Contains(message)), Is.True);
    }

    [Test]
    public void Group_branch_is_validated_independently_of_the_stage_link()
    {
        QuestDefinitionDto quest = Quest();
        quest.Stages[0].ObjectiveGroups[0].CompletionStageId = 999;
        Assert.That(QuestDefinitionValidator.Validate(quest).Any(e => e.Field.EndsWith("CompletionStageId") && e.StageId == 10), Is.True);
    }

    [Test]
    public void Duplicate_stage_and_objective_ids_are_rejected()
    {
        QuestDefinitionDto quest = Quest();
        quest.Stages.Add(new() { StageId = 10 });
        quest.Stages[0].ObjectiveGroups[0].Objectives.Add(new()
        { ObjectiveId = "tails", TypeTag = "collect", DisplayText = "Collect more", TargetTag = "rat_tail" });
        List<QuestValidationError> errors = QuestDefinitionValidator.Validate(quest);
        Assert.That(errors.Any(e => e.Field.EndsWith("StageId") && e.Message.Contains("more than once")), Is.True);
        Assert.That(errors.Any(e => e.Field.EndsWith("ObjectiveId") && e.Message.Contains("more than once")), Is.True);
    }

    [TestCase("Completed", false)]
    [TestCase("Failed", true)]
    public void Legacy_completion_flag_cannot_conflict_with_explicit_state(string state, bool conflict)
    {
        QuestDefinitionDto quest = Quest();
        quest.Stages[1].QuestState = state;
        quest.Stages[1].IsCompletionStage = true;
        Assert.That(QuestDefinitionValidator.Validate(quest).Any(e => e.Message.Contains("conflicts")), Is.EqualTo(conflict));
    }

    [Test]
    public void Legacy_and_camel_case_endings_remain_supported()
    {
        QuestDefinitionDto quest = Quest();
        quest.Stages[1].QuestState = null;
        quest.Stages[1].IsCompletionStage = true;
        Assert.That(QuestDefinitionValidator.IsTerminal(quest.Stages[1]), Is.True);
        Assert.That(QuestDefinitionValidator.Validate(quest), Is.Empty);
        quest.Stages[1].QuestState = "completed";
        Assert.That(QuestDefinitionValidator.Validate(quest), Is.Empty);
    }

    [TestCase("unknown", "TypeTag")]
    [TestCase("collect", "TargetTag")]
    public void Unsupported_actions_and_missing_targets_are_rejected(string type, string field)
    {
        QuestDefinitionDto quest = Quest();
        quest.Stages[0].ObjectiveGroups[0].Objectives[0].TypeTag = type;
        quest.Stages[0].ObjectiveGroups[0].Objectives[0].TargetTag = "";
        Assert.That(QuestDefinitionValidator.Validate(quest).Any(e => e.Field.EndsWith(field)), Is.True);
    }

    [Test]
    public void Zero_counts_and_negative_rewards_are_rejected()
    {
        QuestDefinitionDto quest = Quest();
        quest.Stages[0].ObjectiveGroups[0].Objectives[0].RequiredCount = 0;
        quest.Stages[0].Rewards = new() { Gold = -10 };
        Assert.That(QuestDefinitionValidator.Validate(quest).Select(e => e.Field), Does.Contain("Stages[0].Rewards.Gold"));
        Assert.That(QuestDefinitionValidator.Validate(quest).Any(e => e.Field.EndsWith("RequiredCount")), Is.True);
    }

    [Test]
    public void Ending_cannot_have_a_next_stage_or_group_branch()
    {
        QuestDefinitionDto quest = Quest();
        quest.Stages[0].QuestState = "Completed";
        quest.Stages[0].ObjectiveGroups[0].CompletionStageId = 30;
        Assert.That(QuestDefinitionValidator.Validate(quest).Count(e => e.Message.Contains("an ending cannot")), Is.EqualTo(2));
    }

    [Test]
    public void Invalid_group_mode_and_empty_group_are_rejected()
    {
        QuestDefinitionDto quest = Quest();
        quest.Stages[0].ObjectiveGroups[0].CompletionMode = "Sometimes";
        quest.Stages[0].ObjectiveGroups[0].Objectives.Clear();
        Assert.That(QuestDefinitionValidator.Validate(quest).Any(e => e.Field.EndsWith("CompletionMode")), Is.True);
        Assert.That(QuestDefinitionValidator.Validate(quest).Any(e => e.Field.EndsWith("Objectives")), Is.True);
    }
}

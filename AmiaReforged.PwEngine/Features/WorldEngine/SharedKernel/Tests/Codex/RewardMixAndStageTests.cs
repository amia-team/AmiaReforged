using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex;

[TestFixture]
public class RewardMixTests
{
    #region Empty / Default

    [Test]
    public void Empty_reward_mix_reports_IsEmpty_true()
    {
        RewardMix mix = RewardMix.Empty;

        Assert.That(mix.IsEmpty, Is.True);
        Assert.That(mix.Xp, Is.EqualTo(0));
        Assert.That(mix.Gold, Is.EqualTo(0));
        Assert.That(mix.KnowledgePoints, Is.EqualTo(0));
        Assert.That(mix.Proficiencies, Is.Empty);
    }

    [Test]
    public void Default_constructed_reward_mix_is_empty()
    {
        RewardMix mix = new();
        Assert.That(mix.IsEmpty, Is.True);
    }

    [Test]
    public void Reward_mix_with_any_value_reports_IsEmpty_false()
    {
        Assert.That(new RewardMix { Xp = 100 }.IsEmpty, Is.False);
        Assert.That(new RewardMix { Gold = 50 }.IsEmpty, Is.False);
        Assert.That(new RewardMix { KnowledgePoints = 1 }.IsEmpty, Is.False);
        Assert.That(new RewardMix
        {
            Proficiencies = [new ProficiencyReward { IndustryTag = "alchemy", ProficiencyXp = 10 }]
        }.IsEmpty, Is.False);
    }

    #endregion

    #region Validation

    [Test]
    public void Valid_reward_mix_returns_null_from_Validate()
    {
        RewardMix mix = new()
        {
            Xp = 200,
            Gold = 100,
            KnowledgePoints = 5,
            Proficiencies =
            [
                new ProficiencyReward { IndustryTag = "alchemy", ProficiencyXp = 50 },
                new ProficiencyReward { IndustryTag = "smithing", ProficiencyXp = 25 }
            ]
        };

        Assert.That(mix.Validate(), Is.Null);
    }

    [Test]
    public void Negative_xp_fails_validation()
    {
        RewardMix mix = new() { Xp = -1 };
        Assert.That(mix.Validate(), Does.Contain("XP"));
    }

    [Test]
    public void Negative_gold_fails_validation()
    {
        RewardMix mix = new() { Gold = -10 };
        Assert.That(mix.Validate(), Does.Contain("Gold"));
    }

    [Test]
    public void Negative_knowledge_points_fails_validation()
    {
        RewardMix mix = new() { KnowledgePoints = -1 };
        Assert.That(mix.Validate(), Does.Contain("Knowledge"));
    }

    [Test]
    public void Empty_industry_tag_fails_validation()
    {
        RewardMix mix = new()
        {
            Proficiencies = [new ProficiencyReward { IndustryTag = "", ProficiencyXp = 10 }]
        };
        Assert.That(mix.Validate(), Does.Contain("industry tag"));
    }

    [Test]
    public void Negative_proficiency_xp_fails_validation()
    {
        RewardMix mix = new()
        {
            Proficiencies = [new ProficiencyReward { IndustryTag = "alchemy", ProficiencyXp = -5 }]
        };
        Assert.That(mix.Validate(), Does.Contain("negative"));
    }

    #endregion

    #region Equality

    [Test]
    public void Two_identical_reward_mixes_are_equal()
    {
        RewardMix a = new() { Xp = 100, Gold = 50 };
        RewardMix b = new() { Xp = 100, Gold = 50 };
        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void Different_reward_mixes_are_not_equal()
    {
        RewardMix a = new() { Xp = 100 };
        RewardMix b = new() { Xp = 200 };
        Assert.That(a, Is.Not.EqualTo(b));
    }

    #endregion
}

[TestFixture]
public class QuestStageTests
{
    [Test]
    public void Stage_defaults_to_empty_objective_groups_and_rewards()
    {
        QuestStage stage = new() { StageId = 10 };

        Assert.That(stage.ObjectiveGroups, Is.Empty);
        Assert.That(stage.Rewards.IsEmpty, Is.True);
        Assert.That(stage.Hints, Is.Empty);
        Assert.That(stage.IsCompletionStage, Is.False);
    }

    [Test]
    public void Stage_owns_objective_groups()
    {
        QuestStage stage = new()
        {
            StageId = 20,
            JournalText = "Investigate the crime scene",
            ObjectiveGroups =
            [
                new QuestObjectiveGroup
                {
                    DisplayName = "Find evidence",
                    CompletionMode = CompletionMode.All,
                    Objectives =
                    [
                        new ObjectiveDefinition
                        {
                            ObjectiveId = ObjectiveId.NewId(),
                            TypeTag = "investigate",
                            DisplayText = "Examine the body"
                        }
                    ]
                }
            ]
        };

        Assert.That(stage.ObjectiveGroups, Has.Count.EqualTo(1));
        Assert.That(stage.ObjectiveGroups[0].DisplayName, Is.EqualTo("Find evidence"));
        Assert.That(stage.ObjectiveGroups[0].Objectives, Has.Count.EqualTo(1));
    }

    [Test]
    public void Stage_carries_reward_mix()
    {
        QuestStage stage = new()
        {
            StageId = 30,
            Rewards = new RewardMix
            {
                Xp = 500,
                Gold = 200,
                KnowledgePoints = 2,
                Proficiencies =
                [
                    new ProficiencyReward { IndustryTag = "alchemy", ProficiencyXp = 25 }
                ]
            }
        };

        Assert.That(stage.Rewards.IsEmpty, Is.False);
        Assert.That(stage.Rewards.Xp, Is.EqualTo(500));
        Assert.That(stage.Rewards.Gold, Is.EqualTo(200));
        Assert.That(stage.Rewards.KnowledgePoints, Is.EqualTo(2));
        Assert.That(stage.Rewards.Proficiencies, Has.Count.EqualTo(1));
        Assert.That(stage.Rewards.Proficiencies[0].IndustryTag, Is.EqualTo("alchemy"));
    }

    [Test]
    public void CodexQuestEntry_owns_stages_with_nested_objective_groups()
    {
        CodexQuestEntry entry = new()
        {
            QuestId = (QuestId)"test_quest",
            Title = "Test Quest",
            Description = "A test",
            DateStarted = DateTime.UtcNow,
            Stages =
            [
                new QuestStage
                {
                    StageId = 10,
                    JournalText = "Begin the quest",
                    ObjectiveGroups =
                    [
                        new QuestObjectiveGroup
                        {
                            DisplayName = "Kill goblins",
                            CompletionMode = CompletionMode.All,
                            Objectives =
                            [
                                new ObjectiveDefinition
                                {
                                    ObjectiveId = ObjectiveId.NewId(),
                                    TypeTag = "kill",
                                    DisplayText = "Kill 5 goblins",
                                    TargetTag = "goblin",
                                    RequiredCount = 5
                                }
                            ]
                        }
                    ],
                    Rewards = new RewardMix { Xp = 100, Gold = 50 }
                },
                new QuestStage
                {
                    StageId = 20,
                    JournalText = "Return to the quest giver",
                    IsCompletionStage = true,
                    Rewards = new RewardMix { Xp = 200 }
                }
            ],
            CompletionReward = new RewardMix { Xp = 500, Gold = 1000, KnowledgePoints = 3 }
        };

        Assert.That(entry.Stages, Has.Count.EqualTo(2));
        Assert.That(entry.Stages[0].ObjectiveGroups, Has.Count.EqualTo(1));
        Assert.That(entry.Stages[1].IsCompletionStage, Is.True);
        Assert.That(entry.CompletionReward.Xp, Is.EqualTo(500));
        Assert.That(entry.CompletionReward.Gold, Is.EqualTo(1000));
    }
}

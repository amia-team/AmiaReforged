using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.Shared.Quests;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Tests;

[TestFixture]
public class QuestControllerTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Persisted_stages_round_trip_through_the_shared_editor_contract_and_runtime(bool camelCase)
    {
        List<QuestStageDto> stages =
        [
            new()
            {
                StageId = 10, Name = "Gather rat tails", JournalText = "Collect tails.", QuestState = "InProgress", NextStageId = 30,
                Hints = ["Search the cellar."], Rewards = new() { Gold = 100, Proficiencies = [new() { IndustryTag = "alchemy", ProficiencyXp = 30 }] },
                ObjectiveGroups = [new()
                {
                    DisplayName = "Collect", CompletionMode = "Any", CompletionStageId = 30,
                    Objectives = [new()
                    {
                        ObjectiveId = "rat_tails", TypeTag = "collect", DisplayText = "Collect five tails", TargetTag = "rat_tail", RequiredCount = 5,
                        Config = new() { ["track_loss"] = true, ["custom"] = new { count = 42 } }
                    }]
                }]
            },
            new() { StageId = 30, Name = "Return to the innkeeper", QuestState = "Completed" }
        ];
        PersistedQuestDefinition persisted = new()
        {
            QuestId = "rats", Title = "Rats", Description = "Clear the cellar.",
            DefaultStageId = 10,
            StagesJson = JsonSerializer.Serialize(stages, new JsonSerializerOptions { PropertyNamingPolicy = camelCase ? JsonNamingPolicy.CamelCase : null })
        };
        Mock<IWorldEngineFacade> facade = new();
        facade.Setup(f => f.QueryAsync<GetQuestDefinitionQuery, PersistedQuestDefinition?>(
            It.Is<GetQuestDefinitionQuery>(q => q.QuestId == "rats"), It.IsAny<CancellationToken>())).ReturnsAsync(persisted);
        RouteContext context = new(null, new() { ["questId"] = "rats" }, CancellationToken.None)
        { Services = new FacadeProvider(facade.Object) };

        ApiResult result = await QuestController.GetById(context);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        QuestDefinitionDto editor = JsonSerializer.Deserialize<QuestDefinitionDto>(JsonSerializer.Serialize(result.Data))!;
        Assert.That(QuestDefinitionValidator.Validate(editor), Is.Empty);
        Assert.That(editor.DefaultStageId, Is.EqualTo(10));
        Assert.That(editor.Stages[0].Hints, Is.EqualTo(stages[0].Hints));
        Assert.That(editor.Stages.Select(s => s.Name), Is.EqualTo(stages.Select(s => s.Name)));
        string editedJson = JsonSerializer.Serialize(editor.Stages);
        Assert.That(editedJson, Does.Not.Contain("IsEmpty"));
        JsonSerializerOptions runtimeOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase), new ObjectiveIdJsonConverter() }
        };
        List<QuestStage> runtime = JsonSerializer.Deserialize<List<QuestStage>>(editedJson, runtimeOptions)!;
        Assert.That(runtime.Select(s => s.Name), Is.EqualTo(stages.Select(s => s.Name)));
        List<QuestStageDto> reloaded = JsonSerializer.Deserialize<List<QuestStageDto>>(JsonSerializer.Serialize(runtime, runtimeOptions))!;
        Assert.That(reloaded.Select(s => s.Name), Is.EqualTo(stages.Select(s => s.Name)));
        Assert.That(runtime[0].NextStageId, Is.EqualTo(30));
        Assert.That(runtime[1].QuestState, Is.EqualTo(QuestState.Completed));
        Assert.That(runtime[0].Rewards.Gold, Is.EqualTo(100));
        Assert.That(runtime[0].Rewards.Proficiencies.Single().ProficiencyXp, Is.EqualTo(30));
        QuestObjectiveGroup group = runtime[0].ObjectiveGroups.Single();
        Assert.That(group.CompletionStageId, Is.EqualTo(30));
        Assert.That(group.CompletionMode, Is.EqualTo(CompletionMode.Any));
        ObjectiveDefinition objective = group.Objectives.Single();
        Assert.That(objective.ObjectiveId.Value, Is.EqualTo("rat_tails"));
        Assert.That(objective.RequiredCount, Is.EqualTo(5));
        Assert.That(((JsonElement)objective.Config["custom"]).GetProperty("count").GetInt32(), Is.EqualTo(42));
    }

    private sealed class FacadeProvider(IWorldEngineFacade facade) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(IWorldEngineFacade) ? facade : null;
    }
}

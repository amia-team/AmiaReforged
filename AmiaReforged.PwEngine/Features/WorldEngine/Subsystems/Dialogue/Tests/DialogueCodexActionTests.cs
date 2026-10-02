using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Quests;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Reputation;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Tests;

[TestFixture]
public class DialogueCodexActionTests
{
    [Test]
    public async Task ReputationActionChangesTheAuthoritativeStateReadByConditions()
    {
        CharacterId id = CharacterId.From(Guid.NewGuid()); PlayerCodex? saved = null;
        Mock<IPlayerCodexRepository> repository = new();
        repository.Setup(r => r.LoadAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(() => saved);
        repository.Setup(r => r.SaveAsync(It.IsAny<PlayerCodex>(), It.IsAny<CancellationToken>())).Callback<PlayerCodex, CancellationToken>((codex, _) => saved = codex).Returns(Task.CompletedTask);
        Mock<IEventBus> bus = new();
        bus.Setup(b => b.PublishAsync(It.IsAny<ReputationChangedEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        AdjustReputationHandler handler = new(repository.Object, bus.Object);
        Mock<IWorldEngineFacade> facade = new();
        facade.Setup(f => f.ExecuteAsync(It.IsAny<AdjustReputationCommand>(), It.IsAny<CancellationToken>())).Returns<AdjustReputationCommand, CancellationToken>(handler.HandleAsync);
        CommandResult result = await DialogueCodexActions.ExecuteAsync(facade.Object, new DialogueAction
        {
            ActionType = DialogueActionType.ChangeReputation, Parameters = new() { ["factionId"] = "guards", ["amount"] = "15" }
        }, id);
        Assert.That(result.Success, Is.True);
        FactionReputation? reputation = await new GetCodexReputationHandler(repository.Object).HandleAsync(new() { CharacterId = id, FactionId = new FactionId("guards") });
        Assert.That(reputation?.CurrentScore.Value, Is.EqualTo(15));
    }

    [Test]
    public async Task StartQuestDispatchesFirstPlayableStageRatherThanLoggingSuccess()
    {
        CharacterId id = CharacterId.From(Guid.NewGuid()); Mock<IWorldEngineFacade> facade = Facade(id, []);
        facade.Setup(f => f.QueryAsync<GetQuestDefinitionQuery, PersistedQuestDefinition?>(It.IsAny<GetQuestDefinitionQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(Definition());
        SetQuestStageCommand? dispatched = null;
        facade.Setup(f => f.ExecuteAsync(It.IsAny<SetQuestStageCommand>(), It.IsAny<CancellationToken>())).Callback<SetQuestStageCommand, CancellationToken>((command, _) => dispatched = command).ReturnsAsync(CommandResult.Ok());
        Assert.That((await DialogueCodexActions.ExecuteAsync(facade.Object, QuestAction(DialogueActionType.StartQuest), id)).Success, Is.True);
        Assert.That(dispatched?.StageId, Is.EqualTo(10));
        Assert.That(dispatched?.CharacterId, Is.EqualTo(id));
    }

    [Test]
    public async Task CompletionRequiresAnActiveQuestAndDispatchesItsCompletionStage()
    {
        CharacterId id = CharacterId.From(Guid.NewGuid());
        Mock<IWorldEngineFacade> facade = Facade(id, [new() { QuestId = new QuestId("rats"), Title = "Rats", Description = "Rats", State = QuestState.InProgress }]);
        facade.Setup(f => f.QueryAsync<GetQuestDefinitionQuery, PersistedQuestDefinition?>(It.IsAny<GetQuestDefinitionQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(Definition());
        facade.Setup(f => f.ExecuteAsync(It.IsAny<SetQuestStageCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(CommandResult.Ok());
        Assert.That((await DialogueCodexActions.ExecuteAsync(facade.Object, QuestAction(DialogueActionType.CompleteQuest), id)).Success, Is.True);
        facade.Verify(f => f.ExecuteAsync(It.Is<SetQuestStageCommand>(c => c.StageId == 30), It.IsAny<CancellationToken>()), Times.Once);
        Mock<IWorldEngineFacade> missing = Facade(id, []);
        Assert.That((await DialogueCodexActions.ExecuteAsync(missing.Object, QuestAction(DialogueActionType.CompleteQuest), id)).Success, Is.False);
        missing.Verify(f => f.ExecuteAsync(It.IsAny<SetQuestStageCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RepeatedStartDoesNotResetAQuestOrReplayStageRewards()
    {
        CharacterId id = CharacterId.From(Guid.NewGuid());
        Mock<IWorldEngineFacade> facade = Facade(id, [new() { QuestId = new QuestId("rats"), Title = "Rats", Description = "Rats", State = QuestState.InProgress, CurrentStageId = 20 }]);
        Assert.That((await DialogueCodexActions.ExecuteAsync(facade.Object, QuestAction(DialogueActionType.StartQuest), id)).Success, Is.True);
        facade.Verify(f => f.ExecuteAsync(It.IsAny<SetQuestStageCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AmbiguousCompletionDoesNotGuessAStage()
    {
        CharacterId id = CharacterId.From(Guid.NewGuid());
        Mock<IWorldEngineFacade> facade = Facade(id, [new() { QuestId = new QuestId("rats"), Title = "Rats", Description = "Rats", State = QuestState.InProgress }]);
        PersistedQuestDefinition definition = Definition();
        definition.StagesJson = "[{\"stageId\":30,\"isCompletionStage\":true},{\"stageId\":40,\"questState\":\"Completed\"}]";
        facade.Setup(f => f.QueryAsync<GetQuestDefinitionQuery, PersistedQuestDefinition?>(It.IsAny<GetQuestDefinitionQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(definition);
        Assert.That((await DialogueCodexActions.ExecuteAsync(facade.Object, QuestAction(DialogueActionType.CompleteQuest), id)).Success, Is.False);
        facade.Verify(f => f.ExecuteAsync(It.IsAny<SetQuestStageCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IWorldEngineFacade> Facade(CharacterId id, IReadOnlyList<CodexQuestEntry> quests)
    {
        Mock<IWorldEngineFacade> facade = new();
        facade.Setup(f => f.QueryAsync<GetCodexQuestsQuery, IReadOnlyList<CodexQuestEntry>>(It.Is<GetCodexQuestsQuery>(q => q.CharacterId == id), It.IsAny<CancellationToken>())).ReturnsAsync(quests);
        return facade;
    }
    private static DialogueAction QuestAction(DialogueActionType type) => new() { ActionType = type, Parameters = new() { ["questId"] = "rats" } };
    private static PersistedQuestDefinition Definition() => new() { QuestId = "rats", Title = "Rats", Description = "Rats", StagesJson = "[{\"stageId\":1,\"questState\":\"Discovered\"},{\"stageId\":10},{\"stageId\":30,\"isCompletionStage\":true}]" };
}

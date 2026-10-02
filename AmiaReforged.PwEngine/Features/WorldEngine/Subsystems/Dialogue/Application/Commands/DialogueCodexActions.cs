using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Quests;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Reputation;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Commands;

internal static class DialogueCodexActions
{
    public static Task<CommandResult> ExecuteAsync(IWorldEngineFacade facade, DialogueAction action, CharacterId characterId) => action.ActionType switch
    {
        DialogueActionType.StartQuest => SetQuestStateAsync(facade, action, characterId, complete: false),
        DialogueActionType.CompleteQuest => SetQuestStateAsync(facade, action, characterId, complete: true),
        DialogueActionType.ChangeReputation => facade.ExecuteAsync(new AdjustReputationCommand
        {
            CharacterId = characterId, FactionId = action.GetRequiredParam("factionId"),
            FactionName = action.GetParam("factionName") ?? action.GetRequiredParam("factionId"),
            Delta = int.Parse(action.GetRequiredParam("amount")), Reason = action.GetParam("reason") ?? "Dialogue"
        }),
        DialogueActionType.SetQuestStage => facade.ExecuteAsync(new SetQuestStageCommand
        {
            CharacterId = characterId, QuestId = action.GetRequiredParam("questId"), StageId = int.Parse(action.GetRequiredParam("stageId"))
        }),
        DialogueActionType.GrantKnowledge => facade.Codex.GrantKnowledgeAsync(characterId, action.GetRequiredParam("loreId")),
        _ => Task.FromResult(CommandResult.Fail($"Unsupported Codex action: {action.ActionType}"))
    };

    private static async Task<CommandResult> SetQuestStateAsync(IWorldEngineFacade facade, DialogueAction action, CharacterId characterId, bool complete)
    {
        string questId = action.GetRequiredParam("questId");
        IReadOnlyList<CodexQuestEntry> quests = await facade.QueryAsync<GetCodexQuestsQuery, IReadOnlyList<CodexQuestEntry>>(new() { CharacterId = characterId });
        CodexQuestEntry? existing = quests.FirstOrDefault(q => q.QuestId.Value == questId);
        if (complete && existing?.EffectiveState == QuestState.Completed || !complete && existing?.EffectiveState == QuestState.InProgress) return CommandResult.Ok();
        if (complete && existing?.EffectiveState != QuestState.InProgress) return CommandResult.Fail($"Quest '{questId}' is not in progress");
        if (!complete && existing is not null && existing.EffectiveState != QuestState.Discovered) return CommandResult.Fail($"Quest '{questId}' cannot be restarted from {existing.EffectiveState}");

        PersistedQuestDefinition? definition = await facade.QueryAsync<GetQuestDefinitionQuery, PersistedQuestDefinition?>(new() { QuestId = questId });
        if (definition is null) return CommandResult.Fail($"Quest definition '{questId}' not found");
        List<Stage> stages = JsonSerializer.Deserialize<List<Stage>>(definition.StagesJson ?? "[]", new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() }
        }) ?? [];
        List<Stage> eligible = stages.Where(s => s.StageId > 0 && (complete
            ? s.QuestState == QuestState.Completed || s.QuestState is null && s.IsCompletionStage
            : !s.IsCompletionStage && s.QuestState is null or QuestState.InProgress)).OrderBy(s => s.StageId).ToList();
        Stage? target;
        if (action.GetParam("stageId") is { } stageId)
            target = int.TryParse(stageId, out int id) ? eligible.FirstOrDefault(s => s.StageId == id) : null;
        else
            target = complete ? eligible.Count == 1 ? eligible[0] : null : eligible.FirstOrDefault();
        if (target is null) return CommandResult.Fail($"Quest '{questId}' needs an explicit valid {(complete ? "completion" : "starting")} stageId");
        return await facade.ExecuteAsync(new SetQuestStageCommand { CharacterId = characterId, QuestId = questId, StageId = target.StageId });
    }

    private sealed record Stage
    {
        public int StageId { get; init; }
        public bool IsCompletionStage { get; init; }
        public QuestState? QuestState { get; init; }
    }
}

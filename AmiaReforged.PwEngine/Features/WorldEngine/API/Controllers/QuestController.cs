using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;
using Anvil;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for managing codex quest definitions.
/// Supports CRUD operations for the admin panel.
/// </summary>
public class QuestController
{
    private const string BasePath = "/api/worldengine/codex/quests";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// List all quest definitions with optional search and pagination.
    /// GET /api/worldengine/codex/quests?search=&amp;page=1&amp;pageSize=50
    /// </summary>
    [HttpGet(BasePath)]
    public static async Task<ApiResult> GetAll(RouteContext ctx)
    {
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        string? search = ctx.GetQueryParam("search");
        int page = int.TryParse(ctx.GetQueryParam("page"), out int p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out int ps) ? Math.Clamp(ps, 1, 200) : 50;

        List<PersistedQuestDefinition> matches = await facade.QueryAsync<SearchQuestDefinitionsQuery, List<PersistedQuestDefinition>>(
            new SearchQuestDefinitionsQuery { SearchTerm = search }, ctx.CancellationToken);

        int totalCount = matches.Count;
        List<PersistedQuestDefinition> items = matches
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new ApiResult(200, new
        {
            items = items.Select(ToDto).ToArray(),
            totalCount,
            page,
            pageSize
        });
    }

    /// <summary>
    /// Get a single quest definition by ID.
    /// GET /api/worldengine/codex/quests/{questId}
    /// </summary>
    [HttpGet(BasePath + "/{questId}")]
    public static async Task<ApiResult> GetById(RouteContext ctx)
    {
        string questId = ctx.GetRouteValue("questId");

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        PersistedQuestDefinition? definition = await facade.QueryAsync<GetQuestDefinitionQuery, PersistedQuestDefinition?>(
            new GetQuestDefinitionQuery { QuestId = questId }, ctx.CancellationToken);

        if (definition == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No quest definition with ID '{questId}'"));
        }

        return new ApiResult(200, ToDto(definition));
    }

    /// <summary>
    /// Create a new quest definition.
    /// POST /api/worldengine/codex/quests
    /// </summary>
    [HttpPost(BasePath)]
    public static async Task<ApiResult> Create(RouteContext ctx)
    {
        QuestDefinitionDto? dto = await ctx.ReadJsonBodyAsync<QuestDefinitionDto>();
        if (dto == null)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required"));
        }

        string? validationError = ValidateDto(dto);
        if (validationError != null)
        {
            return new ApiResult(400, new ErrorResponse("Validation failed", validationError));
        }

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        PersistedQuestDefinition definition = FromDto(dto);
        CommandResult result = await facade.ExecuteAsync(new CreateQuestDefinitionCommand
        {
            Definition = definition
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool conflict = result.ErrorMessage?.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true;
            return new ApiResult(conflict ? 409 : 400,
                new ErrorResponse(conflict ? "Conflict" : "Command failed", result.ErrorMessage));
        }

        PersistedQuestDefinition? created = await facade.QueryAsync<GetQuestDefinitionQuery, PersistedQuestDefinition?>(
            new GetQuestDefinitionQuery { QuestId = definition.QuestId }, ctx.CancellationToken);

        return new ApiResult(201, ToDto(created ?? definition));
    }

    /// <summary>
    /// Update an existing quest definition.
    /// PUT /api/worldengine/codex/quests/{questId}
    /// </summary>
    [HttpPut(BasePath + "/{questId}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string questId = ctx.GetRouteValue("questId");

        QuestDefinitionDto? dto = await ctx.ReadJsonBodyAsync<QuestDefinitionDto>();
        if (dto == null)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required"));
        }

        string? validationError = ValidateDto(dto);
        if (validationError != null)
        {
            return new ApiResult(400, new ErrorResponse("Validation failed", validationError));
        }

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        PersistedQuestDefinition definition = FromDto(dto);
        definition.QuestId = questId;
        CommandResult result = await facade.ExecuteAsync(new UpdateQuestDefinitionCommand
        {
            QuestId = questId,
            Definition = definition
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool notFound = result.ErrorMessage?.StartsWith("No quest definition with ID", StringComparison.OrdinalIgnoreCase) == true;
            return new ApiResult(notFound ? 404 : 400, new ErrorResponse(
                notFound ? "Not found" : "Command failed", result.ErrorMessage));
        }

        PersistedQuestDefinition? updated = await facade.QueryAsync<GetQuestDefinitionQuery, PersistedQuestDefinition?>(
            new GetQuestDefinitionQuery { QuestId = questId }, ctx.CancellationToken);

        return new ApiResult(200, ToDto(updated ?? definition));
    }

    /// <summary>
    /// Delete a quest definition.
    /// DELETE /api/worldengine/codex/quests/{questId}
    /// </summary>
    [HttpDelete(BasePath + "/{questId}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string questId = ctx.GetRouteValue("questId");

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        CommandResult result = await facade.ExecuteAsync(new DeleteQuestDefinitionCommand
        {
            QuestId = questId
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", result.ErrorMessage));
        }

        return new ApiResult(204, new { message = "Deleted" });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

    private static string? ValidateDto(QuestDefinitionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.QuestId)) return "QuestId is required";
        if (dto.QuestId.Length > 100) return "QuestId must not exceed 100 characters";
        if (string.IsNullOrWhiteSpace(dto.Title)) return "Title is required";
        if (dto.Title.Length > 200) return "Title must not exceed 200 characters";
        if (string.IsNullOrWhiteSpace(dto.Description)) return "Description is required";
        if (dto.Keywords is { Length: > 1000 }) return "Keywords must not exceed 1000 characters";

        // Validate stages and their nested objectives/rewards
        foreach (QuestStageJsonModel stage in dto.Stages)
        {
            if (stage.ObjectiveGroups != null)
            {
                foreach (ObjectiveGroupJsonModel group in stage.ObjectiveGroups)
                {
                    if (string.IsNullOrWhiteSpace(group.DisplayName))
                        return $"Stage {stage.StageId}: objective group display name is required";

                    if (group.Objectives != null)
                    {
                        foreach (ObjectiveJsonModel obj in group.Objectives)
                        {
                            if (string.IsNullOrWhiteSpace(obj.TypeTag))
                                return $"Stage {stage.StageId}: objective type tag is required";
                            if (string.IsNullOrWhiteSpace(obj.DisplayText))
                                return $"Stage {stage.StageId}: objective display text is required";
                            if (obj.RequiredCount < 0)
                                return $"Stage {stage.StageId}: objective required count cannot be negative";
                        }
                    }
                }
            }

            string? rewardError = ValidateReward(stage.Rewards, $"Stage {stage.StageId}");
            if (rewardError != null) return rewardError;
        }

        // Validate completion reward
        string? completionRewardError = ValidateReward(dto.CompletionReward, "Completion reward");
        if (completionRewardError != null) return completionRewardError;

        return null;
    }

    private static string? ValidateReward(RewardMixJsonModel? reward, string context)
    {
        if (reward == null) return null;
        if (reward.Xp < 0) return $"{context}: XP reward cannot be negative";
        if (reward.Gold < 0) return $"{context}: gold reward cannot be negative";
        if (reward.KnowledgePoints < 0) return $"{context}: knowledge points cannot be negative";

        if (reward.Proficiencies != null)
        {
            foreach (ProficiencyRewardJsonModel prof in reward.Proficiencies)
            {
                if (string.IsNullOrWhiteSpace(prof.IndustryTag))
                    return $"{context}: proficiency reward must specify an industry tag";
                if (prof.ProficiencyXp < 0)
                    return $"{context}: proficiency XP for '{prof.IndustryTag}' cannot be negative";
            }
        }

        return null;
    }

    private static object ToDto(PersistedQuestDefinition def)
    {
        return new
        {
            def.QuestId,
            def.Title,
            def.Description,
            Stages = DeserializeStages(def.StagesJson),
            CompletionReward = DeserializeReward(def.CompletionRewardJson),
            def.QuestGiver,
            def.Location,
            def.Keywords,
            def.IsAlwaysAvailable,
            def.CreatedUtc
        };
    }

    private static PersistedQuestDefinition FromDto(QuestDefinitionDto dto)
    {
        return new PersistedQuestDefinition
        {
            QuestId = dto.QuestId.Trim(),
            Title = dto.Title.Trim(),
            Description = dto.Description,
            StagesJson = SerializeStages(dto.Stages),
            CompletionRewardJson = SerializeReward(dto.CompletionReward),
            QuestGiver = string.IsNullOrWhiteSpace(dto.QuestGiver) ? null : dto.QuestGiver.Trim(),
            Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim(),
            Keywords = string.IsNullOrWhiteSpace(dto.Keywords) ? null : dto.Keywords.Trim(),
            IsAlwaysAvailable = dto.IsAlwaysAvailable
        };
    }

    private static List<QuestStageJsonModel> DeserializeStages(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]") return [];
        try { return JsonSerializer.Deserialize<List<QuestStageJsonModel>>(json, JsonOpts) ?? []; }
        catch { return []; }
    }

    private static string SerializeStages(List<QuestStageJsonModel>? stages)
    {
        if (stages == null || stages.Count == 0) return "[]";
        return JsonSerializer.Serialize(stages, JsonOpts);
    }

    private static RewardMixJsonModel? DeserializeReward(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}") return null;
        try { return JsonSerializer.Deserialize<RewardMixJsonModel>(json, JsonOpts); }
        catch { return null; }
    }

    private static string SerializeReward(RewardMixJsonModel? reward)
    {
        if (reward == null) return "{}";
        return JsonSerializer.Serialize(reward, JsonOpts);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DTOs
    // ═══════════════════════════════════════════════════════════════════

    private record QuestStageJsonModel
    {
        public int StageId { get; init; }
        public string JournalText { get; init; } = string.Empty;
        public bool IsCompletionStage { get; init; }
        public string? QuestState { get; init; }
        public int? NextStageId { get; init; }
        public List<string> Hints { get; init; } = [];
        public List<ObjectiveGroupJsonModel>? ObjectiveGroups { get; init; }
        public RewardMixJsonModel? Rewards { get; init; }
    }

    private record ObjectiveGroupJsonModel
    {
        public string DisplayName { get; init; } = string.Empty;
        public string CompletionMode { get; init; } = "All";
        public int? CompletionStageId { get; init; }
        public List<ObjectiveJsonModel>? Objectives { get; init; }
    }

    private record ObjectiveJsonModel
    {
        public string ObjectiveId { get; init; } = string.Empty;
        public string TypeTag { get; init; } = string.Empty;
        public string DisplayText { get; init; } = string.Empty;
        public string? TargetTag { get; init; }
        public int RequiredCount { get; init; } = 1;
        public Dictionary<string, object>? Config { get; init; }
    }

    private record RewardMixJsonModel
    {
        public int Xp { get; init; }
        public int Gold { get; init; }
        public int KnowledgePoints { get; init; }
        public List<ProficiencyRewardJsonModel>? Proficiencies { get; init; }
    }

    private record ProficiencyRewardJsonModel
    {
        public string IndustryTag { get; init; } = string.Empty;
        public int ProficiencyXp { get; init; }
    }

    private record QuestDefinitionDto
    {
        public string QuestId { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public List<QuestStageJsonModel> Stages { get; init; } = [];
        public RewardMixJsonModel? CompletionReward { get; init; }
        public string? QuestGiver { get; init; }
        public string? Location { get; init; }
        public string? Keywords { get; init; }
        public bool IsAlwaysAvailable { get; init; }
    }
}

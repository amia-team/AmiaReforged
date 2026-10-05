using AmiaReforged.Shared.Quests;
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
        => QuestDefinitionValidator.Validate(dto).FirstOrDefault()?.Message;

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
            def.DefaultStageId,
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
            IsAlwaysAvailable = dto.IsAlwaysAvailable,
            DefaultStageId = dto.DefaultStageId
        };
    }

    private static List<QuestStageDto> DeserializeStages(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]") return [];
        try { return JsonSerializer.Deserialize<List<QuestStageDto>>(json, JsonOpts) ?? []; }
        catch { return []; }
    }

    private static string SerializeStages(List<QuestStageDto>? stages)
    {
        if (stages == null || stages.Count == 0) return "[]";
        return JsonSerializer.Serialize(stages, JsonOpts);
    }

    private static RewardMixDto? DeserializeReward(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}") return null;
        try { return JsonSerializer.Deserialize<RewardMixDto>(json, JsonOpts); }
        catch { return null; }
    }

    private static string SerializeReward(RewardMixDto? reward)
    {
        if (reward == null) return "{}";
        return JsonSerializer.Serialize(reward, JsonOpts);
    }

}

using AmiaReforged.Shared.Dialogue;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.API;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for managing dialogue tree definitions.
/// Supports CRUD operations for the admin panel dialogue tree editor.
/// </summary>
public class DialogueController
{
    private const string BasePath = "/api/worldengine/dialogue";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// List all dialogue trees with optional search and pagination.
    /// GET /api/worldengine/dialogue?search=&amp;page=1&amp;pageSize=50
    /// </summary>
    [HttpGet(BasePath)]
    public static async Task<ApiResult> GetAll(RouteContext ctx)
    {
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        string? search = ctx.GetQueryParam("search");
        int page = int.TryParse(ctx.GetQueryParam("page"), out int p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out int ps) ? Math.Clamp(ps, 1, 200) : 50;

        List<PersistedDialogueTree> matches = await facade.QueryAsync<SearchDialogueTreesQuery, List<PersistedDialogueTree>>(
            new SearchDialogueTreesQuery { SearchTerm = search }, ctx.CancellationToken);

        int totalCount = matches.Count;
        List<PersistedDialogueTree> items = matches
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
    /// Get a single dialogue tree by ID.
    /// GET /api/worldengine/dialogue/{dialogueTreeId}
    /// </summary>
    [HttpGet(BasePath + "/{dialogueTreeId}")]
    public static async Task<ApiResult> GetById(RouteContext ctx)
    {
        string dialogueTreeId = ctx.GetRouteValue("dialogueTreeId");

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        PersistedDialogueTree? definition = await facade.QueryAsync<GetDialogueTreeQuery, PersistedDialogueTree?>(
            new GetDialogueTreeQuery { DialogueTreeId = dialogueTreeId }, ctx.CancellationToken);

        if (definition == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No dialogue tree with ID '{dialogueTreeId}'"));
        }

        return new ApiResult(200, ToDto(definition));
    }

    /// <summary>
    /// Get dialogue trees by NPC speaker tag.
    /// GET /api/worldengine/dialogue/by-speaker/{speakerTag}
    /// </summary>
    [HttpGet(BasePath + "/by-speaker/{speakerTag}")]
    public static async Task<ApiResult> GetBySpeaker(RouteContext ctx)
    {
        string speakerTag = ctx.GetRouteValue("speakerTag");

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        List<PersistedDialogueTree> items = await facade.QueryAsync<GetDialogueTreesBySpeakerQuery, List<PersistedDialogueTree>>(
            new GetDialogueTreesBySpeakerQuery { SpeakerTag = speakerTag }, ctx.CancellationToken);

        return new ApiResult(200, new
        {
            items = items.Select(ToDto).ToArray(),
            totalCount = items.Count
        });
    }

    /// <summary>
    /// Create a new dialogue tree.
    /// POST /api/worldengine/dialogue
    /// </summary>
    [HttpPost(BasePath)]
    public static async Task<ApiResult> Create(RouteContext ctx)
    {
        DialogueTreeDto? dto = await ctx.ReadJsonBodyAsync<DialogueTreeDto>();
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

        PersistedDialogueTree entity = FromDto(dto);
        CommandResult result = await facade.ExecuteAsync(new CreateDialogueTreeCommand
        {
            Tree = entity
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool conflict = result.ErrorMessage?.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true || result.ErrorMessage?.StartsWith("Speaker tag", StringComparison.Ordinal) == true;
            return new ApiResult(conflict ? 409 : 400,
                new ErrorResponse(conflict ? "Conflict" : "Command failed", result.ErrorMessage));
        }

        PersistedDialogueTree? created = await facade.QueryAsync<GetDialogueTreeQuery, PersistedDialogueTree?>(
            new GetDialogueTreeQuery { DialogueTreeId = entity.DialogueTreeId }, ctx.CancellationToken);

        return new ApiResult(201, ToDto(created ?? entity));
    }

    /// <summary>
    /// Update an existing dialogue tree.
    /// PUT /api/worldengine/dialogue/{dialogueTreeId}
    /// </summary>
    [HttpPut(BasePath + "/{dialogueTreeId}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string dialogueTreeId = ctx.GetRouteValue("dialogueTreeId");

        DialogueTreeDto? dto = await ctx.ReadJsonBodyAsync<DialogueTreeDto>();
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

        PersistedDialogueTree entity = FromDto(dto);
        entity.DialogueTreeId = dialogueTreeId;
        CommandResult result = await facade.ExecuteAsync(new UpdateDialogueTreeCommand
        {
            DialogueTreeId = dialogueTreeId,
            Tree = entity
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool notFound = result.ErrorMessage?.StartsWith("No dialogue tree with ID", StringComparison.OrdinalIgnoreCase) == true;
            bool conflict = result.ErrorMessage?.StartsWith("Speaker tag", StringComparison.Ordinal) == true;
            return new ApiResult(notFound ? 404 : conflict ? 409 : 400, new ErrorResponse(
                notFound ? "Not found" : conflict ? "Conflict" : "Command failed", result.ErrorMessage));
        }

        PersistedDialogueTree? updated = await facade.QueryAsync<GetDialogueTreeQuery, PersistedDialogueTree?>(
            new GetDialogueTreeQuery { DialogueTreeId = dialogueTreeId }, ctx.CancellationToken);

        return new ApiResult(200, ToDto(updated ?? entity));
    }

    /// <summary>
    /// Delete a dialogue tree.
    /// DELETE /api/worldengine/dialogue/{dialogueTreeId}
    /// </summary>
    [HttpDelete(BasePath + "/{dialogueTreeId}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string dialogueTreeId = ctx.GetRouteValue("dialogueTreeId");

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        CommandResult result = await facade.ExecuteAsync(new DeleteDialogueTreeCommand
        {
            DialogueTreeId = dialogueTreeId
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", result.ErrorMessage));
        }

        return new ApiResult(204, new { message = "Deleted" });
    }

    [HttpGet(BasePath + "/{dialogueTreeId}/runtime")]
    public static async Task<ApiResult> GetRuntimeStatus(RouteContext ctx)
    {
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();
        DialogueRuntimeStatusDto? status = await facade.QueryAsync<GetDialogueRuntimeStatusQuery, DialogueRuntimeStatusDto?>(
            new() { DialogueTreeId = ctx.GetRouteValue("dialogueTreeId") }, ctx.CancellationToken);
        return status is null ? new ApiResult(404, new ErrorResponse("Not found", "Dialogue not found")) : new ApiResult(200, status);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

    private static string? ValidateDto(DialogueTreeDto dto)
    {
        DialogueDefinitionValidator.Normalize(dto);
        List<string> errors = DialogueDefinitionValidator.Validate(dto);
        return errors.Count == 0 ? null : string.Join("\n", errors);
    }

    private static object ToDto(PersistedDialogueTree entity)
    {
        List<DialogueNodeDto>? nodes = null;
        if (!string.IsNullOrWhiteSpace(entity.NodesJson) && entity.NodesJson != "[]")
        {
            try
            {
                nodes = JsonSerializer.Deserialize<List<DialogueNodeDto>>(entity.NodesJson, JsonOpts);
            }
            catch { nodes = []; }
        }

        return new
        {
            entity.DialogueTreeId,
            entity.Title,
            entity.Description,
            entity.RootNodeId,
            entity.SpeakerTag,
            Nodes = nodes ?? [],
            entity.CreatedUtc,
            entity.UpdatedUtc
        };
    }

    private static PersistedDialogueTree FromDto(DialogueTreeDto dto)
    {
        return new PersistedDialogueTree
        {
            DialogueTreeId = dto.DialogueTreeId.Trim(),
            Title = dto.Title.Trim(),
            Description = dto.Description ?? string.Empty,
            RootNodeId = dto.RootNodeId,
            SpeakerTag = string.IsNullOrWhiteSpace(dto.SpeakerTag) ? null : dto.SpeakerTag.Trim(),
            NodesJson = JsonSerializer.Serialize(dto.Nodes ?? [], JsonOpts)
        };
    }

}

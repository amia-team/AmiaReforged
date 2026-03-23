using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.API;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;
using Anvil;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for managing dialogue tree definitions.
/// Supports CRUD operations for the admin panel dialogue tree editor.
/// </summary>
public class DialogueController
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
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
        string? search = ctx.GetQueryParam("search");
        int page = int.TryParse(ctx.GetQueryParam("page"), out int p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out int ps) ? Math.Clamp(ps, 1, 200) : 50;

        using PwEngineContext context = ResolveContext();

        IQueryable<PersistedDialogueTree> query = context.DialogueTrees;

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(d =>
                d.DialogueTreeId.ToLower().Contains(term) ||
                d.Title.ToLower().Contains(term) ||
                (d.SpeakerTag != null && d.SpeakerTag.ToLower().Contains(term)));
        }

        int totalCount = await query.CountAsync();

        List<PersistedDialogueTree> items = await query
            .OrderBy(d => d.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

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

        using PwEngineContext context = ResolveContext();
        PersistedDialogueTree? definition = await context.DialogueTrees.FindAsync(dialogueTreeId);

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

        using PwEngineContext context = ResolveContext();
        List<PersistedDialogueTree> items = await context.DialogueTrees
            .Where(d => d.SpeakerTag == speakerTag)
            .OrderBy(d => d.Title)
            .ToListAsync();

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

        using PwEngineContext context = ResolveContext();

        bool exists = await context.DialogueTrees.AnyAsync(d => d.DialogueTreeId == dto.DialogueTreeId);
        if (exists)
        {
            return new ApiResult(409, new ErrorResponse(
                "Conflict", $"A dialogue tree with ID '{dto.DialogueTreeId}' already exists"));
        }

        PersistedDialogueTree entity = FromDto(dto);
        entity.CreatedUtc = DateTime.UtcNow;

        context.DialogueTrees.Add(entity);
        await context.SaveChangesAsync();

        // Dynamically register matching NPCs for conversation hook
        await TryRegisterNpcsAsync(entity.SpeakerTag, entity.DialogueTreeId);

        return new ApiResult(201, ToDto(entity));
    }

    /// <summary>
    /// Update an existing dialogue tree.
    /// PUT /api/worldengine/dialogue/{dialogueTreeId}
    /// </summary>
    [HttpPut(BasePath + "/{dialogueTreeId}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string dialogueTreeId = ctx.GetRouteValue("dialogueTreeId");

        using PwEngineContext context = ResolveContext();
        PersistedDialogueTree? existing = await context.DialogueTrees.FindAsync(dialogueTreeId);

        if (existing == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No dialogue tree with ID '{dialogueTreeId}'"));
        }

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

        // Update mutable fields — DialogueTreeId is immutable
        existing.Title = dto.Title.Trim();
        existing.Description = dto.Description ?? string.Empty;
        existing.RootNodeId = dto.RootNodeId;
        existing.SpeakerTag = string.IsNullOrWhiteSpace(dto.SpeakerTag) ? null : dto.SpeakerTag.Trim();
        existing.NodesJson = JsonSerializer.Serialize(dto.Nodes ?? [], JsonOpts);
        existing.UpdatedUtc = DateTime.UtcNow;

        await context.SaveChangesAsync();

        // Re-register NPCs — hook resolves old tag from its internal registry
        await TryUpdateNpcRegistrationAsync(dialogueTreeId, existing.SpeakerTag);

        return new ApiResult(200, ToDto(existing));
    }

    /// <summary>
    /// Delete a dialogue tree.
    /// DELETE /api/worldengine/dialogue/{dialogueTreeId}
    /// </summary>
    [HttpDelete(BasePath + "/{dialogueTreeId}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string dialogueTreeId = ctx.GetRouteValue("dialogueTreeId");

        using PwEngineContext context = ResolveContext();
        PersistedDialogueTree? existing = await context.DialogueTrees.FindAsync(dialogueTreeId);

        if (existing == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No dialogue tree with ID '{dialogueTreeId}'"));
        }

        // Unregister NPCs before deleting the tree (by treeId — only affects NPCs owned by this tree)
        await TryUnregisterNpcsAsync(dialogueTreeId);

        context.DialogueTrees.Remove(existing);
        await context.SaveChangesAsync();

        return new ApiResult(204, new { message = "Deleted" });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

    // ═══════════════════════════════════════════════════════════════════
    //  Dynamic NPC registration helpers
    // ═══════════════════════════════════════════════════════════════════

    private static async Task TryRegisterNpcsAsync(string? speakerTag, string dialogueTreeId)
    {
        if (string.IsNullOrWhiteSpace(speakerTag)) return;
        try
        {
            DialogueNpcHook? hook = AnvilCore.GetService<DialogueNpcHook>();
            if (hook == null)
            {
                Log.Warn("DialogueNpcHook service not available — skipping NPC registration");
                return;
            }

            int count = await hook.RegisterNpcsForTreeAsync(speakerTag, dialogueTreeId);
            Log.Info("Registered {Count} NPCs with tag '{Tag}' for dialogue tree '{TreeId}'",
                count, speakerTag, dialogueTreeId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to dynamically register NPCs for tag '{Tag}'", speakerTag);
        }
    }

    private static async Task TryUnregisterNpcsAsync(string dialogueTreeId)
    {
        if (string.IsNullOrWhiteSpace(dialogueTreeId)) return;
        try
        {
            DialogueNpcHook? hook = AnvilCore.GetService<DialogueNpcHook>();
            if (hook == null) return;

            int count = await hook.UnregisterNpcsForTreeAsync(dialogueTreeId);
            Log.Info("Unregistered {Count} NPCs for tree '{TreeId}'", count, dialogueTreeId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to dynamically unregister NPCs for tree '{TreeId}'", dialogueTreeId);
        }
    }

    private static async Task TryUpdateNpcRegistrationAsync(
        string dialogueTreeId, string? newSpeakerTag)
    {
        try
        {
            DialogueNpcHook? hook = AnvilCore.GetService<DialogueNpcHook>();
            if (hook == null) return;

            (int unregistered, int registered) = await hook.UpdateNpcRegistrationAsync(
                dialogueTreeId, newSpeakerTag);

            Log.Info(
                "Updated NPC registration for tree '{TreeId}': unregistered {Unregistered}, registered {Registered} (new tag '{NewTag}')",
                dialogueTreeId, unregistered, registered, newSpeakerTag);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update NPC registration for tree '{TreeId}'", dialogueTreeId);
        }
    }

    private static PwEngineContext ResolveContext()
    {
        PwContextFactory factory = AnvilCore.GetService<PwContextFactory>()
                                   ?? throw new InvalidOperationException("PwContextFactory service not available");
        return factory.CreateDbContext();
    }

    private static string? ValidateDto(DialogueTreeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DialogueTreeId)) return "DialogueTreeId is required";
        if (dto.DialogueTreeId.Length > 100) return "DialogueTreeId must not exceed 100 characters";
        if (string.IsNullOrWhiteSpace(dto.Title)) return "Title is required";
        if (dto.Title.Length > 200) return "Title must not exceed 200 characters";
        if (dto.SpeakerTag is { Length: > 64 }) return "SpeakerTag must not exceed 64 characters";

        // Validate nodes if present
        if (dto.Nodes != null)
        {
            foreach (DialogueNodeDto node in dto.Nodes)
            {
                if (string.IsNullOrWhiteSpace(node.Id))
                    return "Each node must have an Id";
            }
        }

        return null;
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

    // ═══════════════════════════════════════════════════════════════════
    //  DTOs (controller-local)
    // ═══════════════════════════════════════════════════════════════════

    private record DialogueTreeDto
    {
        public string DialogueTreeId { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? RootNodeId { get; init; }
        public string? SpeakerTag { get; init; }
        public List<DialogueNodeDto>? Nodes { get; init; }
    }

    private record DialogueNodeDto
    {
        public string Id { get; init; } = string.Empty;
        public string Type { get; init; } = "NpcText";
        public string? SpeakerTag { get; init; }
        public string Text { get; init; } = string.Empty;
        public int SortOrder { get; init; }
        public string? ParentNodeId { get; init; }
        public List<DialogueChoiceDto>? Choices { get; init; }
        public List<DialogueActionDto>? Actions { get; init; }
    }

    private record DialogueChoiceDto
    {
        public string TargetNodeId { get; init; } = string.Empty;
        public string ResponseText { get; init; } = string.Empty;
        public int SortOrder { get; init; }
        public List<DialogueConditionDto>? Conditions { get; init; }
    }

    private record DialogueConditionDto
    {
        public string Type { get; init; } = string.Empty;
        public Dictionary<string, string>? Parameters { get; init; }
    }

    private record DialogueActionDto
    {
        public string ActionType { get; init; } = string.Empty;
        public Dictionary<string, string>? Parameters { get; init; }
        public int ExecutionOrder { get; init; }
    }
}

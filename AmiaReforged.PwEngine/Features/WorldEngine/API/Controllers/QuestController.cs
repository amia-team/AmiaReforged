using System.Text.Json;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
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
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// List all quest definitions with optional search and pagination.
    /// GET /api/worldengine/codex/quests?search=&amp;page=1&amp;pageSize=50
    /// </summary>
    [HttpGet(BasePath)]
    public static async Task<ApiResult> GetAll(RouteContext ctx)
    {
        string? search = ctx.GetQueryParam("search");
        int page = int.TryParse(ctx.GetQueryParam("page"), out int p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out int ps) ? Math.Clamp(ps, 1, 200) : 50;

        using PwEngineContext context = ResolveContext();

        IQueryable<PersistedQuestDefinition> query = context.CodexQuestDefinitions;

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(d =>
                d.QuestId.ToLower().Contains(term) ||
                d.Title.ToLower().Contains(term) ||
                (d.Keywords != null && d.Keywords.ToLower().Contains(term)));
        }

        int totalCount = await query.CountAsync();

        List<PersistedQuestDefinition> items = await query
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
    /// Get a single quest definition by ID.
    /// GET /api/worldengine/codex/quests/{questId}
    /// </summary>
    [HttpGet(BasePath + "/{questId}")]
    public static async Task<ApiResult> GetById(RouteContext ctx)
    {
        string questId = ctx.GetRouteValue("questId");

        using PwEngineContext context = ResolveContext();
        PersistedQuestDefinition? definition = await context.CodexQuestDefinitions.FindAsync(questId);

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

        using PwEngineContext context = ResolveContext();

        bool exists = await context.CodexQuestDefinitions.AnyAsync(d => d.QuestId == dto.QuestId);
        if (exists)
        {
            return new ApiResult(409, new ErrorResponse(
                "Conflict", $"A quest definition with ID '{dto.QuestId}' already exists"));
        }

        PersistedQuestDefinition definition = FromDto(dto);
        definition.CreatedUtc = DateTime.UtcNow;

        context.CodexQuestDefinitions.Add(definition);
        await context.SaveChangesAsync();

        return new ApiResult(201, ToDto(definition));
    }

    /// <summary>
    /// Update an existing quest definition.
    /// PUT /api/worldengine/codex/quests/{questId}
    /// </summary>
    [HttpPut(BasePath + "/{questId}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string questId = ctx.GetRouteValue("questId");

        using PwEngineContext context = ResolveContext();
        PersistedQuestDefinition? existing = await context.CodexQuestDefinitions.FindAsync(questId);

        if (existing == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No quest definition with ID '{questId}'"));
        }

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

        // Update mutable fields — QuestId is immutable
        existing.Title = dto.Title;
        existing.Description = dto.Description;
        existing.StagesJson = SerializeStages(dto.Stages);
        existing.QuestGiver = string.IsNullOrWhiteSpace(dto.QuestGiver) ? null : dto.QuestGiver.Trim();
        existing.Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
        existing.Keywords = string.IsNullOrWhiteSpace(dto.Keywords) ? null : dto.Keywords.Trim();
        existing.IsAlwaysAvailable = dto.IsAlwaysAvailable;

        await context.SaveChangesAsync();

        return new ApiResult(200, ToDto(existing));
    }

    /// <summary>
    /// Delete a quest definition.
    /// DELETE /api/worldengine/codex/quests/{questId}
    /// </summary>
    [HttpDelete(BasePath + "/{questId}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string questId = ctx.GetRouteValue("questId");

        using PwEngineContext context = ResolveContext();
        PersistedQuestDefinition? existing = await context.CodexQuestDefinitions.FindAsync(questId);

        if (existing == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No quest definition with ID '{questId}'"));
        }

        context.CodexQuestDefinitions.Remove(existing);
        await context.SaveChangesAsync();

        return new ApiResult(204, new { message = "Deleted" });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

    private static PwEngineContext ResolveContext()
    {
        PwContextFactory factory = AnvilCore.GetService<PwContextFactory>()
                                   ?? throw new InvalidOperationException("PwContextFactory service not available");
        return factory.CreateDbContext();
    }

    private static string? ValidateDto(QuestDefinitionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.QuestId)) return "QuestId is required";
        if (dto.QuestId.Length > 100) return "QuestId must not exceed 100 characters";
        if (string.IsNullOrWhiteSpace(dto.Title)) return "Title is required";
        if (dto.Title.Length > 200) return "Title must not exceed 200 characters";
        if (string.IsNullOrWhiteSpace(dto.Description)) return "Description is required";
        if (dto.Keywords is { Length: > 1000 }) return "Keywords must not exceed 1000 characters";
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

    // ═══════════════════════════════════════════════════════════════════
    //  DTOs
    // ═══════════════════════════════════════════════════════════════════

    private record QuestStageJsonModel
    {
        public int StageId { get; init; }
        public string JournalText { get; init; } = string.Empty;
        public bool IsCompletionStage { get; init; }
        public List<string> Hints { get; init; } = [];
    }

    private record QuestDefinitionDto
    {
        public string QuestId { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public List<QuestStageJsonModel> Stages { get; init; } = [];
        public string? QuestGiver { get; init; }
        public string? Location { get; init; }
        public string? Keywords { get; init; }
        public bool IsAlwaysAvailable { get; init; }
    }
}

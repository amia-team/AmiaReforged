using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using Anvil;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for managing codex lore definitions.
/// Supports CRUD operations for the admin panel.
/// </summary>
public class LoreController
{
    private const string BasePath = "/api/worldengine/codex/lore";

    /// <summary>
    /// List all lore definitions with optional search, category filter, and pagination.
    /// GET /api/worldengine/codex/lore?search=&amp;category=&amp;page=1&amp;pageSize=50
    /// </summary>
    [HttpGet(BasePath)]
    public static async Task<ApiResult> GetAll(RouteContext ctx)
    {
        string? search = ctx.GetQueryParam("search");
        string? category = ctx.GetQueryParam("category");
        int page = int.TryParse(ctx.GetQueryParam("page"), out int p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out int ps) ? Math.Clamp(ps, 1, 200) : 50;

        using PwEngineContext context = ResolveContext();

        IQueryable<PersistedLoreDefinition> query = context.CodexLoreDefinitions;

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(d =>
                d.LoreId.ToLower().Contains(term) ||
                d.Title.ToLower().Contains(term) ||
                (d.Keywords != null && d.Keywords.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            if (int.TryParse(category.Trim(), out int catInt) && Enum.IsDefined(typeof(LoreCategory), catInt))
                query = query.Where(d => d.Category == (LoreCategory)catInt);
        }

        int totalCount = await query.CountAsync();

        List<PersistedLoreDefinition> items = await query
            .OrderBy(d => d.Category)
            .ThenBy(d => d.Title)
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
    /// Get a single lore definition by ID.
    /// GET /api/worldengine/codex/lore/{loreId}
    /// </summary>
    [HttpGet(BasePath + "/{loreId}")]
    public static async Task<ApiResult> GetById(RouteContext ctx)
    {
        string loreId = ctx.GetRouteValue("loreId");

        using PwEngineContext context = ResolveContext();
        PersistedLoreDefinition? definition = await context.CodexLoreDefinitions.FindAsync(loreId);

        if (definition == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No lore definition with ID '{loreId}'"));
        }

        return new ApiResult(200, ToDto(definition));
    }

    /// <summary>
    /// Create a new lore definition.
    /// POST /api/worldengine/codex/lore
    /// </summary>
    [HttpPost(BasePath)]
    public static async Task<ApiResult> Create(RouteContext ctx)
    {
        LoreDefinitionDto? dto = await ctx.ReadJsonBodyAsync<LoreDefinitionDto>();
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

        // Check for duplicate ID
        bool exists = await context.CodexLoreDefinitions.AnyAsync(d => d.LoreId == dto.LoreId);
        if (exists)
        {
            return new ApiResult(409, new ErrorResponse(
                "Conflict", $"A lore definition with ID '{dto.LoreId}' already exists"));
        }

        PersistedLoreDefinition definition = FromDto(dto);
        definition.CreatedUtc = DateTime.UtcNow;

        context.CodexLoreDefinitions.Add(definition);
        await context.SaveChangesAsync();

        return new ApiResult(201, ToDto(definition));
    }

    /// <summary>
    /// Update an existing lore definition.
    /// PUT /api/worldengine/codex/lore/{loreId}
    /// </summary>
    [HttpPut(BasePath + "/{loreId}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string loreId = ctx.GetRouteValue("loreId");

        using PwEngineContext context = ResolveContext();
        PersistedLoreDefinition? existing = await context.CodexLoreDefinitions.FindAsync(loreId);

        if (existing == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No lore definition with ID '{loreId}'"));
        }

        LoreDefinitionDto? dto = await ctx.ReadJsonBodyAsync<LoreDefinitionDto>();
        if (dto == null)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required"));
        }

        string? validationError = ValidateDto(dto);
        if (validationError != null)
        {
            return new ApiResult(400, new ErrorResponse("Validation failed", validationError));
        }

        // Update mutable fields — LoreId is immutable
        existing.Title = dto.Title;
        existing.Content = dto.Content;
        existing.Category = (LoreCategory)dto.Category;
        existing.Tier = dto.Tier;
        existing.Keywords = dto.Keywords;
        existing.IsAlwaysAvailable = dto.IsAlwaysAvailable;

        await context.SaveChangesAsync();

        return new ApiResult(200, ToDto(existing));
    }

    /// <summary>
    /// Delete a lore definition and all associated unlock records.
    /// DELETE /api/worldengine/codex/lore/{loreId}
    /// </summary>
    [HttpDelete(BasePath + "/{loreId}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string loreId = ctx.GetRouteValue("loreId");

        using PwEngineContext context = ResolveContext();
        PersistedLoreDefinition? existing = await context.CodexLoreDefinitions.FindAsync(loreId);

        if (existing == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No lore definition with ID '{loreId}'"));
        }

        // Remove all player unlock records for this lore entry
        await context.CodexLoreUnlocks
            .Where(u => u.LoreId == loreId)
            .ExecuteDeleteAsync();

        context.CodexLoreDefinitions.Remove(existing);
        await context.SaveChangesAsync();

        return new ApiResult(204, new { message = "Deleted" });
    }

    /// <summary>
    /// Get all distinct categories currently in use.
    /// GET /api/worldengine/codex/lore/categories
    /// </summary>
    [HttpGet(BasePath + "/categories")]
    public static async Task<ApiResult> GetCategories(RouteContext ctx)
    {
        var categories = Enum.GetValues<LoreCategory>()
            .Select(c => new { id = (int)c, name = c.DisplayName() })
            .OrderBy(c => c.id)
            .ToList();

        return new ApiResult(200, categories);
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

    private static string? ValidateDto(LoreDefinitionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.LoreId)) return "LoreId is required";
        if (dto.LoreId.Length > 100) return "LoreId must not exceed 100 characters";
        if (string.IsNullOrWhiteSpace(dto.Title)) return "Title is required";
        if (dto.Title.Length > 200) return "Title must not exceed 200 characters";
        if (string.IsNullOrWhiteSpace(dto.Content)) return "Content is required";
        if (!Enum.IsDefined(typeof(LoreCategory), dto.Category)) return $"Category must be a valid LoreCategory (0–{(int)LoreCategory.Ooc})";
        if (dto.Tier is < 0 or > 3) return "Tier must be between 0 (Common) and 3 (Legendary)";
        if (dto.Keywords is { Length: > 1000 }) return "Keywords must not exceed 1000 characters";
        return null;
    }

    private static object ToDto(PersistedLoreDefinition def)
    {
        return new
        {
            def.LoreId,
            def.Title,
            def.Content,
            Category = (int)def.Category,
            CategoryName = def.Category.DisplayName(),
            def.Tier,
            def.Keywords,
            def.IsAlwaysAvailable,
            def.CreatedUtc
        };
    }

    private static PersistedLoreDefinition FromDto(LoreDefinitionDto dto)
    {
        return new PersistedLoreDefinition
        {
            LoreId = dto.LoreId.Trim(),
            Title = dto.Title.Trim(),
            Content = dto.Content,
            Category = (LoreCategory)dto.Category,
            Tier = dto.Tier,
            Keywords = string.IsNullOrWhiteSpace(dto.Keywords) ? null : dto.Keywords.Trim(),
            IsAlwaysAvailable = dto.IsAlwaysAvailable
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DTO
    // ═══════════════════════════════════════════════════════════════════

    private record LoreDefinitionDto
    {
        public string LoreId { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public int Category { get; init; }
        public int Tier { get; init; }
        public string? Keywords { get; init; }
        public bool IsAlwaysAvailable { get; init; }
    }
}

using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;
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
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        string? search = ctx.GetQueryParam("search");
        string? category = ctx.GetQueryParam("category");
        int page = int.TryParse(ctx.GetQueryParam("page"), out int p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out int ps) ? Math.Clamp(ps, 1, 200) : 50;

        int? categoryFilter = int.TryParse(category?.Trim(), out int catInt)
            && Enum.IsDefined(typeof(LoreCategory), catInt) ? catInt : null;

        List<PersistedLoreDefinition> matches = await facade.QueryAsync<SearchLoreDefinitionsQuery, List<PersistedLoreDefinition>>(
            new SearchLoreDefinitionsQuery { SearchTerm = search, Category = categoryFilter }, ctx.CancellationToken);

        int totalCount = matches.Count;
        List<PersistedLoreDefinition> items = matches
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
    /// Get a single lore definition by ID.
    /// GET /api/worldengine/codex/lore/{loreId}
    /// </summary>
    [HttpGet(BasePath + "/{loreId}")]
    public static async Task<ApiResult> GetById(RouteContext ctx)
    {
        string loreId = ctx.GetRouteValue("loreId");

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        PersistedLoreDefinition? definition = await facade.QueryAsync<GetLoreDefinitionQuery, PersistedLoreDefinition?>(
            new GetLoreDefinitionQuery { LoreId = loreId }, ctx.CancellationToken);

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

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        PersistedLoreDefinition definition = FromDto(dto);
        CommandResult result = await facade.ExecuteAsync(new CreateLoreDefinitionCommand
        {
            Definition = definition
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool conflict = result.ErrorMessage?.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true;
            return new ApiResult(conflict ? 409 : 400,
                new ErrorResponse(conflict ? "Conflict" : "Command failed", result.ErrorMessage));
        }

        PersistedLoreDefinition? created = await facade.QueryAsync<GetLoreDefinitionQuery, PersistedLoreDefinition?>(
            new GetLoreDefinitionQuery { LoreId = definition.LoreId }, ctx.CancellationToken);

        return new ApiResult(201, ToDto(created ?? definition));
    }

    /// <summary>
    /// Update an existing lore definition.
    /// PUT /api/worldengine/codex/lore/{loreId}
    /// </summary>
    [HttpPut(BasePath + "/{loreId}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string loreId = ctx.GetRouteValue("loreId");

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

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        PersistedLoreDefinition definition = FromDto(dto);
        definition.LoreId = loreId;
        CommandResult result = await facade.ExecuteAsync(new UpdateLoreDefinitionCommand
        {
            LoreId = loreId,
            Definition = definition
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool notFound = result.ErrorMessage?.StartsWith("No lore definition with ID", StringComparison.OrdinalIgnoreCase) == true;
            return new ApiResult(notFound ? 404 : 400, new ErrorResponse(
                notFound ? "Not found" : "Command failed", result.ErrorMessage));
        }

        PersistedLoreDefinition? updated = await facade.QueryAsync<GetLoreDefinitionQuery, PersistedLoreDefinition?>(
            new GetLoreDefinitionQuery { LoreId = loreId }, ctx.CancellationToken);

        return new ApiResult(200, ToDto(updated ?? definition));
    }

    /// <summary>
    /// Delete a lore definition and all associated unlock records.
    /// DELETE /api/worldengine/codex/lore/{loreId}
    /// </summary>
    [HttpDelete(BasePath + "/{loreId}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string loreId = ctx.GetRouteValue("loreId");

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        CommandResult result = await facade.ExecuteAsync(new DeleteLoreDefinitionCommand
        {
            LoreId = loreId
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", result.ErrorMessage));
        }

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

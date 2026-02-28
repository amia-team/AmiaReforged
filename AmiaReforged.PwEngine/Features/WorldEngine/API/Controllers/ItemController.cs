using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.Persistence;
using Anvil;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for managing item blueprint definitions.
/// Supports CRUD operations and bulk JSON import for the admin panel.
/// </summary>
public class ItemController
{
    private static readonly JsonSerializerOptions ImportOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// List all items with optional search and pagination.
    /// GET /api/worldengine/items?search=&amp;page=1&amp;pageSize=50
    /// </summary>
    [HttpGet("/api/worldengine/items")]
    public static async Task<ApiResult> GetAll(RouteContext ctx)
    {
        var repo = ResolveRepository();
        if (repo is DbItemDefinitionRepository dbRepo)
        {
            string? search = ctx.GetQueryParam("search");
            int page = int.TryParse(ctx.GetQueryParam("page"), out var p) ? Math.Max(1, p) : 1;
            int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out var ps) ? Math.Clamp(ps, 1, 200) : 50;

            var items = dbRepo.Search(search, page, pageSize, out int totalCount);

            return await Task.FromResult(new ApiResult(200, new
            {
                items = items.Select(ToDto),
                totalCount,
                page,
                pageSize
            }));
        }

        // Fallback for non-DB repos
        var allItems = repo.AllItems();
        return await Task.FromResult(new ApiResult(200, new
        {
            items = allItems.Select(ToDto),
            totalCount = allItems.Count,
            page = 1,
            pageSize = allItems.Count
        }));
    }

    /// <summary>
    /// Get a single item by tag.
    /// GET /api/worldengine/items/{tag}
    /// </summary>
    [HttpGet("/api/worldengine/items/{tag}")]
    public static async Task<ApiResult> GetByTag(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");
        var repo = ResolveRepository();

        var item = repo.GetByTag(tag);
        if (item == null)
        {
            return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                "Not found", $"No item with tag '{tag}'")));
        }

        return await Task.FromResult(new ApiResult(200, ToDto(item)));
    }

    /// <summary>
    /// Create a new item blueprint.
    /// POST /api/worldengine/items
    /// </summary>
    [HttpPost("/api/worldengine/items")]
    public static async Task<ApiResult> Create(RouteContext ctx)
    {
        var dto = await ctx.ReadJsonBodyAsync<ItemBlueprintDto>();
        if (dto == null)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required"));
        }

        string? validationError = ValidateDto(dto);
        if (validationError != null)
        {
            return new ApiResult(400, new ErrorResponse("Validation failed", validationError));
        }

        var repo = ResolveRepository();
        var blueprint = FromDto(dto);
        repo.AddItemDefinition(blueprint);

        return new ApiResult(201, ToDto(blueprint));
    }

    /// <summary>
    /// Update an existing item blueprint by tag.
    /// PUT /api/worldengine/items/{tag}
    /// </summary>
    [HttpPut("/api/worldengine/items/{tag}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");
        var repo = ResolveRepository();

        var existing = repo.GetByTag(tag);
        if (existing == null)
        {
            return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                "Not found", $"No item with tag '{tag}'")));
        }

        var dto = await ctx.ReadJsonBodyAsync<ItemBlueprintDto>();
        if (dto == null)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required"));
        }

        string? validationError = ValidateDto(dto);
        if (validationError != null)
        {
            return new ApiResult(400, new ErrorResponse("Validation failed", validationError));
        }

        var blueprint = FromDto(dto);
        repo.AddItemDefinition(blueprint);

        return new ApiResult(200, ToDto(blueprint));
    }

    /// <summary>
    /// Delete an item blueprint by tag.
    /// DELETE /api/worldengine/items/{tag}
    /// </summary>
    [HttpDelete("/api/worldengine/items/{tag}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");
        var repo = ResolveRepository();

        if (repo is DbItemDefinitionRepository dbRepo)
        {
            bool deleted = dbRepo.DeleteByTag(tag);
            if (!deleted)
            {
                return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                    "Not found", $"No item with tag '{tag}'")));
            }
        }
        else
        {
            return await Task.FromResult(new ApiResult(501, new ErrorResponse(
                "Not implemented", "Delete is only supported with database-backed repositories")));
        }

        return await Task.FromResult(new ApiResult(204, new { message = "Deleted" }));
    }

    /// <summary>
    /// Bulk import item blueprints from JSON.
    /// POST /api/worldengine/items/import
    /// Body: JSON array of item blueprints (same format as existing JSON files).
    /// </summary>
    [HttpPost("/api/worldengine/items/import")]
    public static async Task<ApiResult> Import(RouteContext ctx)
    {
        var repo = ResolveRepository();

        string? body = null;
        if (ctx.Request != null)
        {
            using var reader = new StreamReader(ctx.Request.InputStream);
            body = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body must be a JSON array of items"));
        }

        List<ItemBlueprint>? items;
        try
        {
            // Try array first
            items = JsonSerializer.Deserialize<List<ItemBlueprint>>(body, ImportOptions);
        }
        catch (JsonException)
        {
            // Try single item
            try
            {
                var single = JsonSerializer.Deserialize<ItemBlueprint>(body, ImportOptions);
                items = single != null ? new List<ItemBlueprint> { single } : null;
            }
            catch (JsonException ex)
            {
                return new ApiResult(400, new ErrorResponse("Parse error", ex.Message));
            }
        }

        if (items == null || items.Count == 0)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "No valid items found in request body"));
        }

        int succeeded = 0;
        int failed = 0;
        List<string> errors = new();

        foreach (var item in items)
        {
            try
            {
                string? validationError = ValidateBlueprint(item);
                if (validationError != null)
                {
                    failed++;
                    errors.Add($"{item.ItemTag ?? item.ResRef ?? "unknown"}: {validationError}");
                    continue;
                }

                repo.AddItemDefinition(item);
                succeeded++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{item.ItemTag ?? item.ResRef ?? "unknown"}: {ex.Message}");
            }
        }

        return new ApiResult(200, new
        {
            succeeded,
            failed,
            total = items.Count,
            errors
        });
    }

    private static IItemDefinitionRepository ResolveRepository()
    {
        return AnvilCore.GetService<IItemDefinitionRepository>()
               ?? throw new InvalidOperationException("IItemDefinitionRepository service not available");
    }

    private static string? ValidateDto(ItemBlueprintDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ResRef)) return "ResRef is required";
        if (dto.ResRef.Length > 16) return "ResRef must not exceed 16 characters";
        if (string.IsNullOrWhiteSpace(dto.ItemTag)) return "ItemTag is required";
        if (string.IsNullOrWhiteSpace(dto.Name)) return "Name is required";
        return null;
    }

    private static string? ValidateBlueprint(ItemBlueprint bp)
    {
        if (string.IsNullOrWhiteSpace(bp.ResRef)) return "ResRef is required";
        if (bp.ResRef.Length > 16) return "ResRef must not exceed 16 characters";
        if (string.IsNullOrWhiteSpace(bp.ItemTag)) return "ItemTag is required";
        if (string.IsNullOrWhiteSpace(bp.Name)) return "Name is required";
        return null;
    }

    private static object ToDto(ItemBlueprint bp)
    {
        return new
        {
            bp.ResRef,
            bp.ItemTag,
            bp.Name,
            bp.Description,
            Materials = bp.Materials.Select(m => m.ToString()).ToArray(),
            JobSystemType = bp.JobSystemType.ToString(),
            bp.BaseItemType,
            Appearance = new
            {
                bp.Appearance.ModelType,
                bp.Appearance.SimpleModelNumber,
                Data = bp.Appearance.Data != null
                    ? new
                    {
                        bp.Appearance.Data.TopPartModel,
                        bp.Appearance.Data.MiddlePartModel,
                        bp.Appearance.Data.BottomPartModel,
                        bp.Appearance.Data.TopPartColor,
                        bp.Appearance.Data.MiddlePartColor,
                        bp.Appearance.Data.BottomPartColor
                    }
                    : null
            },
            bp.LocalVariables,
            bp.BaseValue,
            bp.WeightIncreaseConstant,
            bp.SourceFile
        };
    }

    private static ItemBlueprint FromDto(ItemBlueprintDto dto)
    {
        MaterialEnum[] materials = (dto.Materials ?? Array.Empty<string>())
            .Select(s => Enum.TryParse<MaterialEnum>(s, true, out var m) ? m : MaterialEnum.None)
            .ToArray();

        Enum.TryParse<JobSystemItemType>(dto.JobSystemType, true, out var jobType);

        WeaponPartData? weaponData = null;
        if (dto.Appearance?.Data != null)
        {
            weaponData = new WeaponPartData(
                dto.Appearance.Data.TopPartModel,
                dto.Appearance.Data.MiddlePartModel,
                dto.Appearance.Data.BottomPartModel,
                dto.Appearance.Data.TopPartColor,
                dto.Appearance.Data.MiddlePartColor,
                dto.Appearance.Data.BottomPartColor);
        }

        var appearance = new AppearanceData(
            dto.Appearance?.ModelType ?? 0,
            dto.Appearance?.SimpleModelNumber,
            weaponData);

        return new ItemBlueprint(
            ResRef: dto.ResRef,
            ItemTag: dto.ItemTag,
            Name: dto.Name,
            Description: dto.Description ?? string.Empty,
            Materials: materials,
            JobSystemType: jobType,
            BaseItemType: dto.BaseItemType,
            Appearance: appearance,
            LocalVariables: null, // Local variables handled via raw JSON in import
            BaseValue: dto.BaseValue,
            WeightIncreaseConstant: dto.WeightIncreaseConstant)
        {
            SourceFile = dto.SourceFile
        };
    }

    // DTO classes for request/response
    private record ItemBlueprintDto
    {
        public string ResRef { get; init; } = string.Empty;
        public string ItemTag { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string[]? Materials { get; init; }
        public string? JobSystemType { get; init; }
        public int BaseItemType { get; init; }
        public AppearanceDto? Appearance { get; init; }
        public int BaseValue { get; init; } = 1;
        public int WeightIncreaseConstant { get; init; } = -1;
        public string? SourceFile { get; init; }
    }

    private record AppearanceDto
    {
        public int ModelType { get; init; }
        public int? SimpleModelNumber { get; init; }
        public WeaponPartDto? Data { get; init; }
    }

    private record WeaponPartDto
    {
        public int TopPartModel { get; init; }
        public int MiddlePartModel { get; init; }
        public int BottomPartModel { get; init; }
        public int TopPartColor { get; init; }
        public int MiddlePartColor { get; init; }
        public int BottomPartColor { get; init; }
    }
}

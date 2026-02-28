using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Persistence;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for managing resource node definitions.
/// Supports CRUD operations and bulk JSON import for the admin panel.
/// </summary>
public class ResourceNodeController
{
    private static readonly JsonSerializerOptions ImportOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// List all resource nodes with optional search, type filter, and pagination.
    /// GET /api/worldengine/resource-nodes?search=&amp;type=&amp;page=1&amp;pageSize=50
    /// </summary>
    [HttpGet("/api/worldengine/resource-nodes")]
    public static async Task<ApiResult> GetAll(RouteContext ctx)
    {
        var repo = ResolveRepository();
        if (repo is DbResourceNodeDefinitionRepository dbRepo)
        {
            string? search = ctx.GetQueryParam("search");
            string? type = ctx.GetQueryParam("type");
            int page = int.TryParse(ctx.GetQueryParam("page"), out var p) ? Math.Max(1, p) : 1;
            int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out var ps) ? Math.Clamp(ps, 1, 200) : 50;

            var nodes = dbRepo.Search(search, type, page, pageSize, out int totalCount);

            return await Task.FromResult(new ApiResult(200, new
            {
                items = nodes.Select(ToDto),
                totalCount,
                page,
                pageSize
            }));
        }

        // Fallback for non-DB repos
        var allNodes = repo.All();
        return await Task.FromResult(new ApiResult(200, new
        {
            items = allNodes.Select(ToDto),
            totalCount = allNodes.Count,
            page = 1,
            pageSize = allNodes.Count
        }));
    }

    /// <summary>
    /// Get a single resource node by tag.
    /// GET /api/worldengine/resource-nodes/{tag}
    /// </summary>
    [HttpGet("/api/worldengine/resource-nodes/{tag}")]
    public static async Task<ApiResult> GetByTag(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");
        var repo = ResolveRepository();

        var node = repo.Get(tag);
        if (node == null)
        {
            return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                "Not found", $"No resource node with tag '{tag}'")));
        }

        return await Task.FromResult(new ApiResult(200, ToDto(node)));
    }

    /// <summary>
    /// Create a new resource node definition.
    /// POST /api/worldengine/resource-nodes
    /// </summary>
    [HttpPost("/api/worldengine/resource-nodes")]
    public static async Task<ApiResult> Create(RouteContext ctx)
    {
        var dto = await ctx.ReadJsonBodyAsync<ResourceNodeDto>();
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
        var definition = FromDto(dto);
        repo.Create(definition);

        return new ApiResult(201, ToDto(definition));
    }

    /// <summary>
    /// Update an existing resource node definition by tag.
    /// PUT /api/worldengine/resource-nodes/{tag}
    /// </summary>
    [HttpPut("/api/worldengine/resource-nodes/{tag}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");
        var repo = ResolveRepository();

        var existing = repo.Get(tag);
        if (existing == null)
        {
            return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                "Not found", $"No resource node with tag '{tag}'")));
        }

        var dto = await ctx.ReadJsonBodyAsync<ResourceNodeDto>();
        if (dto == null)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required"));
        }

        string? validationError = ValidateDto(dto);
        if (validationError != null)
        {
            return new ApiResult(400, new ErrorResponse("Validation failed", validationError));
        }

        var definition = FromDto(dto);
        repo.Update(definition);

        return new ApiResult(200, ToDto(definition));
    }

    /// <summary>
    /// Delete a resource node definition by tag.
    /// DELETE /api/worldengine/resource-nodes/{tag}
    /// </summary>
    [HttpDelete("/api/worldengine/resource-nodes/{tag}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");
        var repo = ResolveRepository();

        bool deleted = repo.Delete(tag);
        if (!deleted)
        {
            return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                "Not found", $"No resource node with tag '{tag}'")));
        }

        return await Task.FromResult(new ApiResult(204, new { message = "Deleted" }));
    }

    /// <summary>
    /// Bulk import resource node definitions from JSON.
    /// POST /api/worldengine/resource-nodes/import
    /// Body: JSON array of resource node definitions (same format as existing JSON files).
    /// </summary>
    [HttpPost("/api/worldengine/resource-nodes/import")]
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
            return new ApiResult(400, new ErrorResponse("Bad request",
                "Request body must be a JSON array of resource node definitions"));
        }

        List<ResourceNodeDefinition>? nodes;
        try
        {
            nodes = JsonSerializer.Deserialize<List<ResourceNodeDefinition>>(body, ImportOptions);
        }
        catch (JsonException)
        {
            try
            {
                var single = JsonSerializer.Deserialize<ResourceNodeDefinition>(body, ImportOptions);
                nodes = single != null ? new List<ResourceNodeDefinition> { single } : null;
            }
            catch (JsonException ex)
            {
                return new ApiResult(400, new ErrorResponse("Parse error", ex.Message));
            }
        }

        if (nodes == null || nodes.Count == 0)
        {
            return new ApiResult(400, new ErrorResponse("Bad request",
                "No valid resource node definitions found in request body"));
        }

        int succeeded = 0;
        int failed = 0;
        List<string> errors = new();

        foreach (var node in nodes)
        {
            try
            {
                string? validationError = ValidateDefinition(node);
                if (validationError != null)
                {
                    failed++;
                    errors.Add($"{node.Tag ?? "unknown"}: {validationError}");
                    continue;
                }

                repo.Create(node);
                succeeded++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{node.Tag ?? "unknown"}: {ex.Message}");
            }
        }

        return new ApiResult(200, new
        {
            succeeded,
            failed,
            total = nodes.Count,
            errors
        });
    }

    private static IResourceNodeDefinitionRepository ResolveRepository()
    {
        return AnvilCore.GetService<IResourceNodeDefinitionRepository>()
               ?? throw new InvalidOperationException("IResourceNodeDefinitionRepository service not available");
    }

    private static string? ValidateDto(ResourceNodeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Tag)) return "Tag is required";
        if (dto.Outputs == null || dto.Outputs.Length == 0) return "At least one output is required";
        return null;
    }

    private static string? ValidateDefinition(ResourceNodeDefinition def)
    {
        if (string.IsNullOrWhiteSpace(def.Tag)) return "Tag is required";
        if (def.Outputs == null || def.Outputs.Length == 0) return "At least one output is required";
        if (def.Requirement == null) return "Requirement must be set";
        return null;
    }

    private static object ToDto(ResourceNodeDefinition def)
    {
        return new
        {
            def.Tag,
            def.Name,
            def.Description,
            def.PlcAppearance,
            Type = def.Type.ToString(),
            def.Uses,
            def.BaseHarvestRounds,
            Requirement = new
            {
                RequiredItemType = def.Requirement.RequiredItemType.ToString(),
                RequiredItemMaterial = def.Requirement.RequiredItemMaterial.ToString()
            },
            Outputs = def.Outputs.Select(o => new
            {
                o.ItemDefinitionTag,
                o.Quantity,
                o.Chance
            }).ToArray(),
            FloraProperties = def.FloraProperties != null
                ? new
                {
                    PreferredClimate = def.FloraProperties.PreferredClimate.ToString(),
                    RequiredSoilQuality = def.FloraProperties.RequiredSoilQuality.ToString()
                }
                : null
        };
    }

    private static ResourceNodeDefinition FromDto(ResourceNodeDto dto)
    {
        Enum.TryParse<ResourceType>(dto.Type, true, out var resourceType);

        Enum.TryParse<JobSystemItemType>(dto.Requirement?.RequiredItemType, true, out var reqItemType);
        Enum.TryParse<MaterialEnum>(dto.Requirement?.RequiredItemMaterial, true, out var reqMaterial);

        var requirement = new HarvestContext(reqItemType, reqMaterial);

        var outputs = (dto.Outputs ?? Array.Empty<HarvestOutputDto>())
            .Select(o => new HarvestOutput(o.ItemDefinitionTag, o.Quantity, o.Chance))
            .ToArray();

        FloraProperties? floraProps = null;
        if (dto.FloraProperties != null)
        {
            Enum.TryParse<Climate>(dto.FloraProperties.PreferredClimate, true, out var climate);
            Enum.TryParse<EconomyQuality>(dto.FloraProperties.RequiredSoilQuality, true, out var soilQuality);
            floraProps = new FloraProperties(climate, soilQuality);
        }

        return new ResourceNodeDefinition(
            PlcAppearance: dto.PlcAppearance,
            Type: resourceType,
            Tag: dto.Tag,
            Requirement: requirement,
            Outputs: outputs,
            Uses: dto.Uses,
            BaseHarvestRounds: dto.BaseHarvestRounds,
            Name: dto.Name ?? string.Empty,
            Description: dto.Description ?? string.Empty,
            FloraProperties: floraProps);
    }

    // DTO classes for request/response
    private record ResourceNodeDto
    {
        public string Tag { get; init; } = string.Empty;
        public string? Name { get; init; }
        public string? Description { get; init; }
        public int PlcAppearance { get; init; }
        public string? Type { get; init; }
        public int Uses { get; init; } = 50;
        public int BaseHarvestRounds { get; init; }
        public RequirementDto? Requirement { get; init; }
        public HarvestOutputDto[]? Outputs { get; init; }
        public FloraPropertiesDto? FloraProperties { get; init; }
    }

    private record RequirementDto
    {
        public string? RequiredItemType { get; init; }
        public string? RequiredItemMaterial { get; init; }
    }

    private record HarvestOutputDto
    {
        public string ItemDefinitionTag { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public int Chance { get; init; } = 100;
    }

    private record FloraPropertiesDto
    {
        public string? PreferredClimate { get; init; }
        public string? RequiredSoilQuality { get; init; }
    }
}

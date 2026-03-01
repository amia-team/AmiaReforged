using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Persistence;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for managing region definitions.
/// Supports CRUD operations, bulk JSON import/export for the admin panel.
/// </summary>
public class RegionController
{
    private static readonly JsonSerializerOptions ImportOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// List all regions with optional search and pagination.
    /// GET /api/worldengine/regions?search=&amp;page=1&amp;pageSize=50
    /// </summary>
    [HttpGet("/api/worldengine/regions")]
    public static async Task<ApiResult> GetAll(RouteContext ctx)
    {
        var repo = ResolveRepository();
        string? search = ctx.GetQueryParam("search");
        int page = int.TryParse(ctx.GetQueryParam("page"), out var p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out var ps) ? Math.Clamp(ps, 1, 200) : 50;

        List<RegionDefinition> paged;
        int totalCount;

        if (repo is DbRegionRepository dbRepo)
        {
            paged = dbRepo.Search(search, page, pageSize, out totalCount);
        }
        else
        {
            var allRegions = repo.All();
            if (!string.IsNullOrWhiteSpace(search))
            {
                allRegions = allRegions.Where(r =>
                    r.Tag.Value.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    r.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            totalCount = allRegions.Count;
            paged = allRegions
                .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        return await Task.FromResult(new ApiResult(200, new
        {
            items = paged.Select(ToDto),
            totalCount,
            page,
            pageSize
        }));
    }

    /// <summary>
    /// Get a single region by tag.
    /// GET /api/worldengine/regions/{tag}
    /// </summary>
    [HttpGet("/api/worldengine/regions/{tag}")]
    public static async Task<ApiResult> GetByTag(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");
        var repo = ResolveRepository();

        var region = repo.All().FirstOrDefault(r =>
            r.Tag.Value.Equals(tag, StringComparison.OrdinalIgnoreCase));
        if (region == null)
        {
            return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                "Not found", $"No region with tag '{tag}'")));
        }

        return await Task.FromResult(new ApiResult(200, ToDto(region)));
    }

    /// <summary>
    /// Create a new region definition.
    /// POST /api/worldengine/regions
    /// </summary>
    [HttpPost("/api/worldengine/regions")]
    public static async Task<ApiResult> Create(RouteContext ctx)
    {
        var dto = await ctx.ReadJsonBodyAsync<RegionDto>();
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
        repo.Add(definition);

        return new ApiResult(201, ToDto(definition));
    }

    /// <summary>
    /// Update an existing region definition by tag.
    /// PUT /api/worldengine/regions/{tag}
    /// </summary>
    [HttpPut("/api/worldengine/regions/{tag}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");
        var repo = ResolveRepository();

        var existing = repo.All().FirstOrDefault(r =>
            r.Tag.Value.Equals(tag, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
        {
            return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                "Not found", $"No region with tag '{tag}'")));
        }

        var dto = await ctx.ReadJsonBodyAsync<RegionDto>();
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
    /// Delete a region definition by tag.
    /// DELETE /api/worldengine/regions/{tag}
    /// </summary>
    [HttpDelete("/api/worldengine/regions/{tag}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");
        var repo = ResolveRepository();

        bool deleted = repo.Delete(new RegionTag(tag));
        if (!deleted)
        {
            return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                "Not found", $"No region with tag '{tag}'")));
        }

        return await Task.FromResult(new ApiResult(204, new { message = "Deleted" }));
    }

    /// <summary>
    /// Export all region definitions (optionally filtered) as a JSON array.
    /// GET /api/worldengine/regions/export?search=
    /// </summary>
    [HttpGet("/api/worldengine/regions/export")]
    public static async Task<ApiResult> Export(RouteContext ctx)
    {
        var repo = ResolveRepository();
        string? search = ctx.GetQueryParam("search");

        var regions = repo.All();
        if (!string.IsNullOrWhiteSpace(search))
        {
            regions = regions.Where(r =>
                r.Tag.Value.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return await Task.FromResult(new ApiResult(200,
            regions.OrderBy(r => r.Name).Select(ToDto).ToArray()));
    }

    /// <summary>
    /// Bulk import region definitions from JSON.
    /// POST /api/worldengine/regions/import
    /// Body: JSON array of region definitions.
    /// </summary>
    [HttpPost("/api/worldengine/regions/import")]
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
                "Request body must be a JSON array of region definitions"));
        }

        List<RegionDto>? dtos;
        try
        {
            dtos = JsonSerializer.Deserialize<List<RegionDto>>(body, ImportOptions);
        }
        catch (JsonException)
        {
            try
            {
                var single = JsonSerializer.Deserialize<RegionDto>(body, ImportOptions);
                dtos = single != null ? new List<RegionDto> { single } : null;
            }
            catch (JsonException ex)
            {
                return new ApiResult(400, new ErrorResponse("Parse error", ex.Message));
            }
        }

        if (dtos == null || dtos.Count == 0)
        {
            return new ApiResult(400, new ErrorResponse("Bad request",
                "No valid region definitions found in request body"));
        }

        int succeeded = 0;
        int failed = 0;
        List<string> errors = new();

        foreach (var dto in dtos)
        {
            try
            {
                string? validationError = ValidateDto(dto);
                if (validationError != null)
                {
                    failed++;
                    errors.Add($"{dto.Tag ?? "unknown"}: {validationError}");
                    continue;
                }

                var definition = FromDto(dto);
                repo.Add(definition); // Acts as upsert — replaces by tag key
                succeeded++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{dto.Tag ?? "unknown"}: {ex.Message}");
            }
        }

        return new ApiResult(200, new
        {
            succeeded,
            failed,
            total = dtos.Count,
            errors
        });
    }

    private static IRegionRepository ResolveRepository()
    {
        return AnvilCore.GetService<IRegionRepository>()
               ?? throw new InvalidOperationException("IRegionRepository service not available");
    }

    private static string? ValidateDto(RegionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Tag)) return "Tag is required";
        if (string.IsNullOrWhiteSpace(dto.Name)) return "Name is required";
        if (dto.Areas == null || dto.Areas.Length == 0) return "At least one area is required";

        for (int i = 0; i < dto.Areas.Length; i++)
        {
            var area = dto.Areas[i];
            if (string.IsNullOrWhiteSpace(area.ResRef))
                return $"Area [{i}]: ResRef is required";
            if (area.ResRef.Length > 16)
                return $"Area [{i}]: ResRef must not exceed 16 characters";
        }

        return null;
    }

    private static object ToDto(RegionDefinition def)
    {
        return new
        {
            Tag = def.Tag.Value,
            def.Name,
            DefaultChaos = def.DefaultChaos != null
                ? new { def.DefaultChaos.Danger, def.DefaultChaos.Corruption, def.DefaultChaos.Density, def.DefaultChaos.Mutation }
                : null,
            Areas = def.Areas.Select(a => new
            {
                ResRef = a.ResRef.Value,
                a.DefinitionTags,
                Environment = new
                {
                    Climate = a.Environment.Climate.ToString(),
                    SoilQuality = a.Environment.SoilQuality.ToString(),
                    MineralQualityRange = new
                    {
                        Min = a.Environment.MineralQualityRange.Min.ToString(),
                        Max = a.Environment.MineralQualityRange.Max.ToString()
                    },
                    Chaos = a.Environment.Chaos != null
                        ? new { a.Environment.Chaos.Danger, a.Environment.Chaos.Corruption, a.Environment.Chaos.Density, a.Environment.Chaos.Mutation }
                        : null
                },
                PlacesOfInterest = a.PlacesOfInterest?.Select(p => new
                {
                    p.ResRef,
                    p.Tag,
                    p.Name,
                    Type = p.Type.ToString(),
                    p.Description
                }).ToArray(),
                LinkedSettlement = a.LinkedSettlement?.Value
            }).ToArray()
        };
    }

    private static RegionDefinition FromDto(RegionDto dto)
    {
        ChaosState? defaultChaos = null;
        if (dto.DefaultChaos != null)
        {
            defaultChaos = new ChaosState
            {
                Danger = dto.DefaultChaos.Danger,
                Corruption = dto.DefaultChaos.Corruption,
                Density = dto.DefaultChaos.Density,
                Mutation = dto.DefaultChaos.Mutation
            };
        }

        var areas = (dto.Areas ?? Array.Empty<AreaDto>()).Select(a =>
        {
            Enum.TryParse<Climate>(a.Environment?.Climate, true, out var climate);
            Enum.TryParse<EconomyQuality>(a.Environment?.SoilQuality, true, out var soilQuality);

            Enum.TryParse<EconomyQuality>(a.Environment?.MineralQualityRange?.Min, true, out var minQuality);
            Enum.TryParse<EconomyQuality>(a.Environment?.MineralQualityRange?.Max, true, out var maxQuality);
            if (minQuality == default) minQuality = EconomyQuality.Average;
            if (maxQuality == default) maxQuality = EconomyQuality.Average;

            ChaosState? areaChaos = null;
            if (a.Environment?.Chaos != null)
            {
                areaChaos = new ChaosState
                {
                    Danger = a.Environment.Chaos.Danger,
                    Corruption = a.Environment.Chaos.Corruption,
                    Density = a.Environment.Chaos.Density,
                    Mutation = a.Environment.Chaos.Mutation
                };
            }

            var env = new EnvironmentData(climate, soilQuality,
                new QualityRange(minQuality, maxQuality), areaChaos);

            List<PlaceOfInterest>? pois = a.PlacesOfInterest?.Select(p =>
            {
                Enum.TryParse<PoiType>(p.Type, true, out var poiType);
                return new PlaceOfInterest(p.ResRef, p.Tag, p.Name, poiType, p.Description);
            }).ToList();

            SettlementId? settlement = a.LinkedSettlement is > 0
                ? SettlementId.Parse(a.LinkedSettlement.Value)
                : null;

            return new AreaDefinition(
                new AreaTag(a.ResRef),
                a.DefinitionTags?.ToList() ?? new List<string>(),
                env,
                pois,
                settlement);
        }).ToList();

        return new RegionDefinition
        {
            Tag = new RegionTag(dto.Tag),
            Name = dto.Name,
            Areas = areas,
            DefaultChaos = defaultChaos
        };
    }

    // ==================== DTO classes for request/response ====================

    private record RegionDto
    {
        public string Tag { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public ChaosStateDto? DefaultChaos { get; init; }
        public AreaDto[]? Areas { get; init; }
    }

    private record AreaDto
    {
        public string ResRef { get; init; } = string.Empty;
        public List<string>? DefinitionTags { get; init; }
        public EnvironmentDto? Environment { get; init; }
        public PlaceOfInterestDto[]? PlacesOfInterest { get; init; }
        public int? LinkedSettlement { get; init; }
    }

    private record EnvironmentDto
    {
        public string? Climate { get; init; }
        public string? SoilQuality { get; init; }
        public QualityRangeDto? MineralQualityRange { get; init; }
        public ChaosStateDto? Chaos { get; init; }
    }

    private record QualityRangeDto
    {
        public string? Min { get; init; }
        public string? Max { get; init; }
    }

    private record PlaceOfInterestDto
    {
        public string ResRef { get; init; } = string.Empty;
        public string Tag { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Type { get; init; }
        public string? Description { get; init; }
    }

    private record ChaosStateDto
    {
        public int Danger { get; init; }
        public int Corruption { get; init; }
        public int Density { get; init; }
        public int Mutation { get; init; }
    }
}

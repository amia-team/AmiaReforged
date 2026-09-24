using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Regions.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Regions.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Persistence;
using RegionType = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.RegionType;
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
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        string? search = ctx.GetQueryParam("search");
        int page = int.TryParse(ctx.GetQueryParam("page"), out int p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out int ps) ? Math.Clamp(ps, 1, 200) : 50;

        List<RegionDefinition> matches = await facade.QueryAsync<SearchRegionDefinitionsQuery, List<RegionDefinition>>(
            new SearchRegionDefinitionsQuery { SearchTerm = search }, ctx.CancellationToken);

        int totalCount = matches.Count;
        List<RegionDefinition> paged = matches
            .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

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
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        RegionDefinition? region = await facade.QueryAsync<GetRegionDefinitionQuery, RegionDefinition?>(
            new GetRegionDefinitionQuery { Tag = tag }, ctx.CancellationToken);
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
        RegionDto? dto = await ctx.ReadJsonBodyAsync<RegionDto>();
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

        RegionDefinition definition = FromDto(dto);
        CommandResult result = await facade.ExecuteAsync(new UpsertRegionCommand
        {
            Definition = definition
        }, ctx.CancellationToken);

        if (!result.Success)
            return new ApiResult(400, new ErrorResponse("Command failed", result.ErrorMessage));

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

        RegionDto? dto = await ctx.ReadJsonBodyAsync<RegionDto>();
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

        RegionDefinition definition = FromDto(dto);
        CommandResult result = await facade.ExecuteAsync(new UpdateRegionCommand
        {
            Tag = tag,
            Definition = definition
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool notFound = result.ErrorMessage?.StartsWith("No region with tag", StringComparison.OrdinalIgnoreCase) == true;
            return new ApiResult(notFound ? 404 : 400, new ErrorResponse(
                notFound ? "Not found" : "Command failed", result.ErrorMessage));
        }

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
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        CommandResult result = await facade.ExecuteAsync(new DeleteRegionCommand
        {
            Tag = tag
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                "Not found", result.ErrorMessage)));
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
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        string? search = ctx.GetQueryParam("search");

        List<RegionDefinition> regions = await facade.QueryAsync<SearchRegionDefinitionsQuery, List<RegionDefinition>>(
            new SearchRegionDefinitionsQuery { SearchTerm = search }, ctx.CancellationToken);

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
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        string? body = null;
        if (ctx.Request != null)
        {
            using StreamReader reader = new StreamReader(ctx.Request.InputStream);
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
                RegionDto? single = JsonSerializer.Deserialize<RegionDto>(body, ImportOptions);
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

        int failed = 0;
        List<string> errors = new();
        List<(string Tag, UpsertRegionCommand Command)> valid = new();

        foreach (RegionDto dto in dtos)
        {
            string? validationError = ValidateDto(dto);
            if (validationError != null)
            {
                failed++;
                errors.Add($"{dto.Tag ?? "unknown"}: {validationError}");
                continue;
            }

            try
            {
                valid.Add((dto.Tag, new UpsertRegionCommand { Definition = FromDto(dto) }));
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{dto.Tag ?? "unknown"}: {ex.Message}");
            }
        }

        BatchCommandResult batch = await facade.ExecuteBatchAsync(
            valid.Select(v => v.Command),
            BatchExecutionOptions.ContinueOnFailure(),
            ctx.CancellationToken);

        int succeeded = 0;
        for (int i = 0; i < batch.Results.Count; i++)
        {
            if (batch.Results[i].Success)
            {
                succeeded++;
            }
            else
            {
                failed++;
                errors.Add($"{valid[i].Tag}: {batch.Results[i].ErrorMessage}");
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

    private static string? ValidateDto(RegionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Tag)) return "Tag is required";
        if (string.IsNullOrWhiteSpace(dto.Name)) return "Name is required";

        if (dto.Areas != null)
        {
            for (int i = 0; i < dto.Areas.Length; i++)
            {
                AreaDto area = dto.Areas[i];
                if (string.IsNullOrWhiteSpace(area.ResRef))
                    return $"Area [{i}]: ResRef is required";
                if (area.ResRef.Length > 16)
                    return $"Area [{i}]: ResRef must not exceed 16 characters";
            }
        }

        return null;
    }

    private static object ToDto(RegionDefinition def)
    {
        return new
        {
            Tag = def.Tag.Value,
            def.Name,
            Description = def.Description,
            Type = def.Type?.ToString(),
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

        List<AreaDefinition> areas = (dto.Areas ?? Array.Empty<AreaDto>()).Select(a =>
        {
            Enum.TryParse<Climate>(a.Environment?.Climate, true, out Climate climate);
            Enum.TryParse<EconomyQuality>(a.Environment?.SoilQuality, true, out EconomyQuality soilQuality);

            Enum.TryParse<EconomyQuality>(a.Environment?.MineralQualityRange?.Min, true, out EconomyQuality minQuality);
            Enum.TryParse<EconomyQuality>(a.Environment?.MineralQualityRange?.Max, true, out EconomyQuality maxQuality);
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

            EnvironmentData env = new EnvironmentData(climate, soilQuality,
                new QualityRange(minQuality, maxQuality), areaChaos);

            List<PlaceOfInterest>? pois = a.PlacesOfInterest?.Select(p =>
            {
                Enum.TryParse<PoiType>(p.Type, true, out PoiType poiType);
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

        RegionType? type = Enum.TryParse<RegionType>(dto.Type, true, out RegionType parsedType) ? parsedType : (RegionType?)null;

        return new RegionDefinition
        {
            Tag = new RegionTag(dto.Tag),
            Name = dto.Name,
            Description = dto.Description,
            Type = type,
            Areas = areas,
            DefaultChaos = defaultChaos
        };
    }

    // ==================== DTO classes for request/response ====================

    private record RegionDto
    {
        public string Tag { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? Type { get; init; }
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

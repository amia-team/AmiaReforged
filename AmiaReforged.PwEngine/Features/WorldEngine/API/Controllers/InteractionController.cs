using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Persistence;
using Anvil;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for managing interaction definitions.
/// Supports CRUD operations and bulk JSON import for the admin panel.
/// </summary>
public class InteractionController
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private const string BasePath = "/api/worldengine/interactions";

    // ═══════════════════════════════════════════════════════════════════
    //  CRUD Endpoints
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// List all interaction definitions with optional search and pagination.
    /// GET /api/worldengine/interactions?search=&amp;page=1&amp;pageSize=50
    /// </summary>
    [HttpGet(BasePath)]
    public static async Task<ApiResult> GetAll(RouteContext ctx)
    {
        string? search = ctx.GetQueryParam("search");
        int page = int.TryParse(ctx.GetQueryParam("page"), out var p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out var ps) ? Math.Clamp(ps, 1, 200) : 50;

        using var context = ResolveContext();

        IQueryable<PersistedInteractionDefinition> query = context.InteractionDefinitions;

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(d =>
                d.Tag.ToLower().Contains(term) ||
                d.Name.ToLower().Contains(term));
        }

        int totalCount = await query.CountAsync();

        List<PersistedInteractionDefinition> items = await query
            .OrderBy(d => d.Name)
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
    /// Get a single interaction definition by tag.
    /// GET /api/worldengine/interactions/{tag}
    /// </summary>
    [HttpGet(BasePath + "/{tag}")]
    public static async Task<ApiResult> GetByTag(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");

        using var context = ResolveContext();
        var definition = await context.InteractionDefinitions.FindAsync(tag);

        if (definition == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No interaction definition with tag '{tag}'"));
        }

        return new ApiResult(200, ToDto(definition));
    }

    /// <summary>
    /// Create a new interaction definition.
    /// POST /api/worldengine/interactions
    /// </summary>
    [HttpPost(BasePath)]
    public static async Task<ApiResult> Create(RouteContext ctx)
    {
        var dto = await ctx.ReadJsonBodyAsync<InteractionDefinitionDto>();
        if (dto == null)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required"));
        }

        string? validationError = ValidateDto(dto);
        if (validationError != null)
        {
            return new ApiResult(400, new ErrorResponse("Validation failed", validationError));
        }

        using var context = ResolveContext();

        bool exists = await context.InteractionDefinitions.AnyAsync(d => d.Tag == dto.Tag);
        if (exists)
        {
            return new ApiResult(409, new ErrorResponse(
                "Conflict", $"An interaction definition with tag '{dto.Tag}' already exists"));
        }

        var entity = FromDto(dto);
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        context.InteractionDefinitions.Add(entity);
        await context.SaveChangesAsync();

        return new ApiResult(201, ToDto(entity));
    }

    /// <summary>
    /// Update an existing interaction definition.
    /// PUT /api/worldengine/interactions/{tag}
    /// </summary>
    [HttpPut(BasePath + "/{tag}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");

        using var context = ResolveContext();
        var existing = await context.InteractionDefinitions.FindAsync(tag);

        if (existing == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No interaction definition with tag '{tag}'"));
        }

        var dto = await ctx.ReadJsonBodyAsync<InteractionDefinitionDto>();
        if (dto == null)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required"));
        }

        string? validationError = ValidateDto(dto);
        if (validationError != null)
        {
            return new ApiResult(400, new ErrorResponse("Validation failed", validationError));
        }

        // Update mutable fields — Tag is immutable
        existing.Name = dto.Name?.Trim() ?? existing.Name;
        existing.Description = dto.Description;
        existing.TargetMode = dto.TargetMode ?? "Trigger";
        existing.BaseRounds = dto.BaseRounds;
        existing.MinRounds = dto.MinRounds;
        existing.ProficiencyReducesRounds = dto.ProficiencyReducesRounds;
        existing.RequiresIndustryMembership = dto.RequiresIndustryMembership;
        existing.ResponsesJson = SerializeResponses(dto.Responses);
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return new ApiResult(200, ToDto(existing));
    }

    /// <summary>
    /// Delete an interaction definition.
    /// DELETE /api/worldengine/interactions/{tag}
    /// </summary>
    [HttpDelete(BasePath + "/{tag}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");

        using var context = ResolveContext();
        var existing = await context.InteractionDefinitions.FindAsync(tag);

        if (existing == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No interaction definition with tag '{tag}'"));
        }

        context.InteractionDefinitions.Remove(existing);
        await context.SaveChangesAsync();

        return new ApiResult(204, new { message = "Deleted" });
    }

    /// <summary>
    /// Bulk import interaction definitions from JSON.
    /// POST /api/worldengine/interactions/import
    /// Body: JSON array (or single object) of interaction definition DTOs.
    /// Existing definitions with matching tags are updated (upsert).
    /// </summary>
    [HttpPost(BasePath + "/import")]
    public static async Task<ApiResult> Import(RouteContext ctx)
    {
        string? body = null;
        if (ctx.Request != null)
        {
            using var reader = new StreamReader(ctx.Request.InputStream);
            body = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return new ApiResult(400, new ErrorResponse("Bad request",
                "Request body must be a JSON array of interaction definitions"));
        }

        List<InteractionDefinitionDto>? dtos;
        try
        {
            dtos = JsonSerializer.Deserialize<List<InteractionDefinitionDto>>(body, JsonOptions);
        }
        catch (JsonException)
        {
            try
            {
                var single = JsonSerializer.Deserialize<InteractionDefinitionDto>(body, JsonOptions);
                dtos = single != null ? [single] : null;
            }
            catch (JsonException ex)
            {
                return new ApiResult(400, new ErrorResponse("Parse error", ex.Message));
            }
        }

        if (dtos == null || dtos.Count == 0)
        {
            return new ApiResult(400, new ErrorResponse("Bad request",
                "No valid interaction definitions found in request body"));
        }

        using var context = ResolveContext();
        int succeeded = 0;
        int failed = 0;
        List<string> errors = [];

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

                var existing = await context.InteractionDefinitions.FindAsync(dto.Tag);
                if (existing != null)
                {
                    existing.Name = dto.Name?.Trim() ?? existing.Name;
                    existing.Description = dto.Description;
                    existing.TargetMode = dto.TargetMode ?? "Trigger";
                    existing.BaseRounds = dto.BaseRounds;
                    existing.MinRounds = dto.MinRounds;
                    existing.ProficiencyReducesRounds = dto.ProficiencyReducesRounds;
                    existing.RequiresIndustryMembership = dto.RequiresIndustryMembership;
                    existing.ResponsesJson = SerializeResponses(dto.Responses);
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var entity = FromDto(dto);
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.UpdatedAt = DateTime.UtcNow;
                    context.InteractionDefinitions.Add(entity);
                }

                succeeded++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{dto.Tag ?? "unknown"}: {ex.Message}");
            }
        }

        await context.SaveChangesAsync();

        return new ApiResult(200, new
        {
            succeeded,
            failed,
            total = dtos.Count,
            errors
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

    private static PwEngineContext ResolveContext()
    {
        var factory = AnvilCore.GetService<PwContextFactory>()
                      ?? throw new InvalidOperationException("PwContextFactory service not available");
        return factory.CreateDbContext();
    }

    private static string? ValidateDto(InteractionDefinitionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Tag)) return "Tag is required";
        if (dto.Tag.Length > 100) return "Tag must not exceed 100 characters";
        if (string.IsNullOrWhiteSpace(dto.Name)) return "Name is required";
        if (dto.Name.Length > 200) return "Name must not exceed 200 characters";
        if (dto.BaseRounds < 1) return "BaseRounds must be at least 1";
        if (dto.MinRounds < 1) return "MinRounds must be at least 1";
        if (dto.MinRounds > dto.BaseRounds) return "MinRounds cannot exceed BaseRounds";

        string[] validTargetModes = ["Node", "Trigger", "Placeable"];
        if (!string.IsNullOrEmpty(dto.TargetMode) && !validTargetModes.Contains(dto.TargetMode, StringComparer.OrdinalIgnoreCase))
        {
            return $"TargetMode must be one of: {string.Join(", ", validTargetModes)}";
        }

        if (dto.Responses is { Count: > 0 })
        {
            for (int i = 0; i < dto.Responses.Count; i++)
            {
                var r = dto.Responses[i];
                if (string.IsNullOrWhiteSpace(r.ResponseTag))
                    return $"Response[{i}].ResponseTag is required";
                if (r.Weight < 1)
                    return $"Response[{i}].Weight must be at least 1";
            }
        }

        return null;
    }

    private static object ToDto(PersistedInteractionDefinition entity)
    {
        List<ResponseJsonDto> responses;
        try
        {
            responses = JsonSerializer.Deserialize<List<ResponseJsonDto>>(entity.ResponsesJson, JsonOptions) ?? [];
        }
        catch
        {
            responses = [];
        }

        return new
        {
            entity.Tag,
            entity.Name,
            entity.Description,
            entity.TargetMode,
            entity.BaseRounds,
            entity.MinRounds,
            entity.ProficiencyReducesRounds,
            entity.RequiresIndustryMembership,
            Responses = responses.Select(r => new
            {
                r.ResponseTag,
                r.Weight,
                r.MinProficiency,
                r.Message,
                Effects = (r.Effects ?? []).Select(e => new
                {
                    e.EffectType,
                    e.Value,
                    e.Metadata
                }).ToArray()
            }).ToArray(),
            entity.CreatedAt,
            entity.UpdatedAt
        };
    }

    private static PersistedInteractionDefinition FromDto(InteractionDefinitionDto dto)
    {
        return new PersistedInteractionDefinition
        {
            Tag = dto.Tag!.Trim(),
            Name = dto.Name!.Trim(),
            Description = dto.Description,
            TargetMode = dto.TargetMode ?? "Trigger",
            BaseRounds = dto.BaseRounds,
            MinRounds = dto.MinRounds,
            ProficiencyReducesRounds = dto.ProficiencyReducesRounds,
            RequiresIndustryMembership = dto.RequiresIndustryMembership,
            ResponsesJson = SerializeResponses(dto.Responses)
        };
    }

    private static string SerializeResponses(List<ResponseDto>? responses)
    {
        if (responses == null || responses.Count == 0) return "[]";

        var jsonDtos = responses.Select(r => new ResponseJsonDto
        {
            ResponseTag = r.ResponseTag,
            Weight = r.Weight,
            MinProficiency = r.MinProficiency,
            Message = r.Message,
            Effects = r.Effects?.Select(e => new EffectJsonDto
            {
                EffectType = e.EffectType,
                Value = e.Value,
                Metadata = e.Metadata
            }).ToList() ?? []
        }).ToList();

        return JsonSerializer.Serialize(jsonDtos, JsonOptions);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DTOs
    // ═══════════════════════════════════════════════════════════════════

    private record InteractionDefinitionDto
    {
        public string? Tag { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public string? TargetMode { get; init; }
        public int BaseRounds { get; init; } = 4;
        public int MinRounds { get; init; } = 2;
        public bool ProficiencyReducesRounds { get; init; } = true;
        public bool RequiresIndustryMembership { get; init; } = true;
        public List<ResponseDto>? Responses { get; init; }
    }

    private record ResponseDto
    {
        public string? ResponseTag { get; init; }
        public int Weight { get; init; } = 1;
        public string? MinProficiency { get; init; }
        public string? Message { get; init; }
        public List<EffectDto>? Effects { get; init; }
    }

    private record EffectDto
    {
        public string? EffectType { get; init; }
        public string? Value { get; init; }
        public Dictionary<string, object>? Metadata { get; init; }
    }

    /// <summary>Internal shape matching the JSONB storage format.</summary>
    private class ResponseJsonDto
    {
        public string? ResponseTag { get; set; }
        public int Weight { get; set; } = 1;
        public string? MinProficiency { get; set; }
        public string? Message { get; set; }
        public List<EffectJsonDto>? Effects { get; set; }
    }

    private class EffectJsonDto
    {
        public string? EffectType { get; set; }
        public string? Value { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}

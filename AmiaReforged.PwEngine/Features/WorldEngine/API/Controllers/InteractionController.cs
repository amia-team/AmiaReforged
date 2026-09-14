using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.Glyph.Integration;
using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Persistence;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Queries;
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

    /// <summary>
    /// Set by <see cref="Glyph.API.GlyphApiBootstrap"/> to allow cache invalidation
    /// after interaction definition mutations.
    /// </summary>
    internal static GlyphInteractionHookService? InteractionHooks;

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
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        string? search = ctx.GetQueryParam("search");
        int page = int.TryParse(ctx.GetQueryParam("page"), out int p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out int ps) ? Math.Clamp(ps, 1, 200) : 50;

        List<InteractionDefinition> matches = await facade.QueryAsync<SearchInteractionDefinitionsQuery, List<InteractionDefinition>>(
            new SearchInteractionDefinitionsQuery { SearchTerm = search }, ctx.CancellationToken);

        int totalCount = matches.Count;
        List<InteractionDefinition> items = matches
            .OrderBy(d => d.Name)
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
    /// Get a single interaction definition by tag.
    /// GET /api/worldengine/interactions/{tag}
    /// </summary>
    [HttpGet(BasePath + "/{tag}")]
    public static async Task<ApiResult> GetByTag(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        InteractionDefinition? definition = await facade.QueryAsync<GetInteractionDefinitionQuery, InteractionDefinition?>(
            new GetInteractionDefinitionQuery { Tag = tag }, ctx.CancellationToken);

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
        InteractionDefinitionDto? dto = await ctx.ReadJsonBodyAsync<InteractionDefinitionDto>();
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

        InteractionDefinition definition = FromDto(dto);
        CommandResult result = await facade.ExecuteAsync(new CreateInteractionDefinitionCommand
        {
            Definition = definition
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool conflict = result.ErrorMessage?.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true;
            return new ApiResult(conflict ? 409 : 400,
                new ErrorResponse(conflict ? "Conflict" : "Command failed", result.ErrorMessage));
        }

        // Refresh glyph interaction cache so scripts see the new definition immediately
        if (InteractionHooks != null) await InteractionHooks.RefreshCacheAsync();

        InteractionDefinition? created = await facade.QueryAsync<GetInteractionDefinitionQuery, InteractionDefinition?>(
            new GetInteractionDefinitionQuery { Tag = definition.Tag }, ctx.CancellationToken);

        return new ApiResult(201, ToDto(created ?? definition));
    }

    /// <summary>
    /// Update an existing interaction definition.
    /// PUT /api/worldengine/interactions/{tag}
    /// </summary>
    [HttpPut(BasePath + "/{tag}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");

        InteractionDefinitionDto? dto = await ctx.ReadJsonBodyAsync<InteractionDefinitionDto>();
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

        InteractionDefinition definition = FromDto(dto, tag);
        CommandResult result = await facade.ExecuteAsync(new UpdateInteractionDefinitionCommand
        {
            Tag = tag,
            Definition = definition
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool notFound = result.ErrorMessage?.StartsWith("No interaction definition with tag", StringComparison.OrdinalIgnoreCase) == true;
            return new ApiResult(notFound ? 404 : 400, new ErrorResponse(
                notFound ? "Not found" : "Command failed", result.ErrorMessage));
        }

        // Refresh glyph interaction cache so scripts pick up the updated definition immediately
        if (InteractionHooks != null) await InteractionHooks.RefreshCacheAsync();

        InteractionDefinition? updated = await facade.QueryAsync<GetInteractionDefinitionQuery, InteractionDefinition?>(
            new GetInteractionDefinitionQuery { Tag = tag }, ctx.CancellationToken);

        return new ApiResult(200, ToDto(updated ?? definition));
    }

    /// <summary>
    /// Delete an interaction definition.
    /// DELETE /api/worldengine/interactions/{tag}
    /// </summary>
    [HttpDelete(BasePath + "/{tag}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        CommandResult result = await facade.ExecuteAsync(new DeleteInteractionDefinitionCommand
        {
            Tag = tag
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", result.ErrorMessage));
        }

        // Refresh glyph interaction cache to remove any references to the deleted definition
        if (InteractionHooks != null) await InteractionHooks.RefreshCacheAsync();

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
                InteractionDefinitionDto? single = JsonSerializer.Deserialize<InteractionDefinitionDto>(body, JsonOptions);
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

        int failed = 0;
        List<string> errors = [];
        List<(string Tag, UpsertInteractionDefinitionCommand Command)> valid = [];

        foreach (InteractionDefinitionDto dto in dtos)
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
                valid.Add((dto.Tag!, new UpsertInteractionDefinitionCommand { Definition = FromDto(dto) }));
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

        // Refresh glyph interaction cache for any imported/updated definitions
        if (InteractionHooks != null) await InteractionHooks.RefreshCacheAsync();

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
                ResponseDto r = dto.Responses[i];
                if (string.IsNullOrWhiteSpace(r.ResponseTag))
                    return $"Response[{i}].ResponseTag is required";
                if (r.Weight < 1)
                    return $"Response[{i}].Weight must be at least 1";
                if (!string.IsNullOrWhiteSpace(r.MinProficiency) &&
                    !Enum.TryParse<ProficiencyLevel>(r.MinProficiency, true, out _))
                    return $"Response[{i}].MinProficiency is not a valid proficiency level";
                if (r.Effects != null)
                {
                    for (int j = 0; j < r.Effects.Count; j++)
                    {
                        EffectDto e = r.Effects[j];
                        if (string.IsNullOrWhiteSpace(e.EffectType) ||
                            !Enum.TryParse<InteractionResponseEffectType>(e.EffectType, true, out _))
                            return $"Response[{i}].Effects[{j}].EffectType is not a valid effect type";
                        if (string.IsNullOrWhiteSpace(e.Value))
                            return $"Response[{i}].Effects[{j}].Value is required";
                    }
                }
            }
        }

        return null;
    }

    private static object ToDto(InteractionDefinition definition)
    {
        return new
        {
            definition.Tag,
            definition.Name,
            definition.Description,
            TargetMode = definition.TargetMode.ToString(),
            definition.BaseRounds,
            definition.MinRounds,
            definition.ProficiencyReducesRounds,
            definition.RequiresIndustryMembership,
            RequiredIndustryTags = definition.RequiredIndustryTags,
            AllowedAreaResRefs = definition.AllowedAreaResRefs,
            RequiredKnowledgeTags = definition.RequiredKnowledgeTags,
            Responses = definition.Responses.Select(r => new
            {
                r.ResponseTag,
                r.Weight,
                MinProficiency = r.MinProficiency?.ToString(),
                r.Message,
                Effects = r.Effects.Select(e => new
                {
                    EffectType = e.EffectType.ToString(),
                    e.Value,
                    e.Metadata
                }).ToArray()
            }).ToArray()
        };
    }

    private static InteractionDefinition FromDto(InteractionDefinitionDto dto, string? tagOverride = null)
    {
        Enum.TryParse<InteractionTargetMode>(dto.TargetMode, true, out InteractionTargetMode targetMode);
        if (!Enum.IsDefined(typeof(InteractionTargetMode), targetMode))
            targetMode = InteractionTargetMode.Trigger;

        return new InteractionDefinition
        {
            Tag = (tagOverride ?? dto.Tag)!.Trim(),
            Name = dto.Name!.Trim(),
            Description = dto.Description,
            TargetMode = targetMode,
            BaseRounds = dto.BaseRounds,
            MinRounds = dto.MinRounds,
            ProficiencyReducesRounds = dto.ProficiencyReducesRounds,
            RequiresIndustryMembership = dto.RequiresIndustryMembership,
            RequiredIndustryTags = dto.RequiredIndustryTags ?? [],
            AllowedAreaResRefs = dto.AllowedAreaResRefs ?? [],
            RequiredKnowledgeTags = dto.RequiredKnowledgeTags ?? [],
            Responses = (dto.Responses ?? []).Select(r =>
            {
                Enum.TryParse<ProficiencyLevel>(r.MinProficiency, true, out ProficiencyLevel minProf);
                return new InteractionResponse
                {
                    ResponseTag = r.ResponseTag!,
                    Weight = r.Weight,
                    MinProficiency = string.IsNullOrWhiteSpace(r.MinProficiency) ? null : minProf,
                    Message = r.Message,
                    Effects = (r.Effects ?? []).Select(e => new InteractionResponseEffect
                    {
                        EffectType = Enum.TryParse<InteractionResponseEffectType>(e.EffectType, true, out InteractionResponseEffectType et)
                            ? et : InteractionResponseEffectType.Custom,
                        Value = e.Value ?? string.Empty,
                        Metadata = e.Metadata ?? new Dictionary<string, object>()
                    }).ToList()
                };
            }).ToList()
        };
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
        public List<string>? RequiredIndustryTags { get; init; }
        public List<string>? AllowedAreaResRefs { get; init; }
        public List<string>? RequiredKnowledgeTags { get; init; }
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

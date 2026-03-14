using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Persistence;
using Anvil;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for managing industry definitions.
/// Supports CRUD operations and bulk JSON import/export for the admin panel.
/// </summary>
public class IndustryController
{
    private static readonly JsonSerializerOptions ImportOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// List all industries with optional search and pagination.
    /// GET /api/worldengine/industries?search=&amp;page=1&amp;pageSize=50
    /// </summary>
    [HttpGet("/api/worldengine/industries")]
    public static async Task<ApiResult> GetAll(RouteContext ctx)
    {
        IIndustryRepository repo = ResolveRepository();
        string? search = ctx.GetQueryParam("search");
        int page = int.TryParse(ctx.GetQueryParam("page"), out int p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out int ps) ? Math.Clamp(ps, 1, 200) : 50;

        List<Industry> paged = repo.Search(search, page, pageSize, out int totalCount);

        return await Task.FromResult(new ApiResult(200, new
        {
            items = paged.Select(ToDto),
            totalCount,
            page,
            pageSize
        }));
    }

    /// <summary>
    /// Get a single industry by tag.
    /// GET /api/worldengine/industries/{tag}
    /// </summary>
    [HttpGet("/api/worldengine/industries/{tag}")]
    public static async Task<ApiResult> GetByTag(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");
        IIndustryRepository repo = ResolveRepository();

        Industry? industry = repo.Get(tag);
        if (industry == null)
        {
            return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                "Not found", $"No industry with tag '{tag}'")));
        }

        return await Task.FromResult(new ApiResult(200, ToDto(industry)));
    }

    /// <summary>
    /// Create a new industry definition.
    /// POST /api/worldengine/industries
    /// </summary>
    [HttpPost("/api/worldengine/industries")]
    public static async Task<ApiResult> Create(RouteContext ctx)
    {
        IndustryDto? dto = await ctx.ReadJsonBodyAsync<IndustryDto>();
        if (dto == null)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required"));
        }

        string? validationError = ValidateDto(dto);
        if (validationError != null)
        {
            return new ApiResult(400, new ErrorResponse("Validation failed", validationError));
        }

        IIndustryRepository repo = ResolveRepository();

        if (repo.IndustryExists(dto.Tag))
        {
            return new ApiResult(409, new ErrorResponse("Conflict",
                $"Industry with tag '{dto.Tag}' already exists"));
        }

        Industry industry = FromDto(dto);
        repo.Add(industry);

        return new ApiResult(201, ToDto(industry));
    }

    /// <summary>
    /// Update an existing industry definition by tag.
    /// PUT /api/worldengine/industries/{tag}
    /// </summary>
    [HttpPut("/api/worldengine/industries/{tag}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");
        IIndustryRepository repo = ResolveRepository();

        Industry? existing = repo.Get(tag);
        if (existing == null)
        {
            return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                "Not found", $"No industry with tag '{tag}'")));
        }

        IndustryDto? dto = await ctx.ReadJsonBodyAsync<IndustryDto>();
        if (dto == null)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required"));
        }

        string? validationError = ValidateDto(dto);
        if (validationError != null)
        {
            return new ApiResult(400, new ErrorResponse("Validation failed", validationError));
        }

        // Ensure the tag in the body matches the route
        dto = dto with { Tag = tag };
        Industry industry = FromDto(dto);
        repo.Update(industry);

        return new ApiResult(200, ToDto(industry));
    }

    /// <summary>
    /// Delete an industry definition by tag.
    /// DELETE /api/worldengine/industries/{tag}
    /// </summary>
    [HttpDelete("/api/worldengine/industries/{tag}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");
        IIndustryRepository repo = ResolveRepository();

        bool deleted = repo.Delete(tag);
        if (!deleted)
        {
            return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                "Not found", $"No industry with tag '{tag}'")));
        }

        return await Task.FromResult(new ApiResult(204, new { message = "Deleted" }));
    }

    /// <summary>
    /// Export all industry definitions (optionally filtered) as a JSON array.
    /// GET /api/worldengine/industries/export?search=
    /// </summary>
    [HttpGet("/api/worldengine/industries/export")]
    public static async Task<ApiResult> Export(RouteContext ctx)
    {
        IIndustryRepository repo = ResolveRepository();
        string? search = ctx.GetQueryParam("search");

        List<Industry> industries = repo.All();
        if (!string.IsNullOrWhiteSpace(search))
        {
            industries = industries.Where(i =>
                i.Tag.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return await Task.FromResult(new ApiResult(200,
            industries.OrderBy(i => i.Name).Select(ToDto).ToArray()));
    }

    /// <summary>
    /// Bulk import industry definitions from JSON.
    /// POST /api/worldengine/industries/import
    /// Body: JSON array of industry definitions.
    /// </summary>
    [HttpPost("/api/worldengine/industries/import")]
    public static async Task<ApiResult> Import(RouteContext ctx)
    {
        IIndustryRepository repo = ResolveRepository();

        string? body = null;
        if (ctx.Request != null)
        {
            using StreamReader reader = new StreamReader(ctx.Request.InputStream);
            body = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return new ApiResult(400, new ErrorResponse("Bad request",
                "Request body must be a JSON array of industry definitions"));
        }

        List<IndustryDto>? dtos;
        try
        {
            dtos = JsonSerializer.Deserialize<List<IndustryDto>>(body, ImportOptions);
        }
        catch (JsonException)
        {
            try
            {
                IndustryDto? single = JsonSerializer.Deserialize<IndustryDto>(body, ImportOptions);
                dtos = single != null ? new List<IndustryDto> { single } : null;
            }
            catch (JsonException ex)
            {
                return new ApiResult(400, new ErrorResponse("Parse error", ex.Message));
            }
        }

        if (dtos == null || dtos.Count == 0)
        {
            return new ApiResult(400, new ErrorResponse("Bad request",
                "No valid industry definitions found in request body"));
        }

        int succeeded = 0;
        int failed = 0;
        List<string> errors = new();

        foreach (IndustryDto dto in dtos)
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

                Industry industry = FromDto(dto);
                repo.Add(industry); // Acts as upsert
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

    private static IIndustryRepository ResolveRepository()
    {
        return AnvilCore.GetService<IIndustryRepository>()
               ?? throw new InvalidOperationException("IIndustryRepository service not available");
    }

    private static string? ValidateDto(IndustryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Tag)) return "Tag is required";
        if (string.IsNullOrWhiteSpace(dto.Name)) return "Name is required";
        return null;
    }

    private static object ToDto(Industry industry)
    {
        return new
        {
            industry.Tag,
            industry.Name,
            Knowledge = industry.Knowledge.Select(k => new
            {
                k.Tag,
                k.Name,
                k.Description,
                Level = k.Level.ToString(),
                k.PointCost,
                HarvestEffects = k.HarvestEffects.Select(e => new
                {
                    e.NodeTag,
                    StepModified = e.StepModified.ToString(),
                    e.Value,
                    Operation = e.Operation.ToString()
                }).ToArray(),
                k.Prerequisites,
                k.Branch,
                Effects = k.Effects.Select(e => new
                {
                    EffectType = e.EffectType.ToString(),
                    e.TargetTag,
                    e.Metadata
                }).ToArray(),
                CraftingModifiers = k.CraftingModifiers.Select(m => new
                {
                    m.TargetTag,
                    Scope = m.Scope.ToString(),
                    StepModified = m.StepModified.ToString(),
                    m.Value,
                    Operation = m.Operation.ToString()
                }).ToArray()
            }).ToArray(),
            Recipes = industry.Recipes.Select(r => new
            {
                RecipeId = r.RecipeId.Value,
                r.Name,
                r.Description,
                IndustryTag = r.IndustryTag.Value,
                r.RequiredKnowledge,
                Ingredients = r.Ingredients.Select(i => new
                {
                    i.ItemTag,
                    Quantity = i.Quantity.Value,
                    i.MinQuality,
                    i.IsConsumed
                }).ToArray(),
                Products = r.Products.Select(p => new
                {
                    p.ItemTag,
                    Quantity = p.Quantity.Value,
                    p.SuccessChance
                }).ToArray(),
                r.CraftingTimeRounds,
                r.KnowledgePointsAwarded,
                RequiredWorkstation = r.RequiredWorkstation?.Value,
                r.RequiredTools
            }).ToArray()
        };
    }

    private static Industry FromDto(IndustryDto dto)
    {
        return new Industry
        {
            Tag = dto.Tag,
            Name = dto.Name,
            Knowledge = dto.Knowledge?.Select(k =>
            {
                Enum.TryParse<ProficiencyLevel>(k.Level, true, out ProficiencyLevel level);
                return new Subsystems.Industries.KnowledgeSubsystem.Knowledge
                {
                    Tag = k.Tag ?? string.Empty,
                    Name = k.Name ?? string.Empty,
                    Description = k.Description ?? string.Empty,
                    Level = level,
                    PointCost = k.PointCost,
                    HarvestEffects = k.HarvestEffects?.Select(e =>
                    {
                        Enum.TryParse<Subsystems.Harvesting.HarvestStep>(e.StepModified, true, out HarvestStep step);
                        Enum.TryParse<Subsystems.Industries.KnowledgeSubsystem.EffectOperation>(e.Operation, true,
                            out EffectOperation op);
                        return new Subsystems.Industries.KnowledgeSubsystem.KnowledgeHarvestEffect(
                            e.NodeTag ?? string.Empty, step, e.Value, op);
                    }).ToList() ?? [],
                    Prerequisites = k.Prerequisites ?? [],
                    Branch = k.Branch,
                    Effects = k.Effects?.Select(e =>
                    {
                        Enum.TryParse<Subsystems.Industries.KnowledgeSubsystem.KnowledgeEffectType>(
                            e.EffectType, true, out KnowledgeEffectType effectType);
                        return new Subsystems.Industries.KnowledgeSubsystem.KnowledgeEffect
                        {
                            EffectType = effectType,
                            TargetTag = e.TargetTag ?? string.Empty,
                            Metadata = e.Metadata ?? new Dictionary<string, object>()
                        };
                    }).ToList() ?? [],
                    CraftingModifiers = k.CraftingModifiers?.Select(m =>
                    {
                        Enum.TryParse<Subsystems.Industries.KnowledgeSubsystem.CraftingModifierScope>(
                            m.Scope, true, out CraftingModifierScope scope);
                        Enum.TryParse<Subsystems.Industries.KnowledgeSubsystem.CraftingStep>(
                            m.StepModified, true, out CraftingStep step);
                        Enum.TryParse<Subsystems.Industries.KnowledgeSubsystem.EffectOperation>(
                            m.Operation, true, out EffectOperation op);
                        return new Subsystems.Industries.KnowledgeSubsystem.CraftingModifier(
                            m.TargetTag ?? string.Empty, scope, step, m.Value, op);
                    }).ToList() ?? []
                };
            }).ToList() ?? [],
            Recipes = dto.Recipes?.Select(r =>
            {
                return new Recipe
                {
                    RecipeId = new RecipeId(r.RecipeId ?? string.Empty),
                    Name = r.Name ?? string.Empty,
                    Description = r.Description,
                    IndustryTag = new SharedKernel.IndustryTag(r.IndustryTag ?? dto.Tag),
                    RequiredKnowledge = r.RequiredKnowledge ?? [],
                    Ingredients = r.Ingredients?.Select(i => new Ingredient
                    {
                        ItemTag = i.ItemTag ?? string.Empty,
                        Quantity = SharedKernel.ValueObjects.Quantity.Parse(i.Quantity),
                        MinQuality = i.MinQuality,
                        IsConsumed = i.IsConsumed
                    }).ToList() ?? [],
                    Products = r.Products?.Select(p => new Product
                    {
                        ItemTag = p.ItemTag ?? string.Empty,
                        Quantity = SharedKernel.ValueObjects.Quantity.Parse(p.Quantity),
                        SuccessChance = p.SuccessChance
                    }).ToList() ?? [],
                    CraftingTimeRounds = r.CraftingTimeRounds,
                    KnowledgePointsAwarded = r.KnowledgePointsAwarded,
                    Metadata = new Dictionary<string, object>(),
                    RequiredWorkstation = !string.IsNullOrEmpty(r.RequiredWorkstation)
                        ? new SharedKernel.WorkstationTag(r.RequiredWorkstation)
                        : null,
                    RequiredTools = r.RequiredTools ?? []
                };
            }).ToList() ?? []
        };
    }

    // ==================== DTO classes for request/response ====================

    private record IndustryDto
    {
        public string Tag { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public KnowledgeDto[]? Knowledge { get; init; }
        public RecipeDto[]? Recipes { get; init; }
    }

    private record KnowledgeDto
    {
        public string? Tag { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public string? Level { get; init; }
        public int PointCost { get; init; }
        public HarvestEffectDto[]? HarvestEffects { get; init; }
        public List<string>? Prerequisites { get; init; }
        public string? Branch { get; init; }
        public KnowledgeEffectDto[]? Effects { get; init; }
        public CraftingModifierDto[]? CraftingModifiers { get; init; }
    }

    private record KnowledgeEffectDto
    {
        public string? EffectType { get; init; }
        public string? TargetTag { get; init; }
        public Dictionary<string, object>? Metadata { get; init; }
    }

    private record HarvestEffectDto
    {
        public string? NodeTag { get; init; }
        public string? StepModified { get; init; }
        public float Value { get; init; }
        public string? Operation { get; init; }
    }

    private record CraftingModifierDto
    {
        public string? TargetTag { get; init; }
        public string? Scope { get; init; }
        public string? StepModified { get; init; }
        public float Value { get; init; }
        public string? Operation { get; init; }
    }

    private record RecipeDto
    {
        public string? RecipeId { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public string? IndustryTag { get; init; }
        public List<string>? RequiredKnowledge { get; init; }
        public IngredientDto[]? Ingredients { get; init; }
        public ProductDto[]? Products { get; init; }
        public int? CraftingTimeRounds { get; init; }
        public int KnowledgePointsAwarded { get; init; }
        public string? RequiredWorkstation { get; init; }
        public List<string>? RequiredTools { get; init; }
    }

    private record IngredientDto
    {
        public string? ItemTag { get; init; }
        public int Quantity { get; init; }
        public int? MinQuality { get; init; }
        public bool IsConsumed { get; init; } = true;
    }

    private record ProductDto
    {
        public string? ItemTag { get; init; }
        public int Quantity { get; init; }
        public float? SuccessChance { get; init; }
    }
}

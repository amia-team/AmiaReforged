using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Effects;
using Anvil;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for managing trait definitions.
/// Supports full CRUD operations for the admin panel.
/// </summary>
public class TraitController
{
    private const string BasePath = "/api/worldengine/traits";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// List all trait definitions with optional search, category filter, and pagination.
    /// GET /api/worldengine/traits?search=&amp;category=&amp;deathBehavior=&amp;dmOnly=&amp;page=1&amp;pageSize=50
    /// </summary>
    [HttpGet(BasePath)]
    public static async Task<ApiResult> GetAll(RouteContext ctx)
    {
        string? search = ctx.GetQueryParam("search");
        string? category = ctx.GetQueryParam("category");
        string? deathBehavior = ctx.GetQueryParam("deathBehavior");
        string? dmOnly = ctx.GetQueryParam("dmOnly");
        int page = int.TryParse(ctx.GetQueryParam("page"), out var p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out var ps) ? Math.Clamp(ps, 1, 200) : 50;

        using var context = ResolveContext();

        IQueryable<PersistedTraitDefinition> query = context.TraitDefinitions;

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(d =>
                d.Tag.ToLower().Contains(term) ||
                d.Name.ToLower().Contains(term) ||
                d.Description.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            if (Enum.TryParse<TraitCategory>(category.Trim(), true, out var cat))
                query = query.Where(d => d.Category == cat);
        }

        if (!string.IsNullOrWhiteSpace(deathBehavior))
        {
            if (Enum.TryParse<TraitDeathBehavior>(deathBehavior.Trim(), true, out var db))
                query = query.Where(d => d.DeathBehavior == db);
        }

        if (!string.IsNullOrWhiteSpace(dmOnly))
        {
            if (bool.TryParse(dmOnly.Trim(), out var dm))
                query = query.Where(d => d.DmOnly == dm);
        }

        int totalCount = await query.CountAsync();

        List<PersistedTraitDefinition> items = await query
            .OrderBy(d => d.Category)
            .ThenBy(d => d.Name)
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
    /// Get a single trait definition by tag.
    /// GET /api/worldengine/traits/{tag}
    /// </summary>
    [HttpGet(BasePath + "/{tag}")]
    public static async Task<ApiResult> GetByTag(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");

        using var context = ResolveContext();
        var definition = await context.TraitDefinitions.FindAsync(tag);

        if (definition == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No trait definition with tag '{tag}'"));
        }

        return new ApiResult(200, ToDto(definition));
    }

    /// <summary>
    /// Create a new trait definition.
    /// POST /api/worldengine/traits
    /// </summary>
    [HttpPost(BasePath)]
    public static async Task<ApiResult> Create(RouteContext ctx)
    {
        var dto = await ctx.ReadJsonBodyAsync<TraitDefinitionDto>();
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

        bool exists = await context.TraitDefinitions.AnyAsync(d => d.Tag == dto.Tag);
        if (exists)
        {
            return new ApiResult(409, new ErrorResponse(
                "Conflict", $"A trait definition with tag '{dto.Tag}' already exists"));
        }

        var definition = FromDto(dto);
        definition.CreatedUtc = DateTime.UtcNow;
        definition.UpdatedUtc = DateTime.UtcNow;

        context.TraitDefinitions.Add(definition);
        await context.SaveChangesAsync();

        // Refresh the in-memory cache
        RefreshInMemoryCache(definition);

        return new ApiResult(201, ToDto(definition));
    }

    /// <summary>
    /// Update an existing trait definition.
    /// PUT /api/worldengine/traits/{tag}
    /// </summary>
    [HttpPut(BasePath + "/{tag}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");

        using var context = ResolveContext();
        var existing = await context.TraitDefinitions.FindAsync(tag);

        if (existing == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No trait definition with tag '{tag}'"));
        }

        var dto = await ctx.ReadJsonBodyAsync<TraitDefinitionDto>();
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
        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.PointCost = dto.PointCost;
        existing.Category = Enum.TryParse<TraitCategory>(dto.Category, true, out var cat)
            ? cat : TraitCategory.Background;
        existing.DeathBehavior = Enum.TryParse<TraitDeathBehavior>(dto.DeathBehavior, true, out var db)
            ? db : TraitDeathBehavior.Persist;
        existing.RequiresUnlock = dto.RequiresUnlock;
        existing.DmOnly = dto.DmOnly;
        existing.EffectsJson = JsonSerializer.Serialize(dto.Effects ?? [], JsonOptions);
        existing.AllowedRacesJson = JsonSerializer.Serialize(dto.AllowedRaces ?? [], JsonOptions);
        existing.AllowedClassesJson = JsonSerializer.Serialize(dto.AllowedClasses ?? [], JsonOptions);
        existing.ForbiddenRacesJson = JsonSerializer.Serialize(dto.ForbiddenRaces ?? [], JsonOptions);
        existing.ForbiddenClassesJson = JsonSerializer.Serialize(dto.ForbiddenClasses ?? [], JsonOptions);
        existing.ConflictingTraitsJson = JsonSerializer.Serialize(dto.ConflictingTraits ?? [], JsonOptions);
        existing.PrerequisiteTraitsJson = JsonSerializer.Serialize(dto.PrerequisiteTraits ?? [], JsonOptions);
        existing.UpdatedUtc = DateTime.UtcNow;

        await context.SaveChangesAsync();

        // Refresh the in-memory cache
        RefreshInMemoryCache(existing);

        return new ApiResult(200, ToDto(existing));
    }

    /// <summary>
    /// Delete a trait definition.
    /// DELETE /api/worldengine/traits/{tag}
    /// </summary>
    [HttpDelete(BasePath + "/{tag}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");

        using var context = ResolveContext();
        var existing = await context.TraitDefinitions.FindAsync(tag);

        if (existing == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No trait definition with tag '{tag}'"));
        }

        // Remove any character trait selections referencing this definition
        var characterTraits = await context.CharacterTraits
            .Where(ct => ct.TraitTag == tag)
            .ToListAsync();

        if (characterTraits.Count > 0)
        {
            context.CharacterTraits.RemoveRange(characterTraits);
        }

        context.TraitDefinitions.Remove(existing);
        await context.SaveChangesAsync();

        // Remove from in-memory cache
        RemoveFromInMemoryCache(tag);

        return new ApiResult(204, new { message = "Deleted" });
    }

    /// <summary>
    /// Get all trait category enum values.
    /// GET /api/worldengine/traits/categories
    /// </summary>
    [HttpGet(BasePath + "/categories")]
    public static Task<ApiResult> GetCategories(RouteContext ctx)
    {
        var categories = Enum.GetValues<TraitCategory>()
            .Select(c => new { id = (int)c, name = c.ToString() })
            .OrderBy(c => c.id)
            .ToList();

        return Task.FromResult(new ApiResult(200, categories));
    }

    /// <summary>
    /// Get all death behavior enum values.
    /// GET /api/worldengine/traits/death-behaviors
    /// </summary>
    [HttpGet(BasePath + "/death-behaviors")]
    public static Task<ApiResult> GetDeathBehaviors(RouteContext ctx)
    {
        var behaviors = Enum.GetValues<TraitDeathBehavior>()
            .Select(b => new { id = (int)b, name = b.ToString() })
            .OrderBy(b => b.id)
            .ToList();

        return Task.FromResult(new ApiResult(200, behaviors));
    }

    /// <summary>
    /// Get all effect type enum values.
    /// GET /api/worldengine/traits/effect-types
    /// </summary>
    [HttpGet(BasePath + "/effect-types")]
    public static Task<ApiResult> GetEffectTypes(RouteContext ctx)
    {
        var types = Enum.GetValues<TraitEffectType>()
            .Select(t => new { id = (int)t, name = t.ToString() })
            .OrderBy(t => t.id)
            .ToList();

        return Task.FromResult(new ApiResult(200, types));
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

    private static void RefreshInMemoryCache(PersistedTraitDefinition persisted)
    {
        try
        {
            var mapper = AnvilCore.GetService<TraitDefinitionMapper>();
            var repository = AnvilCore.GetService<ITraitRepository>();

            if (mapper != null && repository != null)
            {
                Trait trait = mapper.ToDomain(persisted);
                repository.Add(trait);
            }
        }
        catch
        {
            // Best-effort cache refresh — trait will be loaded on next server restart
        }
    }

    private static void RemoveFromInMemoryCache(string traitTag)
    {
        try
        {
            var repository = AnvilCore.GetService<ITraitRepository>();
            repository?.Remove(traitTag);
        }
        catch
        {
            // Best-effort cache removal — trait will be gone on next server restart
        }
    }

    private static string? ValidateDto(TraitDefinitionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Tag)) return "Tag is required";
        if (dto.Tag.Length > 50) return "Tag must not exceed 50 characters";
        if (string.IsNullOrWhiteSpace(dto.Name)) return "Name is required";
        if (dto.Name.Length > 200) return "Name must not exceed 200 characters";
        if (string.IsNullOrWhiteSpace(dto.Description)) return "Description is required";

        if (!string.IsNullOrWhiteSpace(dto.Category) &&
            !Enum.TryParse<TraitCategory>(dto.Category, true, out _))
            return $"Invalid category '{dto.Category}'";

        if (!string.IsNullOrWhiteSpace(dto.DeathBehavior) &&
            !Enum.TryParse<TraitDeathBehavior>(dto.DeathBehavior, true, out _))
            return $"Invalid death behavior '{dto.DeathBehavior}'";

        if (dto.Effects != null)
        {
            foreach (var effect in dto.Effects)
            {
                if (!Enum.IsDefined(typeof(TraitEffectType), effect.EffectType))
                    return $"Invalid effect type '{effect.EffectType}'";
            }
        }

        return null;
    }

    private static object ToDto(PersistedTraitDefinition def)
    {
        return new
        {
            def.Tag,
            def.Name,
            def.Description,
            def.PointCost,
            Category = def.Category.ToString(),
            DeathBehavior = def.DeathBehavior.ToString(),
            def.RequiresUnlock,
            def.DmOnly,
            Effects = DeserializeJson<List<TraitEffectApiDto>>(def.EffectsJson) ?? [],
            AllowedRaces = DeserializeJson<List<string>>(def.AllowedRacesJson) ?? [],
            AllowedClasses = DeserializeJson<List<string>>(def.AllowedClassesJson) ?? [],
            ForbiddenRaces = DeserializeJson<List<string>>(def.ForbiddenRacesJson) ?? [],
            ForbiddenClasses = DeserializeJson<List<string>>(def.ForbiddenClassesJson) ?? [],
            ConflictingTraits = DeserializeJson<List<string>>(def.ConflictingTraitsJson) ?? [],
            PrerequisiteTraits = DeserializeJson<List<string>>(def.PrerequisiteTraitsJson) ?? [],
            def.CreatedUtc,
            def.UpdatedUtc
        };
    }

    private static PersistedTraitDefinition FromDto(TraitDefinitionDto dto)
    {
        return new PersistedTraitDefinition
        {
            Tag = dto.Tag.Trim(),
            Name = dto.Name.Trim(),
            Description = dto.Description,
            PointCost = dto.PointCost,
            Category = Enum.TryParse<TraitCategory>(dto.Category, true, out var cat)
                ? cat : TraitCategory.Background,
            DeathBehavior = Enum.TryParse<TraitDeathBehavior>(dto.DeathBehavior, true, out var db)
                ? db : TraitDeathBehavior.Persist,
            RequiresUnlock = dto.RequiresUnlock,
            DmOnly = dto.DmOnly,
            EffectsJson = JsonSerializer.Serialize(dto.Effects ?? [], JsonOptions),
            AllowedRacesJson = JsonSerializer.Serialize(dto.AllowedRaces ?? [], JsonOptions),
            AllowedClassesJson = JsonSerializer.Serialize(dto.AllowedClasses ?? [], JsonOptions),
            ForbiddenRacesJson = JsonSerializer.Serialize(dto.ForbiddenRaces ?? [], JsonOptions),
            ForbiddenClassesJson = JsonSerializer.Serialize(dto.ForbiddenClasses ?? [], JsonOptions),
            ConflictingTraitsJson = JsonSerializer.Serialize(dto.ConflictingTraits ?? [], JsonOptions),
            PrerequisiteTraitsJson = JsonSerializer.Serialize(dto.PrerequisiteTraits ?? [], JsonOptions)
        };
    }

    private static T? DeserializeJson<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try { return JsonSerializer.Deserialize<T>(json, JsonOptions); }
        catch { return default; }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DTOs
    // ═══════════════════════════════════════════════════════════════════

    private record TraitDefinitionDto
    {
        public string Tag { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public int PointCost { get; init; }
        public string Category { get; init; } = "Background";
        public string DeathBehavior { get; init; } = "Persist";
        public bool RequiresUnlock { get; init; }
        public bool DmOnly { get; init; }
        public List<TraitEffectApiDto>? Effects { get; init; }
        public List<string>? AllowedRaces { get; init; }
        public List<string>? AllowedClasses { get; init; }
        public List<string>? ForbiddenRaces { get; init; }
        public List<string>? ForbiddenClasses { get; init; }
        public List<string>? ConflictingTraits { get; init; }
        public List<string>? PrerequisiteTraits { get; init; }
    }

    private record TraitEffectApiDto
    {
        public int EffectType { get; init; }
        public string? Target { get; init; }
        public int Magnitude { get; init; }
        public string? Description { get; init; }
    }
}

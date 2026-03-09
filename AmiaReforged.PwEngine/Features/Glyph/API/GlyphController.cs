using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.WorldEngine.API;

namespace AmiaReforged.PwEngine.Features.Glyph.API;

/// <summary>
/// HTTP API controller for managing Glyph definitions and profile bindings.
/// Auto-discovered by the WorldEngine route table via [HttpGet]/[HttpPost]/etc. attributes.
/// Static service references are set by <see cref="GlyphApiBootstrap"/> at startup.
/// </summary>
public class GlyphController
{
    internal static IGlyphRepository? Repository;
    internal static IGlyphNodeDefinitionRegistry? NodeRegistry;
    internal static Integration.GlyphEncounterHookService? EncounterHooks;
    internal static Integration.GlyphTraitHookService? TraitHooks;
    internal static Integration.GlyphInteractionHookService? InteractionHooks;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private const string BasePath = "/api/worldengine/glyphs";

    // ==================== Definitions ====================

    /// <summary>
    /// GET /api/worldengine/glyphs — List all Glyph definitions.
    /// </summary>
    [HttpGet("/api/worldengine/glyphs")]
    public static async Task<ApiResult> ListDefinitions(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        List<GlyphDefinition> definitions = await Repository.GetAllDefinitionsAsync();
        return new ApiResult(200, definitions.Select(ToDto).ToList());
    }

    /// <summary>
    /// GET /api/worldengine/glyphs/{id} — Get a single definition with full graph JSON.
    /// </summary>
    [HttpGet("/api/worldengine/glyphs/{id}")]
    public static async Task<ApiResult> GetDefinition(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid definition ID."));

        GlyphDefinition? definition = await Repository.GetDefinitionByIdAsync(id);
        return definition != null
            ? new ApiResult(200, ToDto(definition))
            : new ApiResult(404, new ErrorResponse("Not found", $"Glyph definition {id} not found."));
    }

    /// <summary>
    /// POST /api/worldengine/glyphs — Create a new Glyph definition.
    /// </summary>
    [HttpPost("/api/worldengine/glyphs")]
    public static async Task<ApiResult> CreateDefinition(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        CreateGlyphRequest? req = await ctx.ReadJsonBodyAsync<CreateGlyphRequest>();
        if (req == null || string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.EventType))
            return new ApiResult(400, new ErrorResponse("Bad request", "Name and EventType are required."));

        GlyphDefinition definition = new()
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Description = req.Description,
            EventType = req.EventType,
            Category = req.Category,
            GraphJson = req.GraphJson ?? "{}",
            IsActive = req.IsActive
        };

        await Repository.CreateDefinitionAsync(definition);
        return new ApiResult(201, ToDto(definition));
    }

    /// <summary>
    /// PUT /api/worldengine/glyphs/{id} — Update an existing Glyph definition.
    /// </summary>
    [HttpPut("/api/worldengine/glyphs/{id}")]
    public static async Task<ApiResult> UpdateDefinition(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid definition ID."));

        GlyphDefinition? definition = await Repository.GetDefinitionByIdAsync(id);
        if (definition == null)
            return new ApiResult(404, new ErrorResponse("Not found", $"Glyph definition {id} not found."));

        UpdateGlyphRequest? req = await ctx.ReadJsonBodyAsync<UpdateGlyphRequest>();
        if (req == null)
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required."));

        if (req.Name != null) definition.Name = req.Name;
        if (req.Description != null) definition.Description = req.Description;
        if (req.EventType != null) definition.EventType = req.EventType;
        if (req.Category != null) definition.Category = req.Category;
        if (req.GraphJson != null) definition.GraphJson = req.GraphJson;
        if (req.IsActive.HasValue) definition.IsActive = req.IsActive.Value;

        await Repository.UpdateDefinitionAsync(definition);
        return new ApiResult(200, ToDto(definition));
    }

    /// <summary>
    /// DELETE /api/worldengine/glyphs/{id} — Delete a Glyph definition and all its bindings.
    /// </summary>
    [HttpDelete("/api/worldengine/glyphs/{id}")]
    public static async Task<ApiResult> DeleteDefinition(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid definition ID."));

        await Repository.DeleteDefinitionAsync(id);
        return new ApiResult(204, new { });
    }

    // ==================== Node Catalog ====================

    /// <summary>
    /// GET /api/worldengine/glyph-catalog — Return all registered node definitions for the editor palette.
    /// </summary>
    [HttpGet("/api/worldengine/glyph-catalog")]
    public static Task<ApiResult> GetNodeCatalog(RouteContext ctx)
    {
        if (NodeRegistry == null) return Task.FromResult(ServiceUnavailable());

        IReadOnlyList<GlyphNodeDefinition> definitions = NodeRegistry.GetAll();
        var catalog = definitions.Select(NodeDefinitionToDto).ToList();
        return Task.FromResult(new ApiResult(200, catalog));
    }

    // ==================== Bindings ====================

    /// <summary>
    /// GET /api/worldengine/glyphs/bindings?profileId={id} — List bindings for a profile.
    /// </summary>
    [HttpGet("/api/worldengine/glyphs/bindings")]
    public static async Task<ApiResult> ListBindings(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        string? profileIdStr = ctx.GetQueryParam("profileId");
        if (profileIdStr != null && Guid.TryParse(profileIdStr, out Guid profileId))
        {
            List<SpawnProfileGlyphBinding> bindings = await Repository.GetBindingsForProfileAsync(profileId);
            return new ApiResult(200, bindings.Select(BindingToDto).ToList());
        }

        // No profileId filter — return all
        List<SpawnProfileGlyphBinding> allBindings = await Repository.GetAllBindingsAsync();
        return new ApiResult(200, allBindings.Select(BindingToDto).ToList());
    }

    /// <summary>
    /// POST /api/worldengine/glyphs/bindings — Bind a Glyph definition to a spawn profile.
    /// </summary>
    [HttpPost("/api/worldengine/glyphs/bindings")]
    public static async Task<ApiResult> CreateBinding(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        CreateBindingRequest? req = await ctx.ReadJsonBodyAsync<CreateBindingRequest>();
        if (req == null)
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required."));

        SpawnProfileGlyphBinding binding = new()
        {
            Id = Guid.NewGuid(),
            SpawnProfileId = req.SpawnProfileId,
            GlyphDefinitionId = req.GlyphDefinitionId,
            Priority = req.Priority
        };

        await Repository.CreateBindingAsync(binding);

        // Auto-refresh encounter hook cache so the new binding takes effect immediately
        if (EncounterHooks != null) await EncounterHooks.RefreshCacheAsync();

        return new ApiResult(201, BindingToDto(binding));
    }

    /// <summary>
    /// DELETE /api/worldengine/glyphs/bindings/{id} — Remove a binding.
    /// </summary>
    [HttpDelete("/api/worldengine/glyphs/bindings/{id}")]
    public static async Task<ApiResult> DeleteBinding(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid binding ID."));

        await Repository.DeleteBindingAsync(id);

        // Auto-refresh encounter hook cache
        if (EncounterHooks != null) await EncounterHooks.RefreshCacheAsync();

        return new ApiResult(204, new { });
    }

    // ==================== Trait Bindings ====================

    /// <summary>
    /// GET /api/worldengine/glyphs/trait-bindings?traitTag={tag} — List trait bindings, optionally filtered by tag.
    /// </summary>
    [HttpGet("/api/worldengine/glyphs/trait-bindings")]
    public static async Task<ApiResult> ListTraitBindings(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        string? traitTag = ctx.GetQueryParam("traitTag");
        if (!string.IsNullOrEmpty(traitTag))
        {
            List<TraitGlyphBinding> bindings = await Repository.GetTraitBindingsForTagAsync(traitTag);
            return new ApiResult(200, bindings.Select(TraitBindingToDto).ToList());
        }

        List<TraitGlyphBinding> allBindings = await Repository.GetAllTraitBindingsAsync();
        return new ApiResult(200, allBindings.Select(TraitBindingToDto).ToList());
    }

    /// <summary>
    /// POST /api/worldengine/glyphs/trait-bindings — Bind a Glyph definition to a trait tag.
    /// </summary>
    [HttpPost("/api/worldengine/glyphs/trait-bindings")]
    public static async Task<ApiResult> CreateTraitBinding(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        CreateTraitBindingRequest? req = await ctx.ReadJsonBodyAsync<CreateTraitBindingRequest>();
        if (req == null || string.IsNullOrWhiteSpace(req.TraitTag))
            return new ApiResult(400, new ErrorResponse("Bad request", "TraitTag and GlyphDefinitionId are required."));

        TraitGlyphBinding binding = new()
        {
            Id = Guid.NewGuid(),
            TraitTag = req.TraitTag.Trim(),
            GlyphDefinitionId = req.GlyphDefinitionId,
            Priority = req.Priority
        };

        await Repository.CreateTraitBindingAsync(binding);

        // Auto-refresh trait hook cache so the new binding takes effect immediately
        if (TraitHooks != null) await TraitHooks.RefreshCacheAsync();

        return new ApiResult(201, TraitBindingToDto(binding));
    }

    /// <summary>
    /// DELETE /api/worldengine/glyphs/trait-bindings/{id} — Remove a trait binding.
    /// </summary>
    [HttpDelete("/api/worldengine/glyphs/trait-bindings/{id}")]
    public static async Task<ApiResult> DeleteTraitBinding(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid binding ID."));

        await Repository.DeleteTraitBindingAsync(id);

        // Auto-refresh trait hook cache
        if (TraitHooks != null) await TraitHooks.RefreshCacheAsync();

        return new ApiResult(204, new { });
    }

    // ==================== Definition-Scoped Bindings ====================

    /// <summary>
    /// GET /api/worldengine/glyphs/{id}/bindings — Get all bindings (spawn profile + trait + interaction) for a definition.
    /// Used by the GlyphEditor's binding panel to show "what is this script bound to?"
    /// </summary>
    [HttpGet("/api/worldengine/glyphs/{id}/bindings")]
    public static async Task<ApiResult> GetDefinitionBindings(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid definition ID."));

        List<SpawnProfileGlyphBinding> spawnBindings = await Repository.GetSpawnBindingsForDefinitionAsync(id);
        List<TraitGlyphBinding> traitBindings = await Repository.GetTraitBindingsForDefinitionAsync(id);
        List<InteractionGlyphBinding> interactionBindings = await Repository.GetInteractionBindingsForDefinitionAsync(id);

        return new ApiResult(200, new DefinitionBindingsResponse(
            spawnBindings.Select(BindingToDto).ToList(),
            traitBindings.Select(TraitBindingToDto).ToList(),
            interactionBindings.Select(InteractionBindingToDto).ToList()
        ));
    }

    // ==================== Interaction Bindings ====================

    /// <summary>
    /// GET /api/worldengine/glyphs/interaction-bindings?interactionTag={tag} — List interaction bindings, optionally filtered by tag.
    /// </summary>
    [HttpGet("/api/worldengine/glyphs/interaction-bindings")]
    public static async Task<ApiResult> ListInteractionBindings(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        string? interactionTag = ctx.GetQueryParam("interactionTag");
        if (!string.IsNullOrEmpty(interactionTag))
        {
            List<InteractionGlyphBinding> bindings = await Repository.GetInteractionBindingsForTagAsync(interactionTag);
            return new ApiResult(200, bindings.Select(InteractionBindingToDto).ToList());
        }

        List<InteractionGlyphBinding> allBindings = await Repository.GetAllInteractionBindingsAsync();
        return new ApiResult(200, allBindings.Select(InteractionBindingToDto).ToList());
    }

    /// <summary>
    /// POST /api/worldengine/glyphs/interaction-bindings — Bind a Glyph definition to an interaction tag.
    /// </summary>
    [HttpPost("/api/worldengine/glyphs/interaction-bindings")]
    public static async Task<ApiResult> CreateInteractionBinding(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        CreateInteractionBindingRequest? req = await ctx.ReadJsonBodyAsync<CreateInteractionBindingRequest>();
        if (req == null || string.IsNullOrWhiteSpace(req.InteractionTag))
            return new ApiResult(400, new ErrorResponse("Bad request", "InteractionTag and GlyphDefinitionId are required."));

        InteractionGlyphBinding binding = new()
        {
            Id = Guid.NewGuid(),
            InteractionTag = req.InteractionTag.Trim(),
            AreaResRef = string.IsNullOrWhiteSpace(req.AreaResRef) ? null : req.AreaResRef.Trim(),
            GlyphDefinitionId = req.GlyphDefinitionId,
            Priority = req.Priority
        };

        await Repository.CreateInteractionBindingAsync(binding);

        // Auto-refresh interaction hook cache so the new binding takes effect immediately
        if (InteractionHooks != null) await InteractionHooks.RefreshCacheAsync();

        return new ApiResult(201, InteractionBindingToDto(binding));
    }

    /// <summary>
    /// DELETE /api/worldengine/glyphs/interaction-bindings/{id} — Remove an interaction binding.
    /// </summary>
    [HttpDelete("/api/worldengine/glyphs/interaction-bindings/{id}")]
    public static async Task<ApiResult> DeleteInteractionBinding(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid binding ID."));

        await Repository.DeleteInteractionBindingAsync(id);

        // Auto-refresh interaction hook cache
        if (InteractionHooks != null) await InteractionHooks.RefreshCacheAsync();

        return new ApiResult(204, new { });
    }

    // ==================== Helpers ====================

    private static ApiResult ServiceUnavailable()
        => new(503, new ErrorResponse("Service unavailable", "Glyph service is not initialized."));

    private static GlyphDefinitionDto ToDto(GlyphDefinition d) => new(
        d.Id, d.Name, d.Description, d.EventType, d.Category, d.GraphJson, d.IsActive,
        d.CreatedAt, d.UpdatedAt);

    private static GlyphBindingDto BindingToDto(SpawnProfileGlyphBinding b) => new(
        b.Id, b.SpawnProfileId, b.GlyphDefinitionId,
        b.GlyphDefinition?.Name ?? string.Empty, b.GlyphDefinition?.EventType ?? string.Empty,
        b.Priority);

    private static TraitGlyphBindingDto TraitBindingToDto(TraitGlyphBinding b) => new(
        b.Id, b.TraitTag, b.GlyphDefinitionId,
        b.GlyphDefinition?.Name ?? string.Empty, b.GlyphDefinition?.EventType ?? string.Empty,
        b.Priority);

    private static InteractionGlyphBindingDto InteractionBindingToDto(InteractionGlyphBinding b) => new(
        b.Id, b.InteractionTag, b.AreaResRef, b.GlyphDefinitionId,
        b.GlyphDefinition?.Name ?? string.Empty, b.GlyphDefinition?.EventType ?? string.Empty,
        b.Priority);

    private static GlyphNodeCatalogEntryDto NodeDefinitionToDto(GlyphNodeDefinition d) => new(
        d.TypeId, d.DisplayName, d.Category, d.Description, d.ColorClass,
        d.IsSingleton, d.RestrictToEventType?.ToString(), d.ScriptCategory?.ToString(),
        d.InputPins.Select(PinToDto).ToList(),
        d.OutputPins.Select(PinToDto).ToList());

    private static GlyphPinDto PinToDto(GlyphPin p) => new(
        p.Id, p.Name, p.DataType.ToString(), p.Direction.ToString(),
        p.DefaultValue, p.AllowMultipleConnections);

    // ==================== DTOs ====================

    public record GlyphDefinitionDto(
        Guid Id, string Name, string? Description, string EventType, string Category,
        string GraphJson, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);

    public record GlyphBindingDto(
        Guid Id, Guid SpawnProfileId, Guid GlyphDefinitionId,
        string GlyphName, string EventType, int Priority);

    public record TraitGlyphBindingDto(
        Guid Id, string TraitTag, Guid GlyphDefinitionId,
        string GlyphName, string EventType, int Priority);

    public record DefinitionBindingsResponse(
        List<GlyphBindingDto> SpawnProfileBindings,
        List<TraitGlyphBindingDto> TraitBindings,
        List<InteractionGlyphBindingDto> InteractionBindings);

    public record GlyphNodeCatalogEntryDto(
        string TypeId, string DisplayName, string Category, string Description,
        string ColorClass, bool IsSingleton, string? RestrictToEventType, string? ScriptCategory,
        List<GlyphPinDto> InputPins, List<GlyphPinDto> OutputPins);

    public record GlyphPinDto(
        string Id, string Name, string DataType, string Direction,
        string? DefaultValue, bool AllowMultipleConnections);

    public record CreateGlyphRequest(
        string Name, string EventType, string Category = "Encounter",
        string? Description = null, string? GraphJson = null, bool IsActive = false);

    public record UpdateGlyphRequest(
        string? Name = null, string? Description = null, string? EventType = null,
        string? Category = null, string? GraphJson = null, bool? IsActive = null);

    public record CreateBindingRequest(
        Guid SpawnProfileId, Guid GlyphDefinitionId, int Priority = 0);

    public record CreateTraitBindingRequest(
        string TraitTag, Guid GlyphDefinitionId, int Priority = 0);

    public record InteractionGlyphBindingDto(
        Guid Id, string InteractionTag, string? AreaResRef, Guid GlyphDefinitionId,
        string GlyphName, string EventType, int Priority);

    public record CreateInteractionBindingRequest(
        string InteractionTag, Guid GlyphDefinitionId, string? AreaResRef = null, int Priority = 0);
}

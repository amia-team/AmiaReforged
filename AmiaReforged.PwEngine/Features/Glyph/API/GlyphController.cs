using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;
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
    internal static GlyphBootstrap? Runtime;
    private static readonly SemaphoreSlim Mutations = new(1, 1);
    internal static Integration.GlyphEncounterHookService? EncounterHooks;
    internal static Integration.GlyphTraitHookService? TraitHooks;
    internal static Integration.GlyphInteractionHookService? InteractionHooks;

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
    /// GET /api/worldengine/glyphs/{id} — Get a single definition with source draft.
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
        if (Repository == null || Runtime == null) return ServiceUnavailable();
        CreateGlyphRequest? req = await ctx.ReadJsonBodyAsync<CreateGlyphRequest>();
        if (req == null || string.IsNullOrWhiteSpace(req.Name) || req.SourceText == null)
            return new(400, new ErrorResponse("Bad request", "Name and SourceText are required."));
        if (req.IsActive) return new(400, new ErrorResponse("Bad request", "Create a draft, then activate it."));
        var compilation = Runtime.Compiler.Compile(req.SourceText);
        if (!compilation.Success) return new(400, CompileResponse(compilation).Data);
        var program = compilation.Executable!;
        GlyphDefinition definition = new()
        {
            Id = Guid.NewGuid(), Name = req.Name, Description = req.Description,
            EventType = program.EventType.ToString(), Category = program.EventType.GetCategory().ToString(),
            SourceText = req.SourceText, IsActive = false
        };
        await Mutations.WaitAsync();
        try { await Repository.CreateDefinitionAsync(definition); }
        finally { Mutations.Release(); }
        return new ApiResult(201, ToDto(definition));
    }

    [HttpPut("/api/worldengine/glyphs/{id}")]
    public static async Task<ApiResult> UpdateDefinition(RouteContext ctx)
    {
        if (Repository == null || Runtime == null) return ServiceUnavailable();
        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id)) return new(400, new ErrorResponse("Bad request", "Invalid ID."));
        UpdateGlyphRequest? req = await ctx.ReadJsonBodyAsync<UpdateGlyphRequest>();
        if (req == null || req.IsActive == true) return new(400, new ErrorResponse("Bad request", "Save drafts here; use activate to publish."));
        await Mutations.WaitAsync();
        try
        {
            var definition = await Repository.GetDefinitionByIdAsync(id);
            if (definition == null) return new(404, new ErrorResponse("Not found", "Glyph definition not found."));
            if (req.Name != null) definition.Name = req.Name;
            if (req.Description != null) definition.Description = req.Description;
            if (req.SourceText != null) definition.SourceText = req.SourceText;
            if (req.IsActive == false) definition.IsActive = false;
            await Repository.UpdateDefinitionAsync(definition);
            if (req.IsActive == false) await Runtime.Programs.DeactivateAsync(id);
            return new(200, ToDto(definition));
        }
        finally { Mutations.Release(); }
    }

    [HttpGet("/api/worldengine/glyphs/language-metadata")]
    public static Task<ApiResult> LanguageMetadata(RouteContext ctx) => Task.FromResult(
        Runtime == null ? ServiceUnavailable() : new ApiResult(200, Runtime.LanguageMetadata));

    [HttpPost("/api/worldengine/glyphs/compile")]
    public static async Task<ApiResult> Compile(RouteContext ctx)
    {
        if (Runtime == null) return ServiceUnavailable();
        CompileGlyphRequest? req = await ctx.ReadJsonBodyAsync<CompileGlyphRequest>();
        if (req?.SourceText == null) return new(400, new ErrorResponse("Bad request", "SourceText is required."));
        return CompileResponse(Runtime.Compiler.Compile(req.SourceText, new(req.SourceId ?? "source.glyph", req.LanguageVersion)));
    }

    private static ApiResult CompileResponse(GlyphCompilationResult result) => new(200, new
    {
        result.Success, result.Diagnostics, result.SourceHash,
        EventType = result.Executable?.EventType.ToString(), LanguageVersion = GlyphLanguageVersion.Current
    });

    [HttpPost("/api/worldengine/glyphs/{id}/activate")]
    public static async Task<ApiResult> Activate(RouteContext ctx)
    {
        if (Repository == null || Runtime == null) return ServiceUnavailable();
        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id)) return new(400, new ErrorResponse("Bad request", "Invalid ID."));
        CompileGlyphRequest? req = await ctx.ReadJsonBodyAsync<CompileGlyphRequest>();
        await Mutations.WaitAsync();
        try
        {
            var definition = await Repository.GetDefinitionByIdAsync(id);
            if (definition == null) return new(404, new ErrorResponse("Not found", "Glyph definition not found."));
            Runtime.RestorePublished(definition);
            var compilation = Runtime.Compiler.Compile(req?.SourceText ?? definition.SourceText,
                new($"{id}.glyph", req?.LanguageVersion ?? definition.LanguageVersion));
            if (!compilation.Success) return CompileResponse(compilation);
            var candidate = compilation.Executable!;
            if (candidate.EventType.ToString() != definition.EventType)
                return new(409, new ErrorResponse("Event mismatch", "Create a new definition to change the event type."));
            var version = await Runtime.Programs.ActivateAsync(id, candidate, history => PersistVersion(definition, history));
            return new(200, new { Success = true, Version = VersionDto(version), Diagnostics = Array.Empty<object>() });
        }
        finally { Mutations.Release(); }
    }

    [HttpPost("/api/worldengine/glyphs/{id}/rollback")]
    public static async Task<ApiResult> Rollback(RouteContext ctx)
    {
        if (Repository == null || Runtime == null) return ServiceUnavailable();
        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id)) return new(400, new ErrorResponse("Bad request", "Invalid ID."));
        await Mutations.WaitAsync();
        try
        {
            var definition = await Repository.GetDefinitionByIdAsync(id);
            if (definition == null) return new(404, new ErrorResponse("Not found", "Glyph definition not found."));
            Runtime.RestorePublished(definition);
            var version = await Runtime.Programs.RollbackAsync(id, history => PersistVersion(definition, history));
            return version == null ? new(409, new ErrorResponse("No previous version", "There is no active rollback target.")) :
                new(200, new { Success = true, Version = VersionDto(version), Diagnostics = Array.Empty<object>() });
        }
        finally { Mutations.Release(); }
    }

    [HttpGet("/api/worldengine/glyphs/{id}/versions")]
    public static async Task<ApiResult> Versions(RouteContext ctx)
    {
        if (Repository == null || Runtime == null) return ServiceUnavailable();
        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id)) return new(400, new ErrorResponse("Bad request", "Invalid ID."));
        var definition = await Repository.GetDefinitionByIdAsync(id);
        if (definition == null) return new(404, new ErrorResponse("Not found", "Glyph definition not found."));
        Runtime.RestorePublished(definition);
        return new(200, Runtime.Programs.GetVersions(id).Select(VersionDto).ToArray());
    }

    [HttpGet("/api/worldengine/glyphs/{id}/traces")]
    public static Task<ApiResult> Traces(RouteContext ctx)
    {
        if (Runtime == null) return Task.FromResult(ServiceUnavailable());
        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return Task.FromResult(new ApiResult(400, new ErrorResponse("Bad request", "Invalid ID.")));
        return Task.FromResult(new ApiResult(200, Runtime.Traces.Get(id)));
    }

    private static object VersionDto(GlyphProgramVersion version) => new
    {
        version.VersionId, version.DefinitionId, version.ActivatedAt, version.PreviousVersionId,
        version.Executable.SourceHash, version.Executable.LanguageVersion,
        IsActive = Runtime?.Programs.GetActive(version.DefinitionId)?.VersionId == version.VersionId
    };

    private static async Task PersistVersion(GlyphDefinition definition, IReadOnlyList<GlyphProgramVersion> history)
    {
        var active = history[^1].Executable;
        // Repository failure occurs before the atomic publication; no active executable is modified.
        definition.SourceText = active.SourceText;
        definition.LanguageVersion = active.LanguageVersion;
        definition.PublishedVersionsJson = GlyphPublishedVersion.Serialize(history);
        definition.IsActive = true;
        await Repository!.UpdateDefinitionAsync(definition);
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

        await Mutations.WaitAsync();
        try
        {
            await Repository.DeleteDefinitionAsync(id);
            if (Runtime != null) await Runtime.Programs.DeactivateAsync(id);
        }
        finally { Mutations.Release(); }

        // Refresh all hook caches — cascade-deleted bindings are now stale in cache
        if (EncounterHooks != null) await EncounterHooks.RefreshCacheAsync();
        if (TraitHooks != null) await TraitHooks.RefreshCacheAsync();
        if (InteractionHooks != null) await InteractionHooks.RefreshCacheAsync();

        return new ApiResult(204, new { });
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
        d.Id, d.Name, d.Description, d.EventType, d.Category, d.SourceText, d.IsActive,
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

    // ==================== DTOs ====================

    public record GlyphDefinitionDto(
        Guid Id, string Name, string? Description, string EventType, string Category,
        string SourceText, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);

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

    public record CreateGlyphRequest(
        string Name, string EventType, string Category = "Encounter",
        string? Description = null, string? SourceText = null, bool IsActive = false);

    public record UpdateGlyphRequest(
        string? Name = null, string? Description = null, string? EventType = null,
        string? Category = null, string? SourceText = null, bool? IsActive = null);

    public record CompileGlyphRequest(string SourceText, string? SourceId = null, int LanguageVersion = 1);

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

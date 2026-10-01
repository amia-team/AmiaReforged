using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine Glyph source scripting API.
/// </summary>
public class GlyphApiService : ApiServiceBase
{
    private const string DefinitionsBase = "/api/worldengine/glyphs";
    private const string BindingsBase = "/api/worldengine/glyphs/bindings";
    private const string TraitBindingsBase = "/api/worldengine/glyphs/trait-bindings";
    private const string InteractionBindingsBase = "/api/worldengine/glyphs/interaction-bindings";

    public GlyphApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    private Task<GlyphLanguageMetadataDto?>? _languageMetadata;
    private int _endpointGeneration;

    public override void SelectEndpoint(Guid? endpointId)
    {
        if (SelectedEndpointId != endpointId)
        {
            _languageMetadata = null;
            _endpointGeneration++;
        }
        base.SelectEndpoint(endpointId);
    }

    public async Task<GlyphLanguageMetadataDto?> GetLanguageMetadataAsync()
    {
        int generation = _endpointGeneration;
        var request = _languageMetadata ??= GetAsync<GlyphLanguageMetadataDto>($"{DefinitionsBase}/language-metadata");
        try
        {
            var result = await request;
            if (generation != _endpointGeneration)
                throw new OperationCanceledException("The selected WorldEngine endpoint changed while loading Glyph metadata.");
            if (result == null && ReferenceEquals(_languageMetadata, request)) _languageMetadata = null;
            return result;
        }
        catch
        {
            if (ReferenceEquals(_languageMetadata, request)) _languageMetadata = null;
            throw;
        }
    }

    // ==================== Definitions ====================

    public async Task<List<GlyphDefinitionDto>> GetAllDefinitionsAsync()
    {
        return await GetAsync<List<GlyphDefinitionDto>>(DefinitionsBase) ?? [];
    }

    public async Task<GlyphDefinitionDto?> GetDefinitionAsync(Guid id)
    {
        return await GetAsync<GlyphDefinitionDto>($"{DefinitionsBase}/{id}");
    }

    public async Task<GlyphDefinitionDto?> CreateDefinitionAsync(CreateGlyphRequest request)
    {
        return await PostAsync<GlyphDefinitionDto>(DefinitionsBase, request);
    }

    public async Task<GlyphDefinitionDto?> UpdateDefinitionAsync(Guid id, UpdateGlyphRequest request)
    {
        return await PutAsync<GlyphDefinitionDto>($"{DefinitionsBase}/{id}", request);
    }

    public async Task DeleteDefinitionAsync(Guid id)
    {
        await DeleteRequestAsync($"{DefinitionsBase}/{id}");
    }

    public async Task<GlyphCompilationDto?> CompileAsync(string source, int languageVersion = 2) =>
        await PostAsync<GlyphCompilationDto>($"{DefinitionsBase}/compile", new CompileGlyphRequest(source, LanguageVersion: languageVersion));
    public async Task<GlyphCompilationDto?> ActivateAsync(Guid id, string source, string? compilationHash = null, int languageVersion = 2) =>
        await PostAsync<GlyphCompilationDto>($"{DefinitionsBase}/{id}/activate", new CompileGlyphRequest(source, LanguageVersion: languageVersion, ExpectedCompilationHash: compilationHash));
    public async Task<GlyphCompilationDto?> RollbackAsync(Guid id) =>
        await PostAsync<GlyphCompilationDto>($"{DefinitionsBase}/{id}/rollback", new { });
    public async Task<List<GlyphVersionDto>> GetVersionsAsync(Guid id) =>
        await GetAsync<List<GlyphVersionDto>>($"{DefinitionsBase}/{id}/versions") ?? [];
    public async Task<List<GlyphTraceDto>> GetTracesAsync(Guid id) =>
        await GetAsync<List<GlyphTraceDto>>($"{DefinitionsBase}/{id}/traces") ?? [];

    public async Task<List<GlyphModuleDto>> GetModulesAsync() => await GetAsync<List<GlyphModuleDto>>("/api/worldengine/glyph-modules") ?? [];
    public async Task<GlyphModuleDto?> GetModuleAsync(Guid id) => await GetAsync<GlyphModuleDto>($"/api/worldengine/glyph-modules/{id}");
    public async Task<GlyphModuleDto?> CreateModuleAsync(string name) => await PostAsync<GlyphModuleDto>("/api/worldengine/glyph-modules", new GlyphModuleRequest(name));
    public async Task<GlyphModuleDto?> SaveModuleAsync(Guid id, GlyphModuleRequest request) => await PutAsync<GlyphModuleDto>($"/api/worldengine/glyph-modules/{id}", request);
    public async Task<GlyphCompilationDto?> CompileModuleAsync(string name, string source) => await PostAsync<GlyphCompilationDto>("/api/worldengine/glyph-modules/compile", new GlyphModuleRequest(name, source));
    public async Task<GlyphCompilationDto?> PublishModuleAsync(Guid id, string source, string? hash) => await PostAsync<GlyphCompilationDto>($"/api/worldengine/glyph-modules/{id}/publish", new GlyphModulePublicationRequest(source, hash));
    public async Task<GlyphModuleDto?> RollbackModuleAsync(Guid id) => await PostAsync<GlyphModuleDto>($"/api/worldengine/glyph-modules/{id}/rollback", new { });
    public async Task ArchiveModuleAsync(Guid id) => await DeleteRequestAsync($"/api/worldengine/glyph-modules/{id}");
    public async Task<GlyphModuleMetadataDto?> GetModuleMetadataAsync(string source, int languageVersion = 2)
    {
        int generation = _endpointGeneration;
        var result = await PostAsync<GlyphModuleMetadataDto>($"{DefinitionsBase}/module-metadata", new CompileGlyphRequest(source, LanguageVersion: languageVersion));
        if (generation != _endpointGeneration) throw new OperationCanceledException("The selected endpoint changed while loading module metadata.");
        return result;
    }

    // ==================== Bindings ====================

    public async Task<List<GlyphBindingDto>> GetBindingsForProfileAsync(Guid profileId)
    {
        return await GetAsync<List<GlyphBindingDto>>($"{BindingsBase}?profileId={profileId}") ?? [];
    }

    public async Task<List<GlyphBindingDto>> GetAllBindingsAsync()
    {
        return await GetAsync<List<GlyphBindingDto>>(BindingsBase) ?? [];
    }

    public async Task<GlyphBindingDto?> CreateBindingAsync(CreateGlyphBindingRequest request)
    {
        return await PostAsync<GlyphBindingDto>(BindingsBase, request);
    }

    public async Task DeleteBindingAsync(Guid id)
    {
        await DeleteRequestAsync($"{BindingsBase}/{id}");
    }

    // ==================== Trait Bindings ====================

    public async Task<List<TraitGlyphBindingDto>> GetTraitBindingsAsync(string? traitTag = null)
    {
        string url = string.IsNullOrEmpty(traitTag)
            ? TraitBindingsBase
            : $"{TraitBindingsBase}?traitTag={Uri.EscapeDataString(traitTag)}";
        return await GetAsync<List<TraitGlyphBindingDto>>(url) ?? [];
    }

    public async Task<TraitGlyphBindingDto?> CreateTraitBindingAsync(CreateTraitGlyphBindingRequest request)
    {
        return await PostAsync<TraitGlyphBindingDto>(TraitBindingsBase, request);
    }

    public async Task DeleteTraitBindingAsync(Guid id)
    {
        await DeleteRequestAsync($"{TraitBindingsBase}/{id}");
    }

    // ==================== Definition-Scoped Bindings ====================

    public async Task<DefinitionBindingsDto?> GetBindingsForDefinitionAsync(Guid definitionId)
    {
        return await GetAsync<DefinitionBindingsDto>($"{DefinitionsBase}/{definitionId}/bindings");
    }

    // ==================== Interaction Bindings ====================

    public async Task<List<InteractionGlyphBindingDto>> GetInteractionBindingsAsync(string? interactionTag = null)
    {
        string url = string.IsNullOrEmpty(interactionTag)
            ? InteractionBindingsBase
            : $"{InteractionBindingsBase}?interactionTag={Uri.EscapeDataString(interactionTag)}";
        return await GetAsync<List<InteractionGlyphBindingDto>>(url) ?? [];
    }

    public async Task<InteractionGlyphBindingDto?> CreateInteractionBindingAsync(CreateInteractionGlyphBindingRequest request)
    {
        return await PostAsync<InteractionGlyphBindingDto>(InteractionBindingsBase, request);
    }

    public async Task DeleteInteractionBindingAsync(Guid id)
    {
        await DeleteRequestAsync($"{InteractionBindingsBase}/{id}");
    }
}

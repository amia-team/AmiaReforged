using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine Glyph visual scripting API.
/// </summary>
public class GlyphApiService : ApiServiceBase
{
    private const string DefinitionsBase = "/api/worldengine/glyphs";
    private const string NodeCatalogPath = "/api/worldengine/glyph-catalog";
    private const string BindingsBase = "/api/worldengine/glyphs/bindings";
    private const string TraitBindingsBase = "/api/worldengine/glyphs/trait-bindings";
    private const string InteractionBindingsBase = "/api/worldengine/glyphs/interaction-bindings";

    public GlyphApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
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

    // ==================== Node Catalog ====================

    public async Task<List<GlyphNodeCatalogEntryDto>> GetNodeCatalogAsync()
    {
        return await GetAsync<List<GlyphNodeCatalogEntryDto>>(NodeCatalogPath) ?? [];
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

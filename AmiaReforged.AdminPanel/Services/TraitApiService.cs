using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine trait definitions API.
/// </summary>
public class TraitApiService : ApiServiceBase
{
    private const string TraitBase = "/api/worldengine/traits";

    public TraitApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    // ==================== CRUD Operations ====================

    public async Task<PagedResult<TraitDefinitionDto>> GetAllAsync(
        string? search = null, string? category = null, string? deathBehavior = null,
        bool? dmOnly = null, int page = 1, int pageSize = 50)
    {
        string url = $"{TraitBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrWhiteSpace(category))
            url += $"&category={Uri.EscapeDataString(category)}";
        if (!string.IsNullOrWhiteSpace(deathBehavior))
            url += $"&deathBehavior={Uri.EscapeDataString(deathBehavior)}";
        if (dmOnly.HasValue)
            url += $"&dmOnly={dmOnly.Value}";

        return await GetAsync<PagedResult<TraitDefinitionDto>>(url) ?? new PagedResult<TraitDefinitionDto>();
    }

    public async Task<TraitDefinitionDto?> GetByTagAsync(string tag)
    {
        return await GetAsync<TraitDefinitionDto>($"{TraitBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<TraitDefinitionDto?> CreateAsync(TraitDefinitionDto dto)
    {
        return await PostAsync<TraitDefinitionDto>(TraitBase, dto);
    }

    public async Task<TraitDefinitionDto?> UpdateAsync(string tag, TraitDefinitionDto dto)
    {
        return await PutAsync<TraitDefinitionDto>($"{TraitBase}/{Uri.EscapeDataString(tag)}", dto);
    }

    public async Task DeleteAsync(string tag)
    {
        await DeleteRequestAsync($"{TraitBase}/{Uri.EscapeDataString(tag)}");
    }

    // ==================== Enum Lookups ====================

    public async Task<List<EnumValueDto>> GetCategoriesAsync()
    {
        return await GetAsync<List<EnumValueDto>>($"{TraitBase}/categories") ?? [];
    }

    public async Task<List<EnumValueDto>> GetDeathBehaviorsAsync()
    {
        return await GetAsync<List<EnumValueDto>>($"{TraitBase}/death-behaviors") ?? [];
    }

    public async Task<List<EnumValueDto>> GetEffectTypesAsync()
    {
        return await GetAsync<List<EnumValueDto>>($"{TraitBase}/effect-types") ?? [];
    }
}

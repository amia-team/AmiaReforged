using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine codex lore definitions API.
/// </summary>
public class LoreApiService : ApiServiceBase
{
    private const string LoreBase = "/api/worldengine/codex/lore";

    public LoreApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    // ==================== CRUD Operations ====================

    public async Task<PagedResult<LoreDefinitionDto>> GetAllAsync(
        string? search = null, string? category = null, int page = 1, int pageSize = 50)
    {
        string url = $"{LoreBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrWhiteSpace(category))
            url += $"&category={Uri.EscapeDataString(category)}";

        return await GetAsync<PagedResult<LoreDefinitionDto>>(url) ?? new PagedResult<LoreDefinitionDto>();
    }

    public async Task<LoreDefinitionDto?> GetByIdAsync(string loreId)
    {
        return await GetAsync<LoreDefinitionDto>($"{LoreBase}/{Uri.EscapeDataString(loreId)}");
    }

    public async Task<LoreDefinitionDto?> CreateAsync(LoreDefinitionDto dto)
    {
        return await PostAsync<LoreDefinitionDto>(LoreBase, dto);
    }

    public async Task<LoreDefinitionDto?> UpdateAsync(string loreId, LoreDefinitionDto dto)
    {
        return await PutAsync<LoreDefinitionDto>($"{LoreBase}/{Uri.EscapeDataString(loreId)}", dto);
    }

    public async Task DeleteAsync(string loreId)
    {
        await DeleteRequestAsync($"{LoreBase}/{Uri.EscapeDataString(loreId)}");
    }
}

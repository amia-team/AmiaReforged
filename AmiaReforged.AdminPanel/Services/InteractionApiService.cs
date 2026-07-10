using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine interaction definitions API.
/// </summary>
public class InteractionApiService : ApiServiceBase
{
    private const string InteractionsBase = "/api/worldengine/interactions";

    public InteractionApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    // ==================== CRUD Operations ====================

    public async Task<PagedResult<InteractionDefinitionDto>> GetAllAsync(
        string? search = null, int page = 1, int pageSize = 50)
    {
        string url = $"{InteractionsBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        return await GetAsync<PagedResult<InteractionDefinitionDto>>(url) ?? new PagedResult<InteractionDefinitionDto>();
    }

    public async Task<InteractionDefinitionDto?> GetByTagAsync(string tag)
    {
        return await GetAsync<InteractionDefinitionDto>($"{InteractionsBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<InteractionDefinitionDto?> CreateAsync(InteractionDefinitionDto dto)
    {
        return await PostAsync<InteractionDefinitionDto>(InteractionsBase, dto);
    }

    public async Task<InteractionDefinitionDto?> UpdateAsync(string tag, InteractionDefinitionDto dto)
    {
        return await PutAsync<InteractionDefinitionDto>($"{InteractionsBase}/{Uri.EscapeDataString(tag)}", dto);
    }

    public async Task DeleteAsync(string tag)
    {
        await DeleteRequestAsync($"{InteractionsBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<ImportResult?> ImportJsonAsync(string json)
    {
        return await PostAsync<ImportResult>($"{InteractionsBase}/import", json, rawJson: true);
    }
}

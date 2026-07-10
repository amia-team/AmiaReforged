using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine resource node definition API.
/// </summary>
public class ResourceNodeApiService : ApiServiceBase
{
    private const string NodesBase = "/api/worldengine/resource-nodes";

    public ResourceNodeApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    // ==================== CRUD Operations ====================

    public async Task<PagedResult<ResourceNodeDefinitionDto>> GetAllAsync(
        string? search = null, string? type = null, int page = 1, int pageSize = 50)
    {
        string url = $"{NodesBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrWhiteSpace(type))
            url += $"&type={Uri.EscapeDataString(type)}";

        return await GetAsync<PagedResult<ResourceNodeDefinitionDto>>(url)
               ?? new PagedResult<ResourceNodeDefinitionDto>();
    }

    public async Task<ResourceNodeDefinitionDto?> GetByTagAsync(string tag)
    {
        return await GetAsync<ResourceNodeDefinitionDto>($"{NodesBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<ResourceNodeDefinitionDto?> CreateAsync(ResourceNodeDefinitionDto dto)
    {
        return await PostAsync<ResourceNodeDefinitionDto>(NodesBase, dto);
    }

    public async Task<ResourceNodeDefinitionDto?> UpdateAsync(string tag, ResourceNodeDefinitionDto dto)
    {
        return await PutAsync<ResourceNodeDefinitionDto>($"{NodesBase}/{Uri.EscapeDataString(tag)}", dto);
    }

    public async Task DeleteAsync(string tag)
    {
        await DeleteRequestAsync($"{NodesBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<ImportResult?> ImportJsonAsync(string jsonContent)
    {
        return await PostAsync<ImportResult>($"{NodesBase}/import", jsonContent, rawJson: true);
    }
}

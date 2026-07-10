using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine region definition API.
/// </summary>
public class RegionApiService : ApiServiceBase
{
    private const string RegionsBase = "/api/worldengine/regions";

    public RegionApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    // ==================== CRUD Operations ====================

    public async Task<PagedResult<RegionDefinitionDto>> GetAllAsync(
        string? search = null, int page = 1, int pageSize = 50)
    {
        string url = $"{RegionsBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        return await GetAsync<PagedResult<RegionDefinitionDto>>(url)
               ?? new PagedResult<RegionDefinitionDto>();
    }

    public async Task<RegionDefinitionDto?> GetByTagAsync(string tag)
    {
        return await GetAsync<RegionDefinitionDto>($"{RegionsBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<RegionDefinitionDto?> CreateAsync(RegionDefinitionDto dto)
    {
        return await PostAsync<RegionDefinitionDto>(RegionsBase, dto);
    }

    public async Task<RegionDefinitionDto?> UpdateAsync(string tag, RegionDefinitionDto dto)
    {
        return await PutAsync<RegionDefinitionDto>($"{RegionsBase}/{Uri.EscapeDataString(tag)}", dto);
    }

    public async Task DeleteAsync(string tag)
    {
        await DeleteRequestAsync($"{RegionsBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<ImportResult?> ImportJsonAsync(string jsonContent)
    {
        return await PostAsync<ImportResult>($"{RegionsBase}/import", jsonContent, rawJson: true);
    }

    public async Task<string> ExportJsonAsync(string? search = null)
    {
        string url = $"{RegionsBase}/export";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"?search={Uri.EscapeDataString(search)}";

        return await FetchExportJsonAsync(url);
    }
}

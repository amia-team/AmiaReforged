using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine workstation definition API.
/// </summary>
public class WorkstationApiService : ApiServiceBase
{
    private const string WorkstationsBase = "/api/worldengine/workstations";

    public WorkstationApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    // ==================== CRUD Operations ====================

    public async Task<PagedResult<WorkstationDefinitionDto>> GetAllAsync(
        string? search = null, int page = 1, int pageSize = 50)
    {
        string url = $"{WorkstationsBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        return await GetAsync<PagedResult<WorkstationDefinitionDto>>(url)
               ?? new PagedResult<WorkstationDefinitionDto>();
    }

    public async Task<WorkstationDefinitionDto?> GetByTagAsync(string tag)
    {
        return await GetAsync<WorkstationDefinitionDto>($"{WorkstationsBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<WorkstationDefinitionDto?> CreateAsync(WorkstationDefinitionDto dto)
    {
        return await PostAsync<WorkstationDefinitionDto>(WorkstationsBase, dto);
    }

    public async Task<WorkstationDefinitionDto?> UpdateAsync(string tag, WorkstationDefinitionDto dto)
    {
        return await PutAsync<WorkstationDefinitionDto>($"{WorkstationsBase}/{Uri.EscapeDataString(tag)}", dto);
    }

    public async Task DeleteAsync(string tag)
    {
        await DeleteRequestAsync($"{WorkstationsBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<ImportResult?> ImportJsonAsync(string jsonContent)
    {
        return await PostAsync<ImportResult>($"{WorkstationsBase}/import", jsonContent, rawJson: true);
    }

    public async Task<string> ExportJsonAsync()
    {
        return await FetchExportJsonAsync($"{WorkstationsBase}/export");
    }
}

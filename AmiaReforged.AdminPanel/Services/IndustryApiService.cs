using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine industry definition API.
/// </summary>
public class IndustryApiService : ApiServiceBase
{
    private const string IndustriesBase = "/api/worldengine/industries";
    private const string ProgressionConfigPath = "/api/worldengine/industries/progression-config";
    private const string CapProfilesBase = "/api/worldengine/industries/cap-profiles";

    public IndustryApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    // ==================== CRUD Operations ====================

    public async Task<PagedResult<IndustryDefinitionDto>> GetAllAsync(
        string? search = null, int page = 1, int pageSize = 50)
    {
        string url = $"{IndustriesBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        return await GetAsync<PagedResult<IndustryDefinitionDto>>(url)
               ?? new PagedResult<IndustryDefinitionDto>();
    }

    public async Task<IndustryDefinitionDto?> GetByTagAsync(string tag)
    {
        return await GetAsync<IndustryDefinitionDto>($"{IndustriesBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<IndustryDefinitionDto?> CreateAsync(IndustryDefinitionDto dto)
    {
        return await PostAsync<IndustryDefinitionDto>(IndustriesBase, dto);
    }

    public async Task<IndustryDefinitionDto?> UpdateAsync(string tag, IndustryDefinitionDto dto)
    {
        return await PutAsync<IndustryDefinitionDto>($"{IndustriesBase}/{Uri.EscapeDataString(tag)}", dto);
    }

    public async Task DeleteAsync(string tag)
    {
        await DeleteRequestAsync($"{IndustriesBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<ImportResult?> ImportJsonAsync(string jsonContent)
    {
        return await PostAsync<ImportResult>($"{IndustriesBase}/import", jsonContent, rawJson: true);
    }

    public async Task<string> ExportJsonAsync(string? search = null)
    {
        string url = $"{IndustriesBase}/export";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"?search={Uri.EscapeDataString(search)}";

        return await FetchExportJsonAsync(url);
    }

    // ==================== Knowledge Progression Config ====================

    public async Task<ProgressionConfigDto?> GetProgressionConfigAsync()
    {
        return await GetAsync<ProgressionConfigDto>(ProgressionConfigPath);
    }

    public async Task<ProgressionConfigDto?> UpdateProgressionConfigAsync(ProgressionConfigDto dto)
    {
        return await PutAsync<ProgressionConfigDto>(ProgressionConfigPath, dto);
    }

    // ==================== Knowledge Cap Profiles ====================

    public async Task<KnowledgeCapProfileDto[]?> GetCapProfilesAsync()
    {
        return await GetAsync<KnowledgeCapProfileDto[]>(CapProfilesBase);
    }

    public async Task<KnowledgeCapProfileDto?> CreateCapProfileAsync(KnowledgeCapProfileDto dto)
    {
        return await PostAsync<KnowledgeCapProfileDto>(CapProfilesBase, dto);
    }

    public async Task<KnowledgeCapProfileDto?> UpdateCapProfileAsync(string tag, KnowledgeCapProfileDto dto)
    {
        return await PutAsync<KnowledgeCapProfileDto>($"{CapProfilesBase}/{Uri.EscapeDataString(tag)}", dto);
    }

    public async Task DeleteCapProfileAsync(string tag)
    {
        await DeleteRequestAsync($"{CapProfilesBase}/{Uri.EscapeDataString(tag)}");
    }
}

using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine coinhouse (bank) API.
/// </summary>
public class CoinhouseApiService : ApiServiceBase
{
    private const string CoinhouseBase = "/api/worldengine/coinhouses";

    public CoinhouseApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    // ==================== CRUD Operations ====================

    public async Task<PagedResult<CoinhouseDto>> GetAllAsync(
        string? search = null, int page = 1, int pageSize = 50)
    {
        string url = $"{CoinhouseBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        return await GetAsync<PagedResult<CoinhouseDto>>(url) ?? new PagedResult<CoinhouseDto>();
    }

    public async Task<CoinhouseDto?> GetByTagAsync(string tag)
    {
        return await GetAsync<CoinhouseDto>($"{CoinhouseBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<CoinhouseDto?> CreateAsync(CoinhouseDto dto)
    {
        return await PostAsync<CoinhouseDto>(CoinhouseBase, dto);
    }

    public async Task<CoinhouseDto?> UpdateAsync(string tag, CoinhouseDto dto)
    {
        return await PutAsync<CoinhouseDto>($"{CoinhouseBase}/{Uri.EscapeDataString(tag)}", dto);
    }

    public async Task DeleteAsync(string tag)
    {
        await DeleteRequestAsync($"{CoinhouseBase}/{Uri.EscapeDataString(tag)}");
    }
}

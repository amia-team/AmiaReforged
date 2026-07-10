using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine item blueprint API.
/// </summary>
public class ItemApiService : ApiServiceBase
{
    private const string ItemsBase = "/api/worldengine/items";

    public ItemApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    // ==================== CRUD Operations ====================

    public async Task<PagedResult<ItemBlueprintDto>> GetAllAsync(string? search = null, int page = 1, int pageSize = 50)
    {
        string url = $"{ItemsBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        return await GetAsync<PagedResult<ItemBlueprintDto>>(url) ?? new PagedResult<ItemBlueprintDto>();
    }

    public async Task<ItemBlueprintDto?> GetByTagAsync(string tag)
    {
        return await GetAsync<ItemBlueprintDto>($"{ItemsBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<ItemEnumsDto?> GetEnumsAsync()
    {
        return await GetAsync<ItemEnumsDto>($"{ItemsBase}/enums");
    }

    public async Task<ItemBlueprintDto?> CreateAsync(ItemBlueprintDto dto)
    {
        return await PostAsync<ItemBlueprintDto>(ItemsBase, dto);
    }

    public async Task<ItemBlueprintDto?> UpdateAsync(string tag, ItemBlueprintDto dto)
    {
        return await PutAsync<ItemBlueprintDto>($"{ItemsBase}/{Uri.EscapeDataString(tag)}", dto);
    }

    public async Task DeleteAsync(string tag)
    {
        await DeleteRequestAsync($"{ItemsBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<ImportResult?> ImportJsonAsync(string jsonContent)
    {
        return await PostAsync<ImportResult>($"{ItemsBase}/import", jsonContent, rawJson: true);
    }

    public async Task<List<ItemBlueprintDto>> GetExpandedAsync(string tag)
    {
        return await GetAsync<List<ItemBlueprintDto>>($"{ItemsBase}/{Uri.EscapeDataString(tag)}/expanded")
               ?? new List<ItemBlueprintDto>();
    }

    public async Task<string> ExportJsonAsync(string? search = null)
    {
        string url = $"{ItemsBase}/export";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"?search={Uri.EscapeDataString(search)}";

        return await FetchExportJsonAsync(url);
    }
}

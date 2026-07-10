using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine recipe template API.
/// </summary>
public class RecipeTemplateApiService : ApiServiceBase
{
    private const string TemplatesBase = "/api/worldengine/recipe-templates";

    public RecipeTemplateApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    // ==================== CRUD Operations ====================

    public async Task<PagedResult<RecipeTemplateDefinitionDto>> GetAllAsync(
        string? search = null, int page = 1, int pageSize = 50)
    {
        string url = $"{TemplatesBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        return await GetAsync<PagedResult<RecipeTemplateDefinitionDto>>(url)
               ?? new PagedResult<RecipeTemplateDefinitionDto>();
    }

    public async Task<RecipeTemplateDefinitionDto?> GetByTagAsync(string tag)
    {
        return await GetAsync<RecipeTemplateDefinitionDto>($"{TemplatesBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<List<RecipeTemplateDefinitionDto>> GetByIndustryAsync(string industryTag)
    {
        return await GetAsync<List<RecipeTemplateDefinitionDto>>(
                   $"{TemplatesBase}/industry/{Uri.EscapeDataString(industryTag)}")
               ?? [];
    }

    public async Task<RecipeTemplateDefinitionDto?> CreateAsync(RecipeTemplateDefinitionDto dto)
    {
        return await PostAsync<RecipeTemplateDefinitionDto>(TemplatesBase, dto);
    }

    public async Task<RecipeTemplateDefinitionDto?> UpdateAsync(string tag, RecipeTemplateDefinitionDto dto)
    {
        return await PutAsync<RecipeTemplateDefinitionDto>(
            $"{TemplatesBase}/{Uri.EscapeDataString(tag)}", dto);
    }

    public async Task DeleteAsync(string tag)
    {
        await DeleteRequestAsync($"{TemplatesBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<RecipeTemplateEnumsDto?> GetEnumsAsync()
    {
        return await GetAsync<RecipeTemplateEnumsDto>($"{TemplatesBase}/enums");
    }

    public async Task InvalidateCacheAsync()
    {
        await PostAsync<object>($"{TemplatesBase}/invalidate", null);
    }
}

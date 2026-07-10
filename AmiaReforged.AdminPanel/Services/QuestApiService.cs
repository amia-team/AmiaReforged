using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine codex quest definitions API.
/// </summary>
public class QuestApiService : ApiServiceBase
{
    private const string QuestBase = "/api/worldengine/codex/quests";

    public QuestApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    // ==================== CRUD Operations ====================

    public async Task<PagedResult<QuestDefinitionDto>> GetAllAsync(
        string? search = null, int page = 1, int pageSize = 50)
    {
        string url = $"{QuestBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        return await GetAsync<PagedResult<QuestDefinitionDto>>(url) ?? new PagedResult<QuestDefinitionDto>();
    }

    public async Task<QuestDefinitionDto?> GetByIdAsync(string questId)
    {
        return await GetAsync<QuestDefinitionDto>($"{QuestBase}/{Uri.EscapeDataString(questId)}");
    }

    public async Task<QuestDefinitionDto?> CreateAsync(QuestDefinitionDto dto)
    {
        return await PostAsync<QuestDefinitionDto>(QuestBase, dto);
    }

    public async Task<QuestDefinitionDto?> UpdateAsync(string questId, QuestDefinitionDto dto)
    {
        return await PutAsync<QuestDefinitionDto>($"{QuestBase}/{Uri.EscapeDataString(questId)}", dto);
    }

    public async Task DeleteAsync(string questId)
    {
        await DeleteRequestAsync($"{QuestBase}/{Uri.EscapeDataString(questId)}");
    }
}

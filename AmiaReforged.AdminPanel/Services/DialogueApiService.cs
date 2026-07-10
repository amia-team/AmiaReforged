using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine dialogue trees API.
/// </summary>
public class DialogueApiService : ApiServiceBase
{
    private const string DialogueBase = "/api/worldengine/dialogue";

    public DialogueApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    // ==================== CRUD Operations ====================

    public async Task<PagedResult<DialogueTreeDto>> GetAllAsync(
        string? search = null, int page = 1, int pageSize = 50)
    {
        string url = $"{DialogueBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        return await GetAsync<PagedResult<DialogueTreeDto>>(url) ?? new PagedResult<DialogueTreeDto>();
    }

    public async Task<DialogueTreeDto?> GetByIdAsync(string dialogueTreeId)
    {
        return await GetAsync<DialogueTreeDto>(
            $"{DialogueBase}/{Uri.EscapeDataString(dialogueTreeId)}");
    }

    public async Task<PagedResult<DialogueTreeDto>> GetBySpeakerTagAsync(string speakerTag)
    {
        return await GetAsync<PagedResult<DialogueTreeDto>>(
            $"{DialogueBase}/by-speaker/{Uri.EscapeDataString(speakerTag)}")
               ?? new PagedResult<DialogueTreeDto>();
    }

    public async Task<DialogueTreeDto?> CreateAsync(DialogueTreeDto dto)
    {
        return await PostAsync<DialogueTreeDto>(DialogueBase, dto);
    }

    public async Task<DialogueTreeDto?> UpdateAsync(string dialogueTreeId, DialogueTreeDto dto)
    {
        return await PutAsync<DialogueTreeDto>(
            $"{DialogueBase}/{Uri.EscapeDataString(dialogueTreeId)}", dto);
    }

    public async Task DeleteAsync(string dialogueTreeId)
    {
        await DeleteRequestAsync($"{DialogueBase}/{Uri.EscapeDataString(dialogueTreeId)}");
    }
}

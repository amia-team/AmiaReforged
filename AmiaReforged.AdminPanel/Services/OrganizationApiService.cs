using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine organization API.
/// </summary>
public class OrganizationApiService : ApiServiceBase
{
    private const string OrganizationsBase = "/api/worldengine/organizations";

    public OrganizationApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    // ==================== Organization CRUD ====================

    public async Task<PagedResult<OrganizationDto>> GetAllAsync(
        string? search = null, string? type = null, int page = 1, int pageSize = 50)
    {
        string url = $"{OrganizationsBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrWhiteSpace(type))
            url += $"&type={Uri.EscapeDataString(type)}";

        return await GetAsync<PagedResult<OrganizationDto>>(url)
               ?? new PagedResult<OrganizationDto>();
    }

    public async Task<OrganizationDto?> GetByIdAsync(Guid id)
    {
        return await GetAsync<OrganizationDto>($"{OrganizationsBase}/{id}");
    }

    public async Task<OrganizationDto?> CreateAsync(CreateOrganizationRequestDto dto)
    {
        return await PostAsync<OrganizationDto>(OrganizationsBase, dto);
    }

    public async Task<OrganizationDto?> UpdateAsync(Guid id, object updateDto)
    {
        return await PutAsync<OrganizationDto>($"{OrganizationsBase}/{id}", updateDto);
    }

    public async Task DeleteAsync(Guid id)
    {
        await DeleteRequestAsync($"{OrganizationsBase}/{id}");
    }

    // ==================== Member Operations ====================

    public async Task<List<OrganizationMemberDto>> GetMembersAsync(Guid organizationId, bool activeOnly = true)
    {
        string url = $"{OrganizationsBase}/{organizationId}/members?activeOnly={activeOnly}";
        return await GetAsync<List<OrganizationMemberDto>>(url) ?? [];
    }

    public async Task<OrganizationMemberDto?> AddMemberAsync(Guid organizationId, AddMemberRequestDto dto)
    {
        return await PostAsync<OrganizationMemberDto>($"{OrganizationsBase}/{organizationId}/members", dto);
    }

    public async Task RemoveMemberAsync(Guid organizationId, Guid characterId)
    {
        await DeleteRequestAsync($"{OrganizationsBase}/{organizationId}/members/{characterId}");
    }
}

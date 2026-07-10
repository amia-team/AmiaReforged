using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine encounter API.
/// Endpoints are managed at runtime via <see cref="IWorldEngineEndpointService"/>
/// and persisted to JSON on disk.
/// </summary>
public class EncounterApiService : ApiServiceBase
{
    private const string ProfilesBase = "/api/worldengine/encounters/profiles";
    private const string GroupsBase = "/api/worldengine/encounters/groups";
    private const string EntriesBase = "/api/worldengine/encounters/entries";
    private const string ConditionsBase = "/api/worldengine/encounters/conditions";
    private const string BonusesBase = "/api/worldengine/encounters/bonuses";
    private const string MiniBossBase = "/api/worldengine/encounters/miniboss";
    private const string BossesBase = "/api/worldengine/encounters/bosses";
    private const string BossConditionsBase = "/api/worldengine/encounters/boss-conditions";
    private const string MutationsBase = "/api/worldengine/encounters/mutations";
    private const string MutationEffectsBase = "/api/worldengine/encounters/effects";
    private const string CacheBase = "/api/worldengine/encounters/cache";

    public EncounterApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    // ==================== Profiles ====================

    public async Task<List<SpawnProfileDto>> GetAllProfilesAsync()
    {
        return await GetAsync<List<SpawnProfileDto>>(ProfilesBase) ?? [];
    }

    public async Task<SpawnProfileDto?> GetProfileAsync(Guid id)
    {
        return await GetAsync<SpawnProfileDto>($"{ProfilesBase}/{id}");
    }

    public async Task<SpawnProfileDto?> GetProfileByAreaAsync(string areaResRef)
    {
        return await GetAsync<SpawnProfileDto>($"{ProfilesBase}/by-area/{areaResRef}");
    }

    public async Task<SpawnProfileDto?> CreateProfileAsync(CreateProfileRequest request)
    {
        return await PostAsync<SpawnProfileDto>(ProfilesBase, request);
    }

    public async Task<SpawnProfileDto?> UpdateProfileAsync(Guid id, UpdateProfileRequest request)
    {
        return await PutAsync<SpawnProfileDto>($"{ProfilesBase}/{id}", request);
    }

    public async Task DeleteProfileAsync(Guid id)
    {
        await DeleteRequestAsync($"{ProfilesBase}/{id}");
    }

    public async Task ActivateProfileAsync(Guid id)
    {
        await PostAsync<object>($"{ProfilesBase}/{id}/activate", null);
    }

    public async Task DeactivateProfileAsync(Guid id)
    {
        await PostAsync<object>($"{ProfilesBase}/{id}/deactivate", null);
    }

    // ==================== Groups ====================

    public async Task<SpawnGroupDto?> AddGroupAsync(Guid profileId, CreateGroupRequest request)
    {
        return await PostAsync<SpawnGroupDto>($"{ProfilesBase}/{profileId}/groups", request);
    }

    public async Task<SpawnGroupDto?> UpdateGroupAsync(Guid groupId, UpdateGroupRequest request)
    {
        return await PutAsync<SpawnGroupDto>($"{GroupsBase}/{groupId}", request);
    }

    public async Task DeleteGroupAsync(Guid groupId)
    {
        await DeleteRequestAsync($"{GroupsBase}/{groupId}");
    }

    // ==================== Group Mutation Overrides ====================

    public async Task<GroupMutationOverrideDto?> AddMutationOverrideAsync(Guid groupId, SetGroupMutationOverrideRequest request)
    {
        return await PostAsync<GroupMutationOverrideDto>($"{GroupsBase}/{groupId}/mutation-overrides", request);
    }

    public async Task<GroupMutationOverrideDto?> UpdateMutationOverrideAsync(Guid groupId, Guid overrideId, UpdateGroupMutationOverrideRequest request)
    {
        return await PutAsync<GroupMutationOverrideDto>($"{GroupsBase}/{groupId}/mutation-overrides/{overrideId}", request);
    }

    public async Task DeleteMutationOverrideAsync(Guid groupId, Guid overrideId)
    {
        await DeleteRequestAsync($"{GroupsBase}/{groupId}/mutation-overrides/{overrideId}");
    }

    // ==================== Entries ====================

    public async Task<SpawnEntryDto?> AddEntryAsync(Guid groupId, CreateEntryRequest request)
    {
        return await PostAsync<SpawnEntryDto>($"{GroupsBase}/{groupId}/entries", request);
    }

    public async Task<SpawnEntryDto?> UpdateEntryAsync(Guid entryId, UpdateEntryRequest request)
    {
        return await PutAsync<SpawnEntryDto>($"{EntriesBase}/{entryId}", request);
    }

    public async Task DeleteEntryAsync(Guid entryId)
    {
        await DeleteRequestAsync($"{EntriesBase}/{entryId}");
    }

    // ==================== Conditions ====================

    public async Task<SpawnConditionDto?> AddConditionAsync(Guid groupId, CreateConditionRequest request)
    {
        return await PostAsync<SpawnConditionDto>($"{GroupsBase}/{groupId}/conditions", request);
    }

    public async Task<SpawnConditionDto?> UpdateConditionAsync(Guid conditionId, UpdateConditionRequest request)
    {
        return await PutAsync<SpawnConditionDto>($"{ConditionsBase}/{conditionId}", request);
    }

    public async Task DeleteConditionAsync(Guid conditionId)
    {
        await DeleteRequestAsync($"{ConditionsBase}/{conditionId}");
    }

    // ==================== Bonuses ====================

    public async Task<SpawnBonusDto?> AddBonusAsync(Guid profileId, CreateBonusRequest request)
    {
        return await PostAsync<SpawnBonusDto>($"{ProfilesBase}/{profileId}/bonuses", request);
    }

    public async Task<SpawnBonusDto?> UpdateBonusAsync(Guid bonusId, UpdateBonusRequest request)
    {
        return await PutAsync<SpawnBonusDto>($"{BonusesBase}/{bonusId}", request);
    }

    public async Task DeleteBonusAsync(Guid bonusId)
    {
        await DeleteRequestAsync($"{BonusesBase}/{bonusId}");
    }

    // ==================== Mini-Boss ====================

    public async Task<MiniBossConfigDto?> CreateMiniBossAsync(Guid profileId, CreateMiniBossRequest request)
    {
        return await PostAsync<MiniBossConfigDto>($"{ProfilesBase}/{profileId}/miniboss", request);
    }

    public async Task<MiniBossConfigDto?> UpdateMiniBossAsync(Guid profileId, UpdateMiniBossRequest request)
    {
        return await PutAsync<MiniBossConfigDto>($"{ProfilesBase}/{profileId}/miniboss", request);
    }

    public async Task DeleteMiniBossAsync(Guid profileId)
    {
        await DeleteRequestAsync($"{ProfilesBase}/{profileId}/miniboss");
    }

    public async Task<SpawnBonusDto?> AddMiniBossBonusAsync(Guid miniBossId, CreateBonusRequest request)
    {
        return await PostAsync<SpawnBonusDto>($"{MiniBossBase}/{miniBossId}/bonuses", request);
    }

    // ==================== Boss Pool ====================

    public async Task<List<BossConfigDto>> GetBossConfigsAsync(Guid profileId)
    {
        return await GetAsync<List<BossConfigDto>>($"{ProfilesBase}/{profileId}/bosses") ?? [];
    }

    public async Task<BossConfigDto?> CreateBossConfigAsync(Guid profileId, CreateBossConfigRequest request)
    {
        return await PostAsync<BossConfigDto>($"{ProfilesBase}/{profileId}/bosses", request);
    }

    public async Task<BossConfigDto?> UpdateBossConfigAsync(Guid bossId, UpdateBossConfigRequest request)
    {
        return await PutAsync<BossConfigDto>($"{BossesBase}/{bossId}", request);
    }

    public async Task DeleteBossConfigAsync(Guid bossId)
    {
        await DeleteRequestAsync($"{BossesBase}/{bossId}");
    }

    public async Task<SpawnConditionDto?> AddBossConditionAsync(Guid bossId, CreateConditionRequest request)
    {
        return await PostAsync<SpawnConditionDto>($"{BossesBase}/{bossId}/conditions", request);
    }

    public async Task<SpawnConditionDto?> UpdateBossConditionAsync(Guid conditionId, UpdateConditionRequest request)
    {
        return await PutAsync<SpawnConditionDto>($"{BossConditionsBase}/{conditionId}", request);
    }

    public async Task DeleteBossConditionAsync(Guid conditionId)
    {
        await DeleteRequestAsync($"{BossConditionsBase}/{conditionId}");
    }

    public async Task<SpawnBonusDto?> AddBossBonusAsync(Guid bossId, CreateBonusRequest request)
    {
        return await PostAsync<SpawnBonusDto>($"{BossesBase}/{bossId}/bonuses", request);
    }

    // ==================== Mutations ====================

    public async Task<List<MutationTemplateDto>> GetAllMutationsAsync()
    {
        return await GetAsync<List<MutationTemplateDto>>(MutationsBase) ?? [];
    }

    public async Task<MutationTemplateDto?> CreateMutationAsync(CreateMutationRequest request)
    {
        return await PostAsync<MutationTemplateDto>(MutationsBase, request);
    }

    public async Task<MutationTemplateDto?> UpdateMutationAsync(Guid id, UpdateMutationRequest request)
    {
        return await PutAsync<MutationTemplateDto>($"{MutationsBase}/{id}", request);
    }

    public async Task DeleteMutationAsync(Guid id)
    {
        await DeleteRequestAsync($"{MutationsBase}/{id}");
    }

    public async Task<MutationEffectDto?> AddMutationEffectAsync(Guid templateId, CreateMutationEffectRequest request)
    {
        return await PostAsync<MutationEffectDto>($"{MutationsBase}/{templateId}/effects", request);
    }

    public async Task<MutationEffectDto?> UpdateMutationEffectAsync(Guid effectId, UpdateMutationEffectRequest request)
    {
        return await PutAsync<MutationEffectDto>($"{MutationEffectsBase}/{effectId}", request);
    }

    public async Task DeleteMutationEffectAsync(Guid effectId)
    {
        await DeleteRequestAsync($"{MutationEffectsBase}/{effectId}");
    }

    public async Task RefreshMutationCacheAsync()
    {
        await PostAsync<object>($"{MutationsBase}/cache/refresh", null);
    }

    // ==================== Cache ====================

    public async Task RefreshCacheAsync()
    {
        await PostAsync<object>($"{CacheBase}/refresh", null);
    }

    // ==================== Bulk Operations ====================

    public async Task BulkSetProfilesActiveAsync(List<Guid> ids, bool isActive)
    {
        await PostAsync<object>($"{ProfilesBase}/bulk-set-active", new BulkSetActiveRequest(ids, isActive));
    }

    public async Task BulkSetBonusesActiveAsync(List<Guid> ids, bool isActive)
    {
        await PostAsync<object>($"{BonusesBase}/bulk-set-active", new BulkSetActiveRequest(ids, isActive));
    }

    public async Task BulkSetMutationsActiveAsync(List<Guid> ids, bool isActive)
    {
        await PostAsync<object>($"{MutationsBase}/bulk-set-active", new BulkSetActiveRequest(ids, isActive));
    }
}

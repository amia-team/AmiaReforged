using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.Encounters.Services;
using AmiaReforged.PwEngine.Features.WorldEngine.API;
using static AmiaReforged.PwEngine.Features.Encounters.API.EncounterApiDtos;

namespace AmiaReforged.PwEngine.Features.Encounters.API;

/// <summary>
/// HTTP API controller for managing spawn profiles, groups, and bonuses.
/// Uses the WorldEngine HTTP server's auto-discovery via [HttpGet]/[HttpPost]/etc. attributes.
///
/// Because the route table instantiates controllers via <c>Activator.CreateInstance</c>,
/// this controller uses a static service reference set during initialization by
/// <see cref="EncounterApiBootstrap"/>.
/// </summary>
public class SpawnProfileController
{
    // Static references set by EncounterApiBootstrap at startup
    internal static ISpawnProfileRepository? Repository;
    internal static DynamicEncounterService? EncounterService;

    // ==================== Profile CRUD ====================

    /// <summary>
    /// GET /api/worldengine/encounters/profiles — List all profiles
    /// </summary>
    [HttpGet("/api/worldengine/encounters/profiles")]
    public static async Task<ApiResult> ListProfiles(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        List<SpawnProfile> profiles = await Repository.GetAllAsync();
        return new ApiResult(200, profiles.Select(ToDto).ToList());
    }

    /// <summary>
    /// GET /api/worldengine/encounters/profiles/by-area/{areaResRef} — Get profile by area resref
    /// </summary>
    [HttpGet("/api/worldengine/encounters/profiles/by-area/{areaResRef}")]
    public static async Task<ApiResult> GetProfileByArea(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        string areaResRef = ctx.GetRouteValue("areaResRef");
        SpawnProfile? profile = await Repository.GetByAreaResRefAsync(areaResRef);

        return profile != null
            ? new ApiResult(200, ToDto(profile))
            : new ApiResult(404, new ErrorResponse("Not found", $"No profile for area '{areaResRef}'."));
    }

    /// <summary>
    /// GET /api/worldengine/encounters/profiles/{id} — Get profile by ID
    /// </summary>
    [HttpGet("/api/worldengine/encounters/profiles/{id}")]
    public static async Task<ApiResult> GetProfileById(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid profile ID."));

        SpawnProfile? profile = await Repository.GetByIdAsync(id);
        return profile != null
            ? new ApiResult(200, ToDto(profile))
            : new ApiResult(404, new ErrorResponse("Not found", $"Profile {id} not found."));
    }

    /// <summary>
    /// POST /api/worldengine/encounters/profiles — Create a new profile
    /// </summary>
    [HttpPost("/api/worldengine/encounters/profiles")]
    public static async Task<ApiResult> CreateProfile(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        CreateProfileRequest? req = await ctx.ReadJsonBodyAsync<CreateProfileRequest>();
        if (req == null || string.IsNullOrWhiteSpace(req.AreaResRef) || string.IsNullOrWhiteSpace(req.Name))
            return new ApiResult(400, new ErrorResponse("Bad request", "AreaResRef and Name are required."));

        if (await Repository.ExistsForAreaAsync(req.AreaResRef))
            return new ApiResult(409, new ErrorResponse("Conflict", $"Profile already exists for area '{req.AreaResRef}'."));

        SpawnProfile profile = new()
        {
            Id = Guid.NewGuid(),
            AreaResRef = req.AreaResRef,
            Name = req.Name,
            IsActive = req.IsActive,
            CooldownSeconds = req.CooldownSeconds,
            DespawnSeconds = req.DespawnSeconds
        };

        await Repository.CreateAsync(profile);

        if (EncounterService != null)
            await EncounterService.RefreshProfileCacheAsync(profile.AreaResRef);

        return new ApiResult(201, ToDto(profile));
    }

    /// <summary>
    /// PUT /api/worldengine/encounters/profiles/{id} — Update a profile
    /// </summary>
    [HttpPut("/api/worldengine/encounters/profiles/{id}")]
    public static async Task<ApiResult> UpdateProfile(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid profile ID."));

        SpawnProfile? profile = await Repository.GetByIdAsync(id);
        if (profile == null)
            return new ApiResult(404, new ErrorResponse("Not found", $"Profile {id} not found."));

        UpdateProfileRequest? req = await ctx.ReadJsonBodyAsync<UpdateProfileRequest>();
        if (req == null) return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required."));

        if (req.Name != null) profile.Name = req.Name;
        if (req.IsActive.HasValue) profile.IsActive = req.IsActive.Value;
        if (req.CooldownSeconds.HasValue) profile.CooldownSeconds = req.CooldownSeconds.Value;
        if (req.DespawnSeconds.HasValue) profile.DespawnSeconds = req.DespawnSeconds.Value;

        await Repository.UpdateAsync(profile);

        if (EncounterService != null)
            await EncounterService.RefreshProfileCacheAsync(profile.AreaResRef);

        return new ApiResult(200, ToDto(profile));
    }

    /// <summary>
    /// DELETE /api/worldengine/encounters/profiles/{id} — Delete a profile
    /// </summary>
    [HttpDelete("/api/worldengine/encounters/profiles/{id}")]
    public static async Task<ApiResult> DeleteProfile(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid profile ID."));

        SpawnProfile? profile = await Repository.GetByIdAsync(id);
        if (profile == null)
            return new ApiResult(404, new ErrorResponse("Not found", $"Profile {id} not found."));

        string areaResRef = profile.AreaResRef;
        await Repository.DeleteAsync(id);

        if (EncounterService != null)
            await EncounterService.RefreshProfileCacheAsync(areaResRef);

        return new ApiResult(200, new { message = "Profile deleted.", id });
    }

    /// <summary>
    /// POST /api/worldengine/encounters/profiles/{id}/activate — Activate a profile
    /// </summary>
    [HttpPost("/api/worldengine/encounters/profiles/{id}/activate")]
    public static async Task<ApiResult> ActivateProfile(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid profile ID."));

        SpawnProfile? profile = await Repository.GetByIdAsync(id);
        if (profile == null)
            return new ApiResult(404, new ErrorResponse("Not found", $"Profile {id} not found."));

        await Repository.SetActiveAsync(id, true);

        if (EncounterService != null)
            await EncounterService.RefreshProfileCacheAsync(profile.AreaResRef);

        return new ApiResult(200, new { message = "Profile activated.", id });
    }

    /// <summary>
    /// POST /api/worldengine/encounters/profiles/{id}/deactivate — Deactivate a profile
    /// </summary>
    [HttpPost("/api/worldengine/encounters/profiles/{id}/deactivate")]
    public static async Task<ApiResult> DeactivateProfile(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid profile ID."));

        SpawnProfile? profile = await Repository.GetByIdAsync(id);
        if (profile == null)
            return new ApiResult(404, new ErrorResponse("Not found", $"Profile {id} not found."));

        await Repository.SetActiveAsync(id, false);

        if (EncounterService != null)
            await EncounterService.RefreshProfileCacheAsync(profile.AreaResRef);

        return new ApiResult(200, new { message = "Profile deactivated.", id });
    }

    // ==================== Group CRUD ====================

    /// <summary>
    /// GET /api/worldengine/encounters/profiles/{id}/groups — List groups for a profile
    /// </summary>
    [HttpGet("/api/worldengine/encounters/profiles/{id}/groups")]
    public static async Task<ApiResult> ListGroups(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid profile ID."));

        SpawnProfile? profile = await Repository.GetByIdAsync(id);
        if (profile == null)
            return new ApiResult(404, new ErrorResponse("Not found", $"Profile {id} not found."));

        return new ApiResult(200, profile.SpawnGroups.Select(ToDto).ToList());
    }

    /// <summary>
    /// POST /api/worldengine/encounters/profiles/{id}/groups — Add a group to a profile
    /// </summary>
    [HttpPost("/api/worldengine/encounters/profiles/{id}/groups")]
    public static async Task<ApiResult> AddGroup(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid profile ID."));

        CreateGroupRequest? req = await ctx.ReadJsonBodyAsync<CreateGroupRequest>();
        if (req == null || string.IsNullOrWhiteSpace(req.Name))
            return new ApiResult(400, new ErrorResponse("Bad request", "Group name is required."));

        SpawnGroup group = new()
        {
            Id = Guid.NewGuid(),
            SpawnProfileId = id,
            Name = req.Name,
            Weight = req.Weight,
            Conditions = req.Conditions?.Select(c => new SpawnCondition
            {
                Id = Guid.NewGuid(),
                Type = c.Type,
                Operator = c.Operator,
                Value = c.Value
            }).ToList() ?? [],
            Entries = req.Entries?.Select(e => new SpawnEntry
            {
                Id = Guid.NewGuid(),
                CreatureResRef = e.CreatureResRef,
                RelativeWeight = e.RelativeWeight,
                MinCount = e.MinCount,
                MaxCount = e.MaxCount
            }).ToList() ?? []
        };

        await Repository.AddGroupAsync(id, group);

        SpawnProfile? profile = await Repository.GetByIdAsync(id);
        if (profile != null && EncounterService != null)
            await EncounterService.RefreshProfileCacheAsync(profile.AreaResRef);

        return new ApiResult(201, ToDto(group));
    }

    /// <summary>
    /// PUT /api/worldengine/encounters/groups/{groupId} — Update a group
    /// </summary>
    [HttpPut("/api/worldengine/encounters/groups/{groupId}")]
    public static async Task<ApiResult> UpdateGroup(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("groupId"), out Guid groupId))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid group ID."));

        SpawnGroup? group = await Repository.GetGroupByIdAsync(groupId);
        if (group == null)
            return new ApiResult(404, new ErrorResponse("Not found", $"Group {groupId} not found."));

        UpdateGroupRequest? req = await ctx.ReadJsonBodyAsync<UpdateGroupRequest>();
        if (req == null) return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required."));

        if (req.Name != null) group.Name = req.Name;
        if (req.Weight.HasValue) group.Weight = req.Weight.Value;

        await Repository.UpdateGroupAsync(group);
        return new ApiResult(200, ToDto(group));
    }

    /// <summary>
    /// DELETE /api/worldengine/encounters/groups/{groupId} — Delete a group
    /// </summary>
    [HttpDelete("/api/worldengine/encounters/groups/{groupId}")]
    public static async Task<ApiResult> DeleteGroup(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("groupId"), out Guid groupId))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid group ID."));

        await Repository.DeleteGroupAsync(groupId);
        return new ApiResult(200, new { message = "Group deleted.", groupId });
    }

    // ==================== Entry CRUD ====================

    /// <summary>
    /// POST /api/worldengine/encounters/groups/{groupId}/entries — Add an entry to a group
    /// </summary>
    [HttpPost("/api/worldengine/encounters/groups/{groupId}/entries")]
    public static async Task<ApiResult> AddEntry(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("groupId"), out Guid groupId))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid group ID."));

        SpawnGroup? group = await Repository.GetGroupByIdAsync(groupId);
        if (group == null)
            return new ApiResult(404, new ErrorResponse("Not found", $"Group {groupId} not found."));

        CreateEntryRequest? req = await ctx.ReadJsonBodyAsync<CreateEntryRequest>();
        if (req == null || string.IsNullOrWhiteSpace(req.CreatureResRef))
            return new ApiResult(400, new ErrorResponse("Bad request", "CreatureResRef is required."));

        SpawnEntry entry = new()
        {
            Id = Guid.NewGuid(),
            SpawnGroupId = groupId,
            CreatureResRef = req.CreatureResRef,
            RelativeWeight = req.RelativeWeight,
            MinCount = req.MinCount,
            MaxCount = req.MaxCount
        };

        await Repository.AddEntryAsync(groupId, entry);
        return new ApiResult(201, ToDto(entry));
    }

    /// <summary>
    /// PUT /api/worldengine/encounters/entries/{entryId} — Update an entry
    /// </summary>
    [HttpPut("/api/worldengine/encounters/entries/{entryId}")]
    public static async Task<ApiResult> UpdateEntry(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("entryId"), out Guid entryId))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid entry ID."));

        SpawnEntry? entry = await Repository.GetEntryByIdAsync(entryId);
        if (entry == null)
            return new ApiResult(404, new ErrorResponse("Not found", $"Entry {entryId} not found."));

        UpdateEntryRequest? req = await ctx.ReadJsonBodyAsync<UpdateEntryRequest>();
        if (req == null) return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required."));

        if (req.CreatureResRef != null) entry.CreatureResRef = req.CreatureResRef;
        if (req.RelativeWeight.HasValue) entry.RelativeWeight = req.RelativeWeight.Value;
        if (req.MinCount.HasValue) entry.MinCount = req.MinCount.Value;
        if (req.MaxCount.HasValue) entry.MaxCount = req.MaxCount.Value;

        await Repository.UpdateEntryAsync(entry);
        return new ApiResult(200, ToDto(entry));
    }

    /// <summary>
    /// DELETE /api/worldengine/encounters/entries/{entryId} — Delete an entry
    /// </summary>
    [HttpDelete("/api/worldengine/encounters/entries/{entryId}")]
    public static async Task<ApiResult> DeleteEntry(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("entryId"), out Guid entryId))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid entry ID."));

        await Repository.DeleteEntryAsync(entryId);
        return new ApiResult(200, new { message = "Entry deleted.", entryId });
    }

    // ==================== Condition CRUD ====================

    /// <summary>
    /// POST /api/worldengine/encounters/groups/{groupId}/conditions — Add a condition to a group
    /// </summary>
    [HttpPost("/api/worldengine/encounters/groups/{groupId}/conditions")]
    public static async Task<ApiResult> AddCondition(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("groupId"), out Guid groupId))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid group ID."));

        SpawnGroup? group = await Repository.GetGroupByIdAsync(groupId);
        if (group == null)
            return new ApiResult(404, new ErrorResponse("Not found", $"Group {groupId} not found."));

        CreateConditionRequest? req = await ctx.ReadJsonBodyAsync<CreateConditionRequest>();
        if (req == null || string.IsNullOrWhiteSpace(req.Operator))
            return new ApiResult(400, new ErrorResponse("Bad request", "Operator is required."));

        SpawnCondition condition = new()
        {
            Id = Guid.NewGuid(),
            SpawnGroupId = groupId,
            Type = req.Type,
            Operator = req.Operator,
            Value = req.Value
        };

        await Repository.AddConditionAsync(groupId, condition);
        return new ApiResult(201, ToDto(condition));
    }

    /// <summary>
    /// PUT /api/worldengine/encounters/conditions/{conditionId} — Update a condition
    /// </summary>
    [HttpPut("/api/worldengine/encounters/conditions/{conditionId}")]
    public static async Task<ApiResult> UpdateCondition(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("conditionId"), out Guid conditionId))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid condition ID."));

        SpawnCondition? condition = await Repository.GetConditionByIdAsync(conditionId);
        if (condition == null)
            return new ApiResult(404, new ErrorResponse("Not found", $"Condition {conditionId} not found."));

        UpdateConditionRequest? req = await ctx.ReadJsonBodyAsync<UpdateConditionRequest>();
        if (req == null) return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required."));

        if (req.Type.HasValue) condition.Type = req.Type.Value;
        if (req.Operator != null) condition.Operator = req.Operator;
        if (req.Value != null) condition.Value = req.Value;

        await Repository.UpdateConditionAsync(condition);
        return new ApiResult(200, ToDto(condition));
    }

    /// <summary>
    /// DELETE /api/worldengine/encounters/conditions/{conditionId} — Delete a condition
    /// </summary>
    [HttpDelete("/api/worldengine/encounters/conditions/{conditionId}")]
    public static async Task<ApiResult> DeleteCondition(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("conditionId"), out Guid conditionId))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid condition ID."));

        await Repository.DeleteConditionAsync(conditionId);
        return new ApiResult(200, new { message = "Condition deleted.", conditionId });
    }

    // ==================== Bonus CRUD ====================

    /// <summary>
    /// GET /api/worldengine/encounters/profiles/{id}/bonuses — List bonuses for a profile
    /// </summary>
    [HttpGet("/api/worldengine/encounters/profiles/{id}/bonuses")]
    public static async Task<ApiResult> ListBonuses(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid profile ID."));

        SpawnProfile? profile = await Repository.GetByIdAsync(id);
        if (profile == null)
            return new ApiResult(404, new ErrorResponse("Not found", $"Profile {id} not found."));

        return new ApiResult(200, profile.Bonuses.Select(ToDto).ToList());
    }

    /// <summary>
    /// POST /api/worldengine/encounters/profiles/{id}/bonuses — Add a bonus to a profile
    /// </summary>
    [HttpPost("/api/worldengine/encounters/profiles/{id}/bonuses")]
    public static async Task<ApiResult> AddBonus(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid profile ID."));

        CreateBonusRequest? req = await ctx.ReadJsonBodyAsync<CreateBonusRequest>();
        if (req == null || string.IsNullOrWhiteSpace(req.Name))
            return new ApiResult(400, new ErrorResponse("Bad request", "Bonus name is required."));

        SpawnBonus bonus = new()
        {
            Id = Guid.NewGuid(),
            SpawnProfileId = id,
            Name = req.Name,
            Type = req.Type,
            Value = req.Value,
            DurationSeconds = req.DurationSeconds,
            IsActive = req.IsActive
        };

        await Repository.AddBonusAsync(id, bonus);

        SpawnProfile? profile = await Repository.GetByIdAsync(id);
        if (profile != null && EncounterService != null)
            await EncounterService.RefreshProfileCacheAsync(profile.AreaResRef);

        return new ApiResult(201, ToDto(bonus));
    }

    /// <summary>
    /// PUT /api/worldengine/encounters/bonuses/{bonusId} — Update a bonus
    /// </summary>
    [HttpPut("/api/worldengine/encounters/bonuses/{bonusId}")]
    public static async Task<ApiResult> UpdateBonus(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("bonusId"), out Guid bonusId))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid bonus ID."));

        SpawnBonus? bonus = await Repository.GetBonusByIdAsync(bonusId);
        if (bonus == null)
            return new ApiResult(404, new ErrorResponse("Not found", $"Bonus {bonusId} not found."));

        UpdateBonusRequest? req = await ctx.ReadJsonBodyAsync<UpdateBonusRequest>();
        if (req == null) return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required."));

        if (req.Name != null) bonus.Name = req.Name;
        if (req.Type.HasValue) bonus.Type = req.Type.Value;
        if (req.Value.HasValue) bonus.Value = req.Value.Value;
        if (req.DurationSeconds.HasValue) bonus.DurationSeconds = req.DurationSeconds.Value;
        if (req.IsActive.HasValue) bonus.IsActive = req.IsActive.Value;

        await Repository.UpdateBonusAsync(bonus);
        return new ApiResult(200, ToDto(bonus));
    }

    /// <summary>
    /// DELETE /api/worldengine/encounters/bonuses/{bonusId} — Delete a bonus
    /// </summary>
    [HttpDelete("/api/worldengine/encounters/bonuses/{bonusId}")]
    public static async Task<ApiResult> DeleteBonus(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("bonusId"), out Guid bonusId))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid bonus ID."));

        await Repository.DeleteBonusAsync(bonusId);
        return new ApiResult(200, new { message = "Bonus deleted.", bonusId });
    }

    // ==================== Cache Management ====================

    /// <summary>
    /// POST /api/worldengine/encounters/cache/refresh — Force-refresh the encounter profile cache
    /// </summary>
    [HttpPost("/api/worldengine/encounters/cache/refresh")]
    public static async Task<ApiResult> RefreshCache(RouteContext ctx)
    {
        if (EncounterService == null) return ServiceUnavailable();

        await EncounterService.RefreshAllProfileCacheAsync();
        return new ApiResult(200, new { message = "Encounter profile cache refreshed." });
    }

    private static ApiResult ServiceUnavailable() =>
        new(503, new ErrorResponse("Service unavailable", "Encounter system is not initialized."));
}

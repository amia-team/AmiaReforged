using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.Encounters.Services;
using AmiaReforged.PwEngine.Features.WorldEngine.API;
using static AmiaReforged.PwEngine.Features.Encounters.API.EncounterApiDtos;

namespace AmiaReforged.PwEngine.Features.Encounters.API;

/// <summary>
/// HTTP API controller for managing global mutation templates and their effects.
/// Uses the WorldEngine HTTP server's auto-discovery via [HttpGet]/[HttpPost]/etc. attributes.
/// </summary>
public class MutationController
{
    // Static references set by EncounterApiBootstrap at startup
    internal static IMutationRepository? Repository;
    internal static MutationApplicator? Applicator;

    // ==================== Template CRUD ====================

    /// <summary>
    /// GET /api/worldengine/encounters/mutations — List all mutation templates
    /// </summary>
    [HttpGet("/api/worldengine/encounters/mutations")]
    public static async Task<ApiResult> GetAll(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        List<MutationTemplate> templates = await Repository.GetAllAsync();
        return new ApiResult(200, templates.Select(ToMutationDto).ToList());
    }

    /// <summary>
    /// GET /api/worldengine/encounters/mutations/{id} — Get a single mutation template
    /// </summary>
    [HttpGet("/api/worldengine/encounters/mutations/{id}")]
    public static async Task<ApiResult> GetById(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid mutation ID."));

        MutationTemplate? template = await Repository.GetByIdAsync(id);
        return template != null
            ? new ApiResult(200, ToMutationDto(template))
            : new ApiResult(404, new ErrorResponse("Not found", $"Mutation {id} not found."));
    }

    /// <summary>
    /// POST /api/worldengine/encounters/mutations — Create a mutation template
    /// </summary>
    [HttpPost("/api/worldengine/encounters/mutations")]
    public static async Task<ApiResult> Create(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        CreateMutationRequest? req = await ctx.ReadJsonBodyAsync<CreateMutationRequest>();
        if (req == null || string.IsNullOrWhiteSpace(req.Prefix))
            return new ApiResult(400, new ErrorResponse("Bad request", "Prefix is required."));

        MutationTemplate template = new()
        {
            Id = Guid.NewGuid(),
            Prefix = req.Prefix.Trim(),
            Description = req.Description?.Trim(),
            SpawnChancePercent = Math.Clamp(req.SpawnChancePercent, 0, 100),
            IsActive = req.IsActive
        };

        await Repository.CreateAsync(template);
        if (Applicator != null) await Applicator.RefreshCacheAsync();

        return new ApiResult(201, ToMutationDto(template));
    }

    /// <summary>
    /// PUT /api/worldengine/encounters/mutations/{id} — Update a mutation template
    /// </summary>
    [HttpPut("/api/worldengine/encounters/mutations/{id}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid mutation ID."));

        MutationTemplate? template = await Repository.GetByIdAsync(id);
        if (template == null)
            return new ApiResult(404, new ErrorResponse("Not found", $"Mutation {id} not found."));

        UpdateMutationRequest? req = await ctx.ReadJsonBodyAsync<UpdateMutationRequest>();
        if (req == null) return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required."));

        if (req.Prefix != null) template.Prefix = req.Prefix.Trim();
        if (req.Description != null) template.Description = req.Description.Trim();
        if (req.SpawnChancePercent.HasValue) template.SpawnChancePercent = Math.Clamp(req.SpawnChancePercent.Value, 0, 100);
        if (req.IsActive.HasValue) template.IsActive = req.IsActive.Value;

        await Repository.UpdateAsync(template);
        if (Applicator != null) await Applicator.RefreshCacheAsync();

        return new ApiResult(200, ToMutationDto(template));
    }

    /// <summary>
    /// DELETE /api/worldengine/encounters/mutations/{id} — Delete a mutation template
    /// </summary>
    [HttpDelete("/api/worldengine/encounters/mutations/{id}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid mutation ID."));

        await Repository.DeleteAsync(id);
        if (Applicator != null) await Applicator.RefreshCacheAsync();

        return new ApiResult(200, new { message = "Mutation template deleted.", id });
    }

    // ==================== Effect CRUD ====================

    /// <summary>
    /// POST /api/worldengine/encounters/mutations/{id}/effects — Add an effect to a template
    /// </summary>
    [HttpPost("/api/worldengine/encounters/mutations/{id}/effects")]
    public static async Task<ApiResult> AddEffect(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("id"), out Guid id))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid mutation ID."));

        CreateMutationEffectRequest? req = await ctx.ReadJsonBodyAsync<CreateMutationEffectRequest>();
        if (req == null)
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required."));

        MutationEffect effect = new()
        {
            Id = Guid.NewGuid(),
            MutationTemplateId = id,
            Type = req.Type,
            Value = req.Value,
            AbilityType = req.AbilityType,
            DamageType = req.DamageType,
            DurationSeconds = req.DurationSeconds,
            IsActive = req.IsActive
        };

        await Repository.AddEffectAsync(id, effect);
        if (Applicator != null) await Applicator.RefreshCacheAsync();

        return new ApiResult(201, ToMutationEffectDto(effect));
    }

    /// <summary>
    /// PUT /api/worldengine/encounters/effects/{effectId} — Update an effect
    /// </summary>
    [HttpPut("/api/worldengine/encounters/effects/{effectId}")]
    public static async Task<ApiResult> UpdateEffect(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("effectId"), out Guid effectId))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid effect ID."));

        MutationEffect? effect = await Repository.GetEffectByIdAsync(effectId);
        if (effect == null)
            return new ApiResult(404, new ErrorResponse("Not found", $"Effect {effectId} not found."));

        UpdateMutationEffectRequest? req = await ctx.ReadJsonBodyAsync<UpdateMutationEffectRequest>();
        if (req == null) return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required."));

        if (req.Type.HasValue) effect.Type = req.Type.Value;
        if (req.Value.HasValue) effect.Value = req.Value.Value;
        if (req.AbilityType.HasValue) effect.AbilityType = req.AbilityType.Value;
        if (req.DamageType.HasValue) effect.DamageType = req.DamageType.Value;
        if (req.DurationSeconds.HasValue) effect.DurationSeconds = req.DurationSeconds.Value;
        if (req.IsActive.HasValue) effect.IsActive = req.IsActive.Value;

        await Repository.UpdateEffectAsync(effect);
        if (Applicator != null) await Applicator.RefreshCacheAsync();

        return new ApiResult(200, ToMutationEffectDto(effect));
    }

    /// <summary>
    /// DELETE /api/worldengine/encounters/effects/{effectId} — Delete an effect
    /// </summary>
    [HttpDelete("/api/worldengine/encounters/effects/{effectId}")]
    public static async Task<ApiResult> DeleteEffect(RouteContext ctx)
    {
        if (Repository == null) return ServiceUnavailable();

        if (!Guid.TryParse(ctx.GetRouteValue("effectId"), out Guid effectId))
            return new ApiResult(400, new ErrorResponse("Bad request", "Invalid effect ID."));

        await Repository.DeleteEffectAsync(effectId);
        if (Applicator != null) await Applicator.RefreshCacheAsync();

        return new ApiResult(200, new { message = "Mutation effect deleted.", effectId });
    }

    // ==================== Cache ====================

    /// <summary>
    /// POST /api/worldengine/encounters/mutations/cache/refresh — Refresh mutation cache
    /// </summary>
    [HttpPost("/api/worldengine/encounters/mutations/cache/refresh")]
    public static async Task<ApiResult> RefreshCache(RouteContext ctx)
    {
        if (Applicator == null) return ServiceUnavailable();

        await Applicator.RefreshCacheAsync();
        return new ApiResult(200, new { message = "Mutation template cache refreshed." });
    }

    private static ApiResult ServiceUnavailable() =>
        new(503, new ErrorResponse("Service unavailable", "Mutation system is not initialized."));
}

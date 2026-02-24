using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.Encounters.Models;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.Encounters.Services;

[ServiceBinding(typeof(ISpawnProfileRepository))]
public class SpawnProfileRepository : ISpawnProfileRepository
{
    private readonly IDbContextFactory<PwEngineContext> _factory;

    public SpawnProfileRepository(IDbContextFactory<PwEngineContext> factory)
    {
        _factory = factory;
    }

    public async Task<SpawnProfile?> GetByAreaResRefAsync(string areaResRef)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        return await FullQuery(ctx).FirstOrDefaultAsync(p => p.AreaResRef == areaResRef);
    }

    public async Task<SpawnProfile?> GetByIdAsync(Guid id)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        return await FullQuery(ctx).FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<SpawnProfile>> GetAllActiveAsync()
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        return await FullQuery(ctx).Where(p => p.IsActive).ToListAsync();
    }

    public async Task<List<SpawnProfile>> GetAllAsync()
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        return await FullQuery(ctx).ToListAsync();
    }

    public async Task<bool> ExistsForAreaAsync(string areaResRef)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        return await ctx.SpawnProfiles.AnyAsync(p => p.AreaResRef == areaResRef);
    }

    public async Task<SpawnProfile> CreateAsync(SpawnProfile profile)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        profile.CreatedAt = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;
        ctx.SpawnProfiles.Add(profile);
        await ctx.SaveChangesAsync();
        return profile;
    }

    public async Task<SpawnProfile> UpdateAsync(SpawnProfile profile)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        profile.UpdatedAt = DateTime.UtcNow;
        ctx.SpawnProfiles.Update(profile);
        await ctx.SaveChangesAsync();
        return profile;
    }

    public async Task DeleteAsync(Guid id)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        SpawnProfile? profile = await ctx.SpawnProfiles.FindAsync(id);
        if (profile != null)
        {
            ctx.SpawnProfiles.Remove(profile);
            await ctx.SaveChangesAsync();
        }
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        SpawnProfile? profile = await ctx.SpawnProfiles.FindAsync(id);
        if (profile != null)
        {
            profile.IsActive = isActive;
            profile.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }

    // === Spawn Group Operations ===

    public async Task<SpawnGroup?> GetGroupByIdAsync(Guid groupId)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        return await ctx.SpawnGroups
            .Include(g => g.Conditions)
            .Include(g => g.Entries)
            .FirstOrDefaultAsync(g => g.Id == groupId);
    }

    public async Task<SpawnGroup> AddGroupAsync(Guid profileId, SpawnGroup group)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        group.SpawnProfileId = profileId;
        ctx.SpawnGroups.Add(group);
        await ctx.SaveChangesAsync();
        return group;
    }

    public async Task<SpawnGroup> UpdateGroupAsync(SpawnGroup group)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        ctx.SpawnGroups.Update(group);
        await ctx.SaveChangesAsync();
        return group;
    }

    public async Task DeleteGroupAsync(Guid groupId)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        SpawnGroup? group = await ctx.SpawnGroups.FindAsync(groupId);
        if (group != null)
        {
            ctx.SpawnGroups.Remove(group);
            await ctx.SaveChangesAsync();
        }
    }

    // === Spawn Entry Operations ===

    public async Task<SpawnEntry?> GetEntryByIdAsync(Guid entryId)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        return await ctx.SpawnEntries.FindAsync(entryId);
    }

    public async Task<SpawnEntry> AddEntryAsync(Guid groupId, SpawnEntry entry)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        entry.SpawnGroupId = groupId;
        ctx.SpawnEntries.Add(entry);
        await ctx.SaveChangesAsync();
        return entry;
    }

    public async Task<SpawnEntry> UpdateEntryAsync(SpawnEntry entry)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        ctx.SpawnEntries.Update(entry);
        await ctx.SaveChangesAsync();
        return entry;
    }

    public async Task DeleteEntryAsync(Guid entryId)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        SpawnEntry? entry = await ctx.SpawnEntries.FindAsync(entryId);
        if (entry != null)
        {
            ctx.SpawnEntries.Remove(entry);
            await ctx.SaveChangesAsync();
        }
    }

    // === Spawn Condition Operations ===

    public async Task<SpawnCondition?> GetConditionByIdAsync(Guid conditionId)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        return await ctx.SpawnConditions.FindAsync(conditionId);
    }

    public async Task<SpawnCondition> AddConditionAsync(Guid groupId, SpawnCondition condition)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        condition.SpawnGroupId = groupId;
        ctx.SpawnConditions.Add(condition);
        await ctx.SaveChangesAsync();
        return condition;
    }

    public async Task<SpawnCondition> UpdateConditionAsync(SpawnCondition condition)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        ctx.SpawnConditions.Update(condition);
        await ctx.SaveChangesAsync();
        return condition;
    }

    public async Task DeleteConditionAsync(Guid conditionId)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        SpawnCondition? condition = await ctx.SpawnConditions.FindAsync(conditionId);
        if (condition != null)
        {
            ctx.SpawnConditions.Remove(condition);
            await ctx.SaveChangesAsync();
        }
    }

    // === Spawn Bonus Operations ===

    public async Task<SpawnBonus?> GetBonusByIdAsync(Guid bonusId)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        return await ctx.SpawnBonuses.FindAsync(bonusId);
    }

    public async Task<SpawnBonus> AddBonusAsync(Guid profileId, SpawnBonus bonus)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        bonus.SpawnProfileId = profileId;
        ctx.SpawnBonuses.Add(bonus);
        await ctx.SaveChangesAsync();
        return bonus;
    }

    public async Task<SpawnBonus> UpdateBonusAsync(SpawnBonus bonus)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        ctx.SpawnBonuses.Update(bonus);
        await ctx.SaveChangesAsync();
        return bonus;
    }

    public async Task DeleteBonusAsync(Guid bonusId)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        SpawnBonus? bonus = await ctx.SpawnBonuses.FindAsync(bonusId);
        if (bonus != null)
        {
            ctx.SpawnBonuses.Remove(bonus);
            await ctx.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Builds a query that eagerly loads the full profile graph.
    /// </summary>
    private static IQueryable<SpawnProfile> FullQuery(PwEngineContext ctx)
    {
        return ctx.SpawnProfiles
            .Include(p => p.SpawnGroups)
                .ThenInclude(g => g.Conditions)
            .Include(p => p.SpawnGroups)
                .ThenInclude(g => g.Entries)
            .Include(p => p.Bonuses)
            .Include(p => p.MiniBoss)
                .ThenInclude(m => m!.Bonuses)
            .AsSplitQuery();
    }
}

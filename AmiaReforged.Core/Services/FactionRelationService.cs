using AmiaReforged.Core.Models;
using AmiaReforged.System.Helpers;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

// [ServiceBinding(typeof(FactionRelationService))]
public class FactionRelationService
{
    private readonly FactionService _factionService;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly AmiaContext _ctx;
    private readonly NwTaskHelper _taskHelper;

    public FactionRelationService(FactionService factionService, AmiaContext ctx, NwTaskHelper taskHelper)
    {
        _factionService = factionService;
        _ctx = ctx;
        _taskHelper = taskHelper;
    }

    private static void ReportNonExistentFactions(FactionRelation relation, Faction? faction, Faction? targetFaction)
    {
        if (faction is null)
        {
            Log.Error($"Faction {relation.FactionName} not found.");
        }

        if (targetFaction is null)
        {
            Log.Error($"Target Faction {relation.TargetFactionName} not found.");
        }
    }

    private async Task AddAsyncOnlyIfFactionsExist(FactionRelation relation, Faction? faction, Faction? targetFaction)
    {
        if (faction is not null && targetFaction is not null)
        {
            try
            {
                await _ctx.FactionRelations.AddAsync(relation);
                await _ctx.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while adding faction relation");
            }
        }
    }

    private async Task AddFactionRelation(FactionRelation relation)
    {
        Faction? faction = await _factionService.GetFactionByName(relation.FactionName);
        Faction? targetFaction = await _factionService.GetFactionByName(relation.TargetFactionName);

        ReportNonExistentFactions(relation, faction, targetFaction);

        await AddAsyncOnlyIfFactionsExist(relation, faction, targetFaction);

        await _taskHelper.TrySwitchToMainThread();
    }

    public async Task UpdateFactionRelation(FactionRelation newRelation)
    {
        try
        {
            _ctx.FactionRelations.Update(newRelation);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while updating faction relation");
        }

        await _taskHelper.TrySwitchToMainThread();
    }

    public async Task<FactionRelation?> GetFactionRelationAsync(Faction factionA, Faction factionB)
    {
        FactionRelation? relation = null;
        try
        {
            relation = await _ctx.FactionRelations
                .FirstOrDefaultAsync(f => f.FactionName == factionA.Name && f.TargetFactionName == factionB.Name);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while getting faction relation");
        }

        if (relation is null)
        {
            relation = new FactionRelation
            {
                FactionName = factionA.Name,
                TargetFactionName = factionB.Name,
                Relation = 0
            };

            await AddFactionRelation(relation);
        }

        await _taskHelper.TrySwitchToMainThread();
        return relation;
    }

    public async Task AddFactionCharacterRelation(FactionCharacterRelation relation)
    {
        // Add if relation does not exist (find by index)
        bool notExists =
            await _ctx.FactionCharacterRelations.FindAsync(relation.CharacterId, relation.FactionName) is null;
        if (notExists)
        {
            await TryAddFactionCharacterRelation(relation);
            await _taskHelper.TrySwitchToMainThread();
        }
    }

    private async Task TryAddFactionCharacterRelation(FactionCharacterRelation relation)
    {
        try
        {
            await _ctx.FactionCharacterRelations.AddAsync(relation);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while adding faction character relation");
        }
    }

    private async Task TryUpdateFactionCharacterRelation(FactionCharacterRelation relation)
    {
        FactionCharacterRelation? existingRelation =
            await _ctx.FactionCharacterRelations.FindAsync(relation.CharacterId, relation.FactionName);

        if (existingRelation is not null)
            existingRelation.Relation = relation.Relation;

        await _ctx.SaveChangesAsync();
    }

    public async Task<FactionCharacterRelation?> GetFactionCharacterRelation(string factionName, Guid characterId)
    {
        FactionCharacterRelation? relation = null;

        try
        {
            relation = await _ctx.FactionCharacterRelations
                .FirstOrDefaultAsync(f => f.FactionName == factionName && f.CharacterId == characterId);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while getting faction character relation");
        }

        // if relation does not exist, create it
        if (relation is null)
        {
            relation = new FactionCharacterRelation
            {
                FactionName = factionName,
                CharacterId = characterId,
                Relation = 0
            };

            await AddFactionCharacterRelation(relation);
        }

        await _taskHelper.TrySwitchToMainThread();
        return relation;
    }

    public async Task UpdateFactionCharacterRelation(FactionCharacterRelation relation)
    {
        await TryUpdateFactionCharacterRelation(relation);
        await _taskHelper.TrySwitchToMainThread();
    }
}
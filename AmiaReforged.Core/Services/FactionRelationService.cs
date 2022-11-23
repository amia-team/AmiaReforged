using AmiaReforged.Core.Models;
using AmiaReforged.System.Helpers;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(FactionRelationService))]
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

    public async Task AddFactionRelation(FactionRelation relation)
    {
        Faction? faction = await _factionService.GetFactionByName(relation.FactionName);
        Faction? targetFaction = await _factionService.GetFactionByName(relation.TargetFactionName);

        ReportNonExistentFactions(relation, faction, targetFaction);

        await AddAsyncOnlyIfFactionsExist(relation, faction, targetFaction);

        await _taskHelper.TrySwitchToMainThread();
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
}
using AmiaReforged.Core;
using AmiaReforged.Core.Models;
using AmiaReforged.System.Helpers;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Services;

// [ServiceBinding(typeof(FactionRelation))]
public class FactionRelationService
{
    private readonly FactionService _factionService;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly AmiaContext _ctx;
    private readonly NwTaskHelper _taskHelper;

    public FactionRelationService(FactionService factionService)
    {
        _factionService = factionService;
        _ctx = new AmiaContext();
        _taskHelper = new NwTaskHelper();
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

    public async Task<IEnumerable<FactionRelation>> GetRelationsForFaction(Faction faction)
    {
        IEnumerable<FactionRelation> relation =
            _ctx.FactionRelations.Where(f1 => f1.FactionName == faction.Name).AsEnumerable();
        await _taskHelper.TrySwitchToMainThread();
        return relation;
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
}
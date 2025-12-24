using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Admin;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.RebuildTool;

/// <summary>
/// Repository for managing character rebuilds and their associated item records.
/// </summary>
[ServiceBinding(typeof(IRebuildRepository))]
public class RebuildRepository(PwContextFactory factory) : IRebuildRepository
{
    private readonly PwEngineContext _ctx = factory.CreateDbContext();

    public CharacterRebuild? GetById(int id)
    {
        return _ctx.CharacterRebuilds.Find(id);
    }

    public IEnumerable<CharacterRebuild> GetByPlayerCdKey(string cdKey)
    {
        return _ctx.CharacterRebuilds
            .Where(r => r.PlayerCdKey == cdKey)
            .ToList();
    }

    public IEnumerable<CharacterRebuild> GetByCharacterId(Guid characterId)
    {
        return _ctx.CharacterRebuilds
            .Where(r => r.CharacterId == characterId)
            .ToList();
    }

    public IEnumerable<CharacterRebuild> GetPendingRebuilds()
    {
        return _ctx.CharacterRebuilds
            .Where(r => r.CompletedUtc == default)
            .ToList();
    }

    public CharacterRebuild? GetWithItems(int id)
    {
        return _ctx.CharacterRebuilds
            .Include(r => r.Player)
            .Include(r => r.Character)
            .Where(r => r.Id == id)
            .Select(r => new
            {
                Rebuild = r,
                Items = _ctx.RebuildItemRecords.Where(i => i.CharacterRebuildId == r.Id).ToList()
            })
            .AsEnumerable()
            .Select(x =>
            {
                // Attach items to the rebuild for convenient access
                return x.Rebuild;
            })
            .FirstOrDefault();
    }

    public void Add(CharacterRebuild rebuild)
    {
        _ctx.CharacterRebuilds.Add(rebuild);
    }

    public void Update(CharacterRebuild rebuild)
    {
        _ctx.CharacterRebuilds.Update(rebuild);
    }

    public void Delete(int id)
    {
        var rebuild = _ctx.CharacterRebuilds.Find(id);
        if (rebuild != null)
        {
            _ctx.CharacterRebuilds.Remove(rebuild);
        }
    }

    public void CompleteRebuild(int id)
    {
        var rebuild = _ctx.CharacterRebuilds.Find(id);
        if (rebuild != null)
        {
            rebuild.CompletedUtc = DateTime.UtcNow;
            _ctx.CharacterRebuilds.Update(rebuild);
        }
    }

    public IEnumerable<RebuildItemRecord> GetItemRecords(int rebuildId)
    {
        return _ctx.RebuildItemRecords
            .Where(r => r.CharacterRebuildId == rebuildId)
            .ToList();
    }

    public void AddItemRecord(RebuildItemRecord itemRecord)
    {
        _ctx.RebuildItemRecords.Add(itemRecord);
    }

    public void AddItemRecords(IEnumerable<RebuildItemRecord> itemRecords)
    {
        _ctx.RebuildItemRecords.AddRange(itemRecords);
    }

    public void DeleteItemRecord(long id)
    {
        var itemRecord = _ctx.RebuildItemRecords.Find(id);
        if (itemRecord != null)
        {
            _ctx.RebuildItemRecords.Remove(itemRecord);
        }
    }

    public void DeleteItemRecordsByRebuildId(int rebuildId)
    {
        var items = _ctx.RebuildItemRecords
            .Where(r => r.CharacterRebuildId == rebuildId)
            .ToList();
        _ctx.RebuildItemRecords.RemoveRange(items);
    }

    public void SaveChanges()
    {
        _ctx.SaveChanges();
    }
}


using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Persistence;

/// <summary>
/// Database-backed implementation of <see cref="IRegionRepository"/>.
/// Persists region definitions to PostgreSQL via EF Core.
/// Complex query methods load all regions and compute results —
/// for high-frequency runtime queries, the in-memory repo (populated via loading service) is preferred.
/// </summary>
[ServiceBinding(typeof(IRegionRepository))]
public class DbRegionRepository : IRegionRepository
{
    private readonly PwContextFactory _contextFactory;

    public DbRegionRepository(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public void Add(RegionDefinition definition)
    {
        using var ctx = _contextFactory.CreateDbContext();

        var existing = ctx.RegionDefinitions
            .FirstOrDefault(e => e.Tag == definition.Tag.Value);

        if (existing != null)
        {
            RegionMapper.UpdateEntity(existing, definition);
        }
        else
        {
            ctx.RegionDefinitions.Add(RegionMapper.ToEntity(definition));
        }

        ctx.SaveChanges();
    }

    public void Update(RegionDefinition definition)
    {
        using var ctx = _contextFactory.CreateDbContext();

        var existing = ctx.RegionDefinitions
            .FirstOrDefault(e => e.Tag == definition.Tag.Value);

        if (existing == null)
        {
            ctx.RegionDefinitions.Add(RegionMapper.ToEntity(definition));
        }
        else
        {
            RegionMapper.UpdateEntity(existing, definition);
        }

        ctx.SaveChanges();
    }

    public bool Exists(RegionTag tag)
    {
        using var ctx = _contextFactory.CreateDbContext();
        return ctx.RegionDefinitions.Any(e => e.Tag == tag.Value);
    }

    public List<RegionDefinition> All()
    {
        using var ctx = _contextFactory.CreateDbContext();

        return ctx.RegionDefinitions
            .AsNoTracking()
            .AsEnumerable()
            .Select(RegionMapper.ToDomain)
            .ToList();
    }

    public bool Delete(RegionTag tag)
    {
        using var ctx = _contextFactory.CreateDbContext();

        var entity = ctx.RegionDefinitions
            .FirstOrDefault(e => e.Tag == tag.Value);

        if (entity == null) return false;

        ctx.RegionDefinitions.Remove(entity);
        ctx.SaveChanges();
        return true;
    }

    public void Clear()
    {
        using var ctx = _contextFactory.CreateDbContext();
        ctx.RegionDefinitions.RemoveRange(ctx.RegionDefinitions);
        ctx.SaveChanges();
    }

    // ==================== Complex query methods ====================
    // These load all regions from DB and compute results.
    // For high-frequency runtime use, the InMemoryRegionRepository should be preferred.

    public bool TryGetRegionBySettlement(SettlementId settlementId, out RegionDefinition? region)
    {
        region = null;
        var all = All();

        foreach (var r in all)
        {
            if (r.Areas.Any(a => a.LinkedSettlement?.Value == settlementId.Value))
            {
                region = r;
                return true;
            }
        }

        return false;
    }

    public IReadOnlyCollection<SettlementId> GetSettlements(RegionTag regionTag)
    {
        using var ctx = _contextFactory.CreateDbContext();
        var entity = ctx.RegionDefinitions
            .AsNoTracking()
            .FirstOrDefault(e => e.Tag == regionTag.Value);

        if (entity == null) return Array.Empty<SettlementId>();

        var definition = RegionMapper.ToDomain(entity);
        return definition.Areas
            .Where(a => a.LinkedSettlement != null)
            .Select(a => a.LinkedSettlement!.Value)
            .Distinct()
            .ToList();
    }

    public bool TryGetSettlementForArea(AreaTag areaTag, out SettlementId settlementId)
    {
        settlementId = default;
        var all = All();

        foreach (var region in all)
        {
            var area = region.Areas.FirstOrDefault(a =>
                string.Equals(a.ResRef.Value, areaTag.Value, StringComparison.OrdinalIgnoreCase));

            if (area?.LinkedSettlement != null)
            {
                settlementId = area.LinkedSettlement.Value;
                return true;
            }
        }

        return false;
    }

    public IReadOnlyList<AreaDefinition> GetAreasForSettlement(SettlementId settlementId)
    {
        var all = All();

        return all
            .SelectMany(r => r.Areas)
            .Where(a => a.LinkedSettlement?.Value == settlementId.Value)
            .ToList();
    }

    public IReadOnlyList<PlaceOfInterest> GetPointsOfInterest(SettlementId settlementId)
    {
        var all = All();

        return all
            .SelectMany(r => r.Areas)
            .Where(a => a.LinkedSettlement?.Value == settlementId.Value)
            .Where(a => a.PlacesOfInterest is { Count: > 0 })
            .SelectMany(a => a.PlacesOfInterest!)
            .ToList();
    }

    public bool TryGetSettlementForPointOfInterest(string poiResRef, out SettlementId settlementId)
    {
        settlementId = default;
        var all = All();

        foreach (var region in all)
        {
            foreach (var area in region.Areas)
            {
                if (area.PlacesOfInterest == null) continue;
                if (area.PlacesOfInterest.Any(p =>
                        string.Equals(p.ResRef, poiResRef, StringComparison.OrdinalIgnoreCase)))
                {
                    if (area.LinkedSettlement != null)
                    {
                        settlementId = area.LinkedSettlement.Value;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public bool TryGetPointOfInterestByResRef(string poiResRef, out PlaceOfInterest poi)
    {
        poi = default;
        var all = All();

        foreach (var region in all)
        {
            foreach (var area in region.Areas)
            {
                if (area.PlacesOfInterest == null) continue;
                var found = area.PlacesOfInterest.FirstOrDefault(p =>
                    string.Equals(p.ResRef, poiResRef, StringComparison.OrdinalIgnoreCase));

                if (found != default)
                {
                    poi = found;
                    return true;
                }
            }
        }

        return false;
    }

    public IReadOnlyList<PlaceOfInterest> GetPointsOfInterestByTag(string tag)
    {
        var all = All();

        return all
            .SelectMany(r => r.Areas)
            .Where(a => a.PlacesOfInterest is { Count: > 0 })
            .SelectMany(a => a.PlacesOfInterest!)
            .Where(p => string.Equals(p.Tag, tag, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public IReadOnlyList<PlaceOfInterest> GetPointsOfInterestByType(PoiType type)
    {
        if (type == PoiType.Undefined) return Array.Empty<PlaceOfInterest>();

        var all = All();

        return all
            .SelectMany(r => r.Areas)
            .Where(a => a.PlacesOfInterest is { Count: > 0 })
            .SelectMany(a => a.PlacesOfInterest!)
            .Where(p => p.Type == type)
            .ToList();
    }

    public IReadOnlyList<PlaceOfInterest> GetPointsOfInterestForArea(AreaTag areaTag)
    {
        var all = All();

        return all
            .SelectMany(r => r.Areas)
            .Where(a => string.Equals(a.ResRef.Value, areaTag.Value, StringComparison.OrdinalIgnoreCase))
            .Where(a => a.PlacesOfInterest is { Count: > 0 })
            .SelectMany(a => a.PlacesOfInterest!)
            .ToList();
    }

    public PoiLocationInfo? GetPoiLocationInfo(string poiResRef)
    {
        var all = All();

        foreach (var region in all)
        {
            foreach (var area in region.Areas)
            {
                if (area.PlacesOfInterest == null) continue;

                var poi = area.PlacesOfInterest.FirstOrDefault(p =>
                    string.Equals(p.ResRef, poiResRef, StringComparison.OrdinalIgnoreCase));

                if (poi != default)
                {
                    return new PoiLocationInfo(poi, area.LinkedSettlement, region.Tag, area.ResRef);
                }
            }
        }

        return null;
    }

    public bool IsAreaRegistered(string areaResRef)
    {
        var all = All();

        return all.Any(r =>
            r.Areas.Any(a =>
                string.Equals(a.ResRef.Value, areaResRef, StringComparison.OrdinalIgnoreCase)));
    }

    public bool TryGetRegionForArea(string areaResRef, out RegionDefinition? region)
    {
        region = null;
        var all = All();

        foreach (var r in all)
        {
            if (r.Areas.Any(a =>
                    string.Equals(a.ResRef.Value, areaResRef, StringComparison.OrdinalIgnoreCase)))
            {
                region = r;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Search region definitions by name or tag with pagination.
    /// </summary>
    public List<RegionDefinition> Search(string? searchTerm, int page, int pageSize, out int totalCount)
    {
        using var ctx = _contextFactory.CreateDbContext();

        IQueryable<PersistedRegionDefinition> query = ctx.RegionDefinitions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string pattern = $"%{searchTerm}%";
            query = query.Where(e =>
                EF.Functions.ILike(e.Tag, pattern) ||
                EF.Functions.ILike(e.Name, pattern));
        }

        totalCount = query.Count();

        return query
            .OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsEnumerable()
            .Select(RegionMapper.ToDomain)
            .ToList();
    }
}

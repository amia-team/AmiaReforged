using AmiaReforged.Core.Models.DmModels;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(DmAreaService))]
public class DmAreaService(DatabaseContextFactory factory)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void SaveArea(DmArea area)
    {
        AmiaDbContext ctx = new AmiaDbContext();
        try
        {
            area.UpdatedAt = DateTime.UtcNow;
            // Attach the untracked entity to the context
            ctx.DmAreas.Attach(area);

            // Mark the entity as modified
            ctx.Entry(area).State = EntityState.Modified;

            // Save changes
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to save area {AreaName}", area.NewName);
        }
    }

    public List<DmArea> All()
    {
        AmiaDbContext ctx = new AmiaDbContext();

        return ctx.DmAreas.ToList();
    }

    public DmArea? InstanceFromKey(string playerCdKey, string selectedAreaResRef, string newInstanceName)
    {
        AmiaDbContext ctx = new AmiaDbContext();

        DmArea? area = ctx.DmAreas.AsNoTracking().FirstOrDefault(a =>
            a.CdKey == playerCdKey && a.OriginalResRef == selectedAreaResRef && a.NewName == newInstanceName);

        return area;
    }

    public void SaveNew(DmArea newInstance)
    {
        AmiaDbContext ctx = new AmiaDbContext();

        try
        {
            var now = DateTime.UtcNow;
            newInstance.CreatedAt = now;
            newInstance.UpdatedAt = now;
            ctx.DmAreas.Add(newInstance);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public List<DmArea> AllFromResRef(string playerCdKey, string selectedAreaResRef)
    {
        return All().Where(a => a.CdKey == playerCdKey && a.OriginalResRef == selectedAreaResRef).ToList();
    }

    public void Delete(DmArea area)
    {
        using AmiaDbContext ctx = new AmiaDbContext();
        try
        {
            // Check if the entity is already being tracked
            DmArea? tracked = ctx.DmAreas.Local.FirstOrDefault(a => a.Id == area.Id);

            if (tracked != null)
            {
                // The entity is already tracked
                ctx.DmAreas.Remove(tracked);
            }
            else
            {
                // Attach if untracked
                ctx.DmAreas.Attach(area);
                ctx.DmAreas.Remove(area);
            }

            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to delete area {AreaName}", area.NewName);
        }
    }
}

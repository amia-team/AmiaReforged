using AmiaReforged.Core.Models.DmModels;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(DmAreaService))]
public class DmAreaService(DatabaseContextFactory factory)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private AmiaDbContext _ctx = factory.CreateDbContext();

    public void SaveArea(DmArea area)
    {
    }

    public List<DmArea> All()
    {
        return _ctx.DmAreas.ToList();
    }

    public DmArea? InstanceFromKey(string playerCdKey, string selectedAreaResRef, string newInstanceName)
    {
        DmArea? area = _ctx.DmAreas.FirstOrDefault(a =>
            a.CdKey == playerCdKey && a.OriginalResRef == selectedAreaResRef && a.NewName == newInstanceName);

        return area;
    }

    public void SaveNew(DmArea newInstance)
    {
        try
        {
            _ctx.DmAreas.Add(newInstance);
            _ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }
}

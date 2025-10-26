using AmiaReforged.PwEngine.Database.Entities.PlayerHousing;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(IHouseRepository))]
public class HouseRepository(PwContextFactory factory) : IHouseRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void AddHouse(House house)
    {
        try
        {
            using PwEngineContext ctx = factory.CreateDbContext();
            ctx.Houses.Add(house);
            ctx.SaveChanges();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AddHouse failed for tag {Tag}", house?.Tag);
            throw;
        }
    }

    public House? GetHouseByTag(string tag)
    {
        try
        {
            using PwEngineContext ctx = factory.CreateDbContext();
            return ctx.Houses.SingleOrDefault(h => h.Tag == tag);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetHouseByTag failed for tag {Tag}", tag);
            throw;
        }
    }

    public House? GetHouseById(long id)
    {
        try
        {
            using PwEngineContext ctx = factory.CreateDbContext();
            return ctx.Houses.Find(id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetHouseById failed for id {Id}", id);
            throw;
        }
    }

    public IEnumerable<House> GetAllHouses()
    {
        try
        {
            using PwEngineContext ctx = factory.CreateDbContext();
            return ctx.Houses.ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetAllHouses failed");
            throw;
        }
    }

    public void UpdateHouse(House house)
    {
        try
        {
            using PwEngineContext ctx = factory.CreateDbContext();
            ctx.Houses.Update(house);
            ctx.SaveChanges();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "UpdateHouse failed for id {Id} tag {Tag}", house?.Id, house?.Tag);
            throw;
        }
    }

    public void DeleteHouse(House house)
    {
        try
        {
            using PwEngineContext ctx = factory.CreateDbContext();
            ctx.Houses.Remove(house);
            ctx.SaveChanges();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DeleteHouse failed for id {Id} tag {Tag}", house?.Id, house?.Tag);
            throw;
        }
    }
}

public interface IHouseRepository
{
    void AddHouse(House house);
    House? GetHouseByTag(string tag);
    House? GetHouseById(long id);
    IEnumerable<House> GetAllHouses();
    void UpdateHouse(House house);
    void DeleteHouse(House house);
}

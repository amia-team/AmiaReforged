using AmiaReforged.PwEngine.Database.Entities.Shops;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(IPlayerShopRepository))]
public class PlayerShopRepository(PwContextFactory factory) : IPlayerShopRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public List<StallProduct>? ProductsForShop(long shopId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.StallProducts.Where(p => p.ShopId == shopId).ToList();
    }

    public void AddProductToShop(long shopId, StallProduct product)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            ctx.StallProducts.Add(product);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public void RemoveProductFromShop(long shopId, long productId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            StallProduct? product = ctx.StallProducts.SingleOrDefault(p => p.Id == productId && p.ShopId == shopId);
            if (product == null) return;
            ctx.StallProducts.Remove(product);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public bool IsShopOwner(Guid characterId, long shopId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        PlayerStall? shop = ctx.PlayerStalls.Find(shopId);
        if (shop == null) return false;

        return shop.CharacterId == characterId;
    }

    public void CreateShop(PlayerStall shop)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            ctx.PlayerStalls.Add(shop);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public void DeleteShop(long shopId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            PlayerStall? shop = ctx.PlayerStalls.Find(shopId);
            if (shop == null) return;
            ctx.PlayerStalls.Remove(shop);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public void UnownShop(long shopId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            PlayerStall? shop = ctx.PlayerStalls.Find(shopId);
            if (shop == null) return;
            shop.CharacterId = null;
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public PlayerStall? GetShopById(long shopId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.PlayerStalls.Find(shopId);
    }

    public List<PlayerStall> GetAllShops()
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.PlayerStalls.ToList();
    }

    public List<PlayerStall> GetPlayerStalls(long shopId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.PlayerStalls.Where(s => s.Id == shopId).ToList();
    }
}

public interface IPlayerShopRepository
{
    List<StallProduct>? ProductsForShop(long shopId);

    void AddProductToShop(long shopId, StallProduct product);

    void RemoveProductFromShop(long shopId, long productId);

    bool IsShopOwner(Guid characterId, long shopId);

    void CreateShop(PlayerStall shop);
    void DeleteShop(long shopId);

    PlayerStall? GetShopById(long shopId);
    List<PlayerStall> GetAllShops();

    List<PlayerStall> GetPlayerStalls(long shopId);

    void UnownShop(long shopId);
}

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public interface IShopItemBlacklist
{
    bool IsBlacklisted(string resRef);
    void Register(IEnumerable<string> resRefs);
}

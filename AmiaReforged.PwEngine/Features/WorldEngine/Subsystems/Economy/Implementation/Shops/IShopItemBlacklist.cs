namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

public interface IShopItemBlacklist
{
    bool IsBlacklisted(string resRef);
    void Register(IEnumerable<string> resRefs);
}

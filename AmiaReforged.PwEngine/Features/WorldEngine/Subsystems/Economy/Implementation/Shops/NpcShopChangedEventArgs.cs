namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

public sealed class NpcShopChangedEventArgs : EventArgs
{
    public NpcShopChangedEventArgs(NpcShop shop, NpcShopChangeKind changeKind)
    {
        Shop = shop ?? throw new ArgumentNullException(nameof(shop));
        ChangeKind = changeKind;
    }

    public NpcShop Shop { get; }
    public NpcShopChangeKind ChangeKind { get; }
}

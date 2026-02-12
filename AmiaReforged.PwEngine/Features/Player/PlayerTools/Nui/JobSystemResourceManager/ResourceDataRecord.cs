namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.JobSystemResourceManager;

internal record ResourceDataRecord(
    string Name,
    int Quantity,
    string Resref,
    ResourceSource Source,
    int SourceIndex); // For Merchant boxes (1-30) or Miniature box item index

internal enum ResourceSource
{
    MerchantBox,
    MiniatureBox,
    Inventory
}

internal enum ResourceTransferDestination
{
    SelfMerchantBox,
    OtherPlayerMerchantBox,
    MiniatureBox,
    Inventory,
    PlcContainer
}


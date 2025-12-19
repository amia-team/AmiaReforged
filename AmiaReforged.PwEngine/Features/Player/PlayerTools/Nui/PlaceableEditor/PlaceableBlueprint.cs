using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.PlaceableEditor;

internal sealed record PlaceableBlueprint(
    NwItem SourceItem,
    string ResRef,
    string DisplayName,
    int Appearance,
    byte[]? SerializedItemData = null,
    int HealthOverride = -1,
    bool IsPlot = false,
    bool IsStatic = false);

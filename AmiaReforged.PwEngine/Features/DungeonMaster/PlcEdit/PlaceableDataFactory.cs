using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.PlcEdit;

internal static class PlaceableDataFactory
{
    public static PlaceableData From(NwPlaceable placeable)
    {
        return new PlaceableData(
            placeable.Name,
            placeable.Description,
            new PlaceableTransformData(
                placeable.VisualTransform.Translation,
                placeable.VisualTransform.Rotation,
                placeable.VisualTransform.Scale),
            new PlaceableAppearanceData(
                placeable.Appearance.RowIndex,
                placeable.PortraitResRef),
            new PlaceableAreaPositionData(placeable.Position)
        );
    }
}
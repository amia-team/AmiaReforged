namespace AmiaReforged.PwEngine.Features.DungeonMaster.PlcEdit;

internal record PlaceableData(
    string Name,
    string Description,
    PlaceableTransformData Transform,
    PlaceableAppearanceData Appearance,
    PlaceableAreaPositionData Position);
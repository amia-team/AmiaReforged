using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.AreaEdit;

/// <summary>
/// Represents a selected tile and its properties
/// </summary>
public sealed class TileSelection
{
    public Location? Location { get; set; }
    public TileRotation Rotation { get; set; } = TileRotation.Rotate0;

    public bool IsSelected => Location?.TileInfo is not null;

    public int TileId => Location?.TileId ?? 0;
    public int TileHeight => Location?.TileHeight ?? 0;

    public void RotateClockwise()
    {
        Rotation = NextRotation(Rotation);
    }

    public void RotateCounterClockwise()
    {
        Rotation = PreviousRotation(Rotation);
    }

    public void ApplyChanges(int tileId, int tileHeight)
    {
        if (Location is null) return;
        Location.SetTile(tileId, Rotation, tileHeight);
    }

    public void Clear()
    {
        Location = null;
        Rotation = TileRotation.Rotate0;
    }

    private static TileRotation NextRotation(TileRotation rotation)
    {
        int next = ((int)rotation + 1) % Enum.GetValues<TileRotation>().Length;
        return (TileRotation)next;
    }

    private static TileRotation PreviousRotation(TileRotation rotation)
    {
        int prev = ((int)rotation - 1 + Enum.GetValues<TileRotation>().Length)
                   % Enum.GetValues<TileRotation>().Length;
        return (TileRotation)prev;
    }
}

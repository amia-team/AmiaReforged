using System.Numerics;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.LevelEdit.AreaEdit;

/// <summary>
/// Handles tile selection and editing operations
/// </summary>
public sealed class TileEditorHandler
{
    private readonly NwPlayer _player;
    private readonly NuiWindowToken _token;
    private readonly AreaEditorView _view;
    private TileSelection Selection { get; }

    public TileEditorHandler(NwPlayer player, NuiWindowToken token, AreaEditorView view, TileSelection selection)
    {
        _player = player;
        _token = token;
        _view = view;
        Selection = selection;
    }

    public void StartTilePicker(NwArea? selectedArea)
    {
        if (selectedArea is null) return;

        _player.EnterTargetMode(OnTilePicked, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Tile
        });
    }

    public void RotateClockwise()
    {
        Selection.RotateClockwise();
        UpdateUiRotation();
    }

    public void RotateCounterClockwise()
    {
        Selection.RotateCounterClockwise();
        UpdateUiRotation();
    }

    public void ApplyCurrentChanges()
    {
        if (!Selection.IsSelected) return;

        string? tileIdStr = _token.GetBindValue(_view.TileId);
        string? tileHeightStr = _token.GetBindValue(_view.TileHeight);

        if (tileIdStr is not null && tileHeightStr is not null)
        {
            int tileId = int.Parse(tileIdStr);
            int tileHeight = int.Parse(tileHeightStr);
            Selection.ApplyChanges(tileId, tileHeight);
        }
    }

    private void OnTilePicked(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.Player.LoginCreature?.Area is null) return;

        Location loc = Location.Create(obj.Player.LoginCreature.Area, obj.TargetPosition, 0);

        if (loc.TileInfo is not null)
        {
            Selection.Location = loc;
            UpdateUiFromSelection();
        }
    }

    private void UpdateUiFromSelection()
    {
        _token.SetBindValue(_view.TileId, Selection.TileId.ToString());
        _token.SetBindValue(_view.TileRotation, Selection.Rotation.ToString());
        _token.SetBindValue(_view.TileHeight, Selection.TileHeight.ToString());
        _token.SetBindValue(_view.TileIsSelected, Selection.IsSelected);
    }

    private void UpdateUiRotation()
    {
        _token.SetBindValue(_view.TileRotation, Selection.Rotation.ToString());
    }

    public void PickNeighbor(Direction d)
    {
        if (Selection.Location is null) return;

        IReadOnlyList<TileInfo>? areaTileInfo = Selection.Location?.Area.TileInfo;
        if (areaTileInfo is null) return;

        int? currentX = Selection.Location?.TileInfo?.GridX;
        int? currentY = Selection.Location?.TileInfo?.GridY;

        if (currentX is null || currentY is null) return;

        // Directional offsets (dx, dy)
        int dx = 0, dy = 0;
        switch (d)
        {
            case Direction.North:    dy = 1; break;
            case Direction.South:  dy =  -1; break;
            case Direction.West:  dx = -1; break;
            case Direction.East: dx =  1; break;
        }

        int neighborX = currentX.Value + dx;
        int neighborY = currentY.Value + dy;

        // Bounds checking
        int width = Selection.Location!.Area.Size.X;
        int height = Selection.Location!.Area.Size.Y;

        bool inBounds = neighborX >= 0 && neighborX < width &&
                        neighborY >= 0 && neighborY < height;

        if (!inBounds)
        {
            _player.SendServerMessage("You're at the edge of the area!");
            return;
        }

        // Find the neighbor tile and select it
        TileInfo? neighborTile = areaTileInfo.FirstOrDefault(t =>
            t.GridX == neighborX && t.GridY == neighborY);

        if (neighborTile is not null)
        {
            // Get the center of the neighbor tile
            Vector3 centerPos = GetTileCenter(neighborTile);

            // Create a new location at the center of that tile
            Selection.Location = Location.Create(
                Selection.Location.Area,
                centerPos,
                0f);

            UpdateUiFromSelection();
            _player.SendServerMessage($"Selected neighbor tile at grid ({neighborX}, {neighborY})");
        }
    }

    /// <summary>
    /// Gets the center position of a tile in world coordinates
    /// </summary>
    /// <param name="tileInfo">The tile to get the center of</param>
    /// <returns>A Vector3 representing the center position</returns>
    public static Vector3 GetTileCenter(TileInfo tileInfo)
    {
        const float tileSize = 10.0f;
        const float halfTile = tileSize / 2.0f;

        float centerX = (tileInfo.GridX * tileSize) + halfTile;
        float centerY = (tileInfo.GridY * tileSize) + halfTile;

        return new Vector3(centerX, centerY, 0f);
    }

    /// <summary>
    /// Gets the center of the currently selected tile
    /// </summary>
    public Vector3? GetSelectedTileCenter()
    {
        if (!Selection.IsSelected || Selection.Location?.TileInfo is null)
            return null;

        return GetTileCenter(Selection.Location.TileInfo);
    }

    public enum Direction
    {
        North,
        South,
        West,
        East
    }
}

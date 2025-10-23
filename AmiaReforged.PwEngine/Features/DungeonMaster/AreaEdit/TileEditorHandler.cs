using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.AreaEdit;

/// <summary>
/// Handles tile selection and editing operations
/// </summary>
public sealed class TileEditorHandler
{
    private readonly NwPlayer _player;
    private readonly NuiWindowToken _token;
    private readonly AreaEditorView _view;
    private readonly TileSelection _selection;

    public TileEditorHandler(NwPlayer player, NuiWindowToken token, AreaEditorView view, TileSelection selection)
    {
        _player = player;
        _token = token;
        _view = view;
        _selection = selection;
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
        _selection.RotateClockwise();
        UpdateUIRotation();
    }

    public void RotateCounterClockwise()
    {
        _selection.RotateCounterClockwise();
        UpdateUIRotation();
    }

    public void ApplyCurrentChanges()
    {
        if (!_selection.IsSelected) return;

        string? tileIdStr = _token.GetBindValue(_view.TileId);
        string? tileHeightStr = _token.GetBindValue(_view.TileHeight);

        if (tileIdStr is not null && tileHeightStr is not null)
        {
            int tileId = int.Parse(tileIdStr);
            int tileHeight = int.Parse(tileHeightStr);
            _selection.ApplyChanges(tileId, tileHeight);
        }
    }

    private void OnTilePicked(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.Player.LoginCreature?.Area is null) return;

        Location loc = Location.Create(obj.Player.LoginCreature.Area, obj.TargetPosition, 0);

        if (loc.TileInfo is not null)
        {
            _selection.Location = loc;
            UpdateUIFromSelection();
        }
    }

    private void UpdateUIFromSelection()
    {
        _token.SetBindValue(_view.TileId, _selection.TileId.ToString());
        _token.SetBindValue(_view.TileRotation, _selection.Rotation.ToString());
        _token.SetBindValue(_view.TileHeight, _selection.TileHeight.ToString());
        _token.SetBindValue(_view.TileIsSelected, _selection.IsSelected);
    }

    private void UpdateUIRotation()
    {
        _token.SetBindValue(_view.TileRotation, _selection.Rotation.ToString());
    }
}

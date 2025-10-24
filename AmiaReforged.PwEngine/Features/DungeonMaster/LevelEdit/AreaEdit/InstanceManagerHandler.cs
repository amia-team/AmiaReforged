using AmiaReforged.Core.Models.DmModels;
using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Microsoft.IdentityModel.Tokens;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.LevelEdit.AreaEdit;

/// <summary>
/// Manages saving, loading, and deleting area instances
/// </summary>
public sealed class InstanceManagerHandler
{
    private const string IsInstanceLocalInt = "is_instance";

    private readonly NwPlayer _player;
    private readonly NuiWindowToken _token;
    private readonly AreaEditorView _view;
    private readonly AreaEditorState _state;
    private readonly DmAreaService _areaService;
    private readonly WindowDirector? _windowDirector;

    public InstanceManagerHandler(
        NwPlayer player,
        NuiWindowToken token,
        AreaEditorView view,
        AreaEditorState state,
        DmAreaService areaService,
        WindowDirector? windowDirector = null)
    {
        _player = player;
        _token = token;
        _view = view;
        _state = state;
        _areaService = areaService;
        _windowDirector = windowDirector;
    }

    public void SaveInstance()
    {
        if (_state.SelectedArea is null) return;

        string? newInstanceName = _token.GetBindValue(_view.NewAreaName);

        if (newInstanceName.IsNullOrEmpty())
        {
            _player.SendServerMessage("Name Input Cannot Be Empty");
            return;
        }

        DmArea? existing = _areaService.InstanceFromKey(_player.CDKey, _state.SelectedArea.ResRef, newInstanceName!);

        if (existing is null)
        {
            CreateNewInstance(newInstanceName!);
        }
        else
        {
            UpdateExistingInstance(existing);
        }

        RefreshInstanceList();
    }

    public void LoadInstance(int arrayIndex)
    {
        if (arrayIndex >= _state.SavedInstances.Count) return;

        DmArea cloneMe = _state.SavedInstances[arrayIndex];

        NwArea? area = NwArea.Deserialize(
            cloneMe.SerializedARE,
            cloneMe.SerializedGIT,
            $"{_player.CDKey}_{cloneMe.OriginalResRef}_{cloneMe.Id}",
            cloneMe.NewName);

        if (area is null)
        {
            _player.SendServerMessage("Failed to create the area.");
            return;
        }

        NWScript.SetLocalInt(area, IsInstanceLocalInt, NWScript.TRUE);
        _player.SendServerMessage($"{area.Name} created");
    }

    public void DeleteInstance(int arrayIndex)
    {
        if (arrayIndex >= _state.SavedInstances.Count) return;

        DmArea area = _state.SavedInstances[arrayIndex];

        if (_windowDirector is null)
        {
            // Direct delete without confirmation if no window director
            _areaService.Delete(area);
            RefreshInstanceList();
            return;
        }

        _windowDirector.OpenPopupWithReaction(
            _player,
            "Are you sure you want to delete this Instance?",
            "This action is permanent!",
            () =>
            {
                _areaService.Delete(area);
                RefreshInstanceList();
            },
            false,
            _token
        );
    }

    public void RefreshInstanceList()
    {
        if (_state.SelectedArea is null) return;

        _state.SavedInstances = _areaService.AllFromResRef(_player.CDKey, _state.SelectedArea.ResRef);

        List<string> names = _state.SavedInstances.Select(a => a.NewName).ToList();

        _token.SetBindValues(_view.SavedVariantNames, names);
        _token.SetBindValue(_view.SavedVariantCounts, names.Count);
    }

    private void CreateNewInstance(string name)
    {
        if (_state.SelectedArea is null) return;

        byte[]? serializedAre = _state.SelectedArea.SerializeARE();
        if (serializedAre is null)
        {
            _player.SendServerMessage("Failed to serialize ARE");
            return;
        }

        byte[]? serializedGit = _state.SelectedArea.SerializeGIT();
        if (serializedGit is null)
        {
            _player.SendServerMessage("Failed to serialize GIT");
            return;
        }

        DmArea newInstance = new()
        {
            CdKey = _player.CDKey,
            OriginalResRef = _state.SelectedArea.ResRef,
            NewName = name,
            SerializedARE = serializedAre,
            SerializedGIT = serializedGit
        };

        _areaService.SaveNew(newInstance);
    }

    private void UpdateExistingInstance(DmArea existing)
    {
        if (_state.SelectedArea is null) return;

        byte[]? serializedAre = _state.SelectedArea.SerializeARE();
        if (serializedAre is null)
        {
            _player.SendServerMessage("Failed to serialize ARE");
            return;
        }

        byte[]? serializedGit = _state.SelectedArea.SerializeGIT();
        if (serializedGit is null)
        {
            _player.SendServerMessage("Failed to serialize GIT");
            return;
        }

        existing.SerializedARE = serializedAre;
        existing.SerializedGIT = serializedGit;

        _areaService.SaveArea(existing);
    }
}

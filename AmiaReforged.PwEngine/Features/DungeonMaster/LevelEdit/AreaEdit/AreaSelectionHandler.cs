using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.LevelEdit.AreaEdit;

/// <summary>
/// Handles area selection and filtering
/// </summary>
public sealed class AreaSelectionHandler
{
    private readonly NwPlayer _player;
    private readonly NuiWindowToken _token;
    private readonly AreaEditorView _view;
    private readonly AreaEditorState _state;

    public AreaSelectionHandler(NwPlayer player, NuiWindowToken token, AreaEditorView view, AreaEditorState state)
    {
        _player = player;
        _token = token;
        _view = view;
        _state = state;
    }

    public void UpdateSearchFilter(string search)
    {
        _state.SearchFilter = search.Trim();
        RefreshAreaList();
    }

    public void SelectCurrentArea()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature?.Area is null) return;

        SelectArea(creature.Area);
    }

    public void SelectAreaByIndex(int arrayIndex)
    {
        if (arrayIndex >= _state.VisibleAreas.Count) return;

        string areaResRef = _state.VisibleAreas[arrayIndex].Split("|")[1];
        NwArea? area = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == areaResRef);

        if (area is not null)
        {
            SelectArea(area);
        }
    }

    public void ReloadSelectedArea()
    {
        if (_state.SelectedArea is null) return;
        if (_player.LoginCreature?.Location is null) return;

        List<(NwCreature c, Location l)> allCurrent = [];

        allCurrent.Add((_player.LoginCreature, _player.LoginCreature.Location));
        _player.LoginCreature.Location = NwModule.Instance.StartingLocation;

        foreach (NwCreature creature in _state.SelectedArea.FindObjectsOfTypeInArea<NwCreature>())
        {
            if (!creature.IsLoginPlayerCharacter || creature.Location is null) continue;

            allCurrent.Add((creature, creature.Location));
            _player.SendServerMessage($"Jumping {creature.Name}");
            creature.ActionJumpToLocation(NwModule.Instance.StartingLocation);
        }

        NWScript.DelayCommand(5.0f, () =>
        {
            foreach ((NwCreature c, Location l) in allCurrent)
            {
                c.Location = l;
            }
        });
    }

    public void RefreshAreaList()
    {
        _state.VisibleAreas = GetFilteredAreas();
        _token.SetBindValue(_view.AreaCount, _state.VisibleAreas.Count);
        _token.SetBindValues(_view.AreaNames, _state.VisibleAreas);
    }

    private void SelectArea(NwArea area)
    {
        _state.SelectedArea = area;

        bool canSave = NWScript.GetLocalInt(area, "is_instance") != NWScript.TRUE;
        _token.SetBindValue(_view.CanSaveArea, canSave);
    }

    private List<string> GetFilteredAreas()
    {
        if (string.IsNullOrWhiteSpace(_state.SearchFilter))
        {
            return NwModule.Instance.Areas.Select(a => $"{a.Name}|{a.ResRef}").ToList();
        }

        string searchLower = _state.SearchFilter.ToLowerInvariant();

        return NwModule.Instance.Areas
            .Where(a =>
                a.ResRef.Contains(searchLower, StringComparison.InvariantCultureIgnoreCase) ||
                a.Name.Contains(searchLower, StringComparison.InvariantCultureIgnoreCase))
            .Select(a => $"{a.Name}|{a.ResRef}")
            .ToList();
    }
}

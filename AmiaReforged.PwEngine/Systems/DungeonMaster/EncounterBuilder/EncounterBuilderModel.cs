using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Systems.DungeonMaster.EncounterBuilder.EncounterEditor;
using AmiaReforged.PwEngine.Systems.DungeonMaster.EncounterBuilder.EncounterMaker;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.IdentityModel.Tokens;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster.EncounterBuilder;

public sealed class EncounterBuilderModel(NwPlayer player)
{
    [Inject] private Lazy<EncounterService> EncounterService { get; init; } = null!;
    [Inject] private Lazy<WindowDirector> Director { get; init; } = null!;

    public List<Encounter> VisibleEncounters { get; set; } = [];
    private List<Encounter> Encounters { get; set; } = [];

    private string _searchTerm = String.Empty;

    public delegate void Updating(EncounterBuilderModel me, EventArgs e);

    public event Updating? Update;

    public void LoadEncounters()
    {
        Encounters = EncounterService.Value.GetEncountersForDm(player.CDKey).ToList();

        RefreshEncounters();
    }

    public void RefreshEncounters()
    {
        if (_searchTerm.IsNullOrEmpty())
        {
            VisibleEncounters = Encounters;
            return;
        }


        VisibleEncounters =
            Encounters
                .FindAll(e => e.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.Id)
                .ToList();
    }

    public void SetSearchTerm(string search)
    {
        _searchTerm = search;
    }

    public void OpenEncounterCreator()
    {
        EncounterMakerView window = new(player);

        EncounterMakerPresenter encounterMakerPresenter = window.Presenter;
        Director.Value.OpenWindow(encounterMakerPresenter);

        encounterMakerPresenter.OnClosing += Reload;
    }

    private void Reload(EncounterMakerPresenter? me, EventArgs e)
    {
        LoadEncounters();
        RefreshEncounters();

        OnUpdate(this);

        me!.OnClosing -= Reload;
    }

    public void OpenEncounterEditor(int eventDataArrayIndex)
    {
        EncounterEditorView window = new(player);

        EncounterEditorPresenter editor = window.Presenter;

        editor.SetModelEncounter(VisibleEncounters[eventDataArrayIndex]);
        editor.OnClosing += ReloadAfterEdit;

        Director.Value.OpenWindow(editor);
    }

    private void ReloadAfterEdit(EncounterEditorPresenter me, EventArgs e)
    {
        LoadEncounters();
        RefreshEncounters();

        OnUpdate(this);

        me!.OnClosing -= ReloadAfterEdit;
    }

    public void PromptForDeletion(int eventDataArrayIndex)
    {
        LoadEncounters();

        Encounter e = VisibleEncounters.ToArray()[eventDataArrayIndex];

        Director.Value.OpenPopupWithReaction(
            player,
            "Really delete this Encounter?",
            "If you delete this Encounter, all entries associated with it will also be destroyed. The action is permanent",
            () =>
            {
                EncounterService.Value.DeleteEncounter(e);
                OnUpdate(this);
            }
        );
    }

    private void OnUpdate(EncounterBuilderModel me)
    {
        Update?.Invoke(me, EventArgs.Empty);
    }

    public void PromptSpawn(int eventDataArrayIndex, NwFaction faction)
    {
        SelectedEncounter = VisibleEncounters.ToArray()[eventDataArrayIndex];
        SelectedFaction = faction;
        player.EnterTargetMode(ValidateAndSpawn, new TargetModeSettings
        {
            CursorType = MouseCursor.Action,
            ValidTargets = ObjectTypes.Tile
        });
    }

    private void ValidateAndSpawn(ModuleEvents.OnPlayerTarget obj)
    {
        Location spawnLocation = Location.Create(player.LoginCreature!.Area!, obj.TargetPosition, 0);

        SelectedEncounter.SpawnEncounters(spawnLocation!, SelectedFaction);
    }

    private NwFaction SelectedFaction { get; set; } = null!;

    private Encounter SelectedEncounter { get; set; } = null!;
}
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.IdentityModel.Tokens;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster.EncounterBuilder.EncounterEditor;

public sealed class EncounterEditorModel(NwPlayer player)
{
    private string _searchTerm = String.Empty;

    [Inject] private Lazy<EncounterService> EncounterService { get; set; } = null!;
    [Inject] private Lazy<WindowDirector> WindowDirector { get; set; } = null!;
    public List<EncounterEntry> VisibleEntries = [];
    public Encounter Encounter { get; set; } = null!;
    private List<EncounterEntry> EncounterEntries { get; set; } = [];

    public delegate void EntryUpdate();

    public event EntryUpdate? EntryUpdated;

    public void LoadEntries()
    {
        EncounterEntries = EncounterService.Value.GetEntries(Encounter.Id).ToList();
    }

    public void SetSearchTerm(string search)
    {
        _searchTerm = search;
    }

    public void RefreshEntryList()
    {
        if (_searchTerm.IsNullOrEmpty())
        {
            VisibleEntries = 
                EncounterEntries
                    .OrderBy(e => e.Id)
                    .ToList();
            return;
        }

        VisibleEntries =
            EncounterEntries
                .FindAll(e => e.Name.Contains(_searchTerm, StringComparison.Ordinal))
                .OrderBy(e => e.Id)
                .ToList();
    }

    public void PromptAdd()
    {
        player.EnterTargetMode(ValidateAndAdd, new()
        {
            CursorType = MouseCursor.Create,
            ValidTargets = ObjectTypes.Creature
        });

    }

    public void PromptDelete(int eventDataArrayIndex)
    {
        EncounterEntry e = VisibleEntries.ToArray()[eventDataArrayIndex];

        WindowDirector.Value.OpenPopupWithReaction(
            player,
            "Really delete entry?",
            "If you delete this NPC from the encounter, the action is permanent", () =>
            {
                EncounterService.Value.DeleteEntry(e);
                OnEntryUpdated();
            }
        );
    }


    private void ValidateAndAdd(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.TargetObject.IsPlayerControlled(out NwPlayer? _))
        {
            player.SendServerMessage("Please don't try to clone players to the database", ColorConstants.Red);
            return;
        }

        if (obj.TargetObject is not NwCreature creature)
        {
            player.SendServerMessage("Target must be creature", ColorConstants.Red);
            return;
        }

        AddEntry(creature);
        OnEntryUpdated();
    }

    private void AddEntry(NwCreature creature)
    {
        byte[]? maybeSerialized = creature.Serialize();
        if (maybeSerialized == null)
        {
            player.SendServerMessage("Serialization failed. Couldn't save this NPC.");
            return;
        }

        EncounterEntry entry = new EncounterEntry
        {
            Name = creature.Name,
            SerializedString = maybeSerialized,
            EncounterId = Encounter.Id
        };

        EncounterService.Value.AddEncounterEntry(entry);
    }

    private void OnEntryUpdated()
    {
        EntryUpdated?.Invoke();
    }
}
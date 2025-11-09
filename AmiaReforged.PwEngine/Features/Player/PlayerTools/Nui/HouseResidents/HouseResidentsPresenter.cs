using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.HouseResidents;

public sealed class HouseResidentsPresenter : ScryPresenter<HouseResidentsView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    private PropertyId? _currentPropertyId;
    private List<ResidentEntry> _residents = new();
    private bool _hasLeaseControl;

    public HouseResidentsPresenter(HouseResidentsView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override HouseResidentsView View { get; }

    [Inject] private Lazy<GetPropertyByPoiQueryHandler> PropertyQueryHandler { get; init; } = null!;
    [Inject] private Lazy<IRentablePropertyRepository> PropertyRepository { get; init; } = null!;
    [Inject] private Lazy<RuntimeCharacterService> CharacterService { get; init; } = null!;

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 630f, 620f),
            Resizable = false
        };
    }

    public override void Create()
    {
        if (_window == null)
        {
            InitBefore();
        }

        if (_window == null)
        {
            _player.SendServerMessage("Failed to create the House Residents window.", ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        InitializeBinds();
        _ = LoadPropertyDataAsync();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleClick(eventData);
                break;
        }
    }

    public override void Close()
    {
        _residents.Clear();
        _currentPropertyId = null;
        _token.Close();
    }

    private void InitializeBinds()
    {
        Token().SetBindValue(View.StatusMessage, "Loading house information...");
        Token().SetBindValue(View.ResidentCount, 0);
        Token().SetBindValue(View.ResidentNames, string.Empty);
        Token().SetBindValue(View.HasLeaseControl, false);
    }

    private void HandleClick(ModuleEvents.OnNuiEvent eventData)
    {
        string? elementId = eventData.ElementId;

        switch (elementId)
        {
            case "btn_add":
                BeginAddResident();
                break;
            case "btn_remove":
                _ = RemoveResidentAsync(eventData.ArrayIndex);
                break;
            case "btn_refresh":
                _ = LoadPropertyDataAsync();
                break;
        }
    }

    private async Task LoadPropertyDataAsync()
    {
        Token().SetBindValue(View.StatusMessage, "Loading property data...");

        try
        {
            NwArea? area = _player.ControlledCreature?.Area;
            if (area == null)
            {
                Token().SetBindValue(View.StatusMessage, "Unable to determine current area.");
                return;
            }

            // Use the area's ResRef to look up the POI and associated property
            string areaResRef = area.ResRef;

            // Query the property using CQRS pattern
            RentablePropertySnapshot? snapshot = await PropertyQueryHandler.Value.HandleAsync(
                new GetPropertyByPoiQuery(areaResRef));

            // Switch back to main thread after async database operation
            await NwTask.SwitchToMainThread();

            if (snapshot == null)
            {
                Token().SetBindValue(View.StatusMessage, "This area is not linked to a rentable property.");
                _hasLeaseControl = false;
                Token().SetBindValue(View.HasLeaseControl, false);
                return;
            }

            _currentPropertyId = snapshot.Definition.Id;

            if (!CharacterService.Value.TryGetPlayerKey(_player, out Guid playerCharacterId))
            {
                Token().SetBindValue(View.StatusMessage, "Unable to determine your character key.");
                _hasLeaseControl = false;
                Token().SetBindValue(View.HasLeaseControl, false);
                return;
            }

            PersonaId playerPersona = PersonaId.FromCharacter(new CharacterId(playerCharacterId));

            // Check if player is the tenant or owner
            _hasLeaseControl = (snapshot.CurrentTenant != null && snapshot.CurrentTenant.Value.Equals(playerPersona)) ||
                              (snapshot.CurrentOwner != null && snapshot.CurrentOwner.Value.Equals(playerPersona)) ||
                              _player.IsDM;

            Token().SetBindValue(View.HasLeaseControl, _hasLeaseControl);

            // Load residents
            _residents = snapshot.Residents
                .Select(persona => new ResidentEntry(persona, ExtractDisplayName(persona)))
                .OrderBy(r => r.DisplayName)
                .ToList();

            UpdateResidentList();

            if (_hasLeaseControl)
            {
                Token().SetBindValue(View.StatusMessage, $"Property loaded. {_residents.Count} resident(s).");
            }
            else
            {
                Token().SetBindValue(View.StatusMessage, "You do not have lease control for this property.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load property data for player {PlayerName}", _player.PlayerName);
            await NwTask.SwitchToMainThread();
            Token().SetBindValue(View.StatusMessage, $"Error loading property: {ex.Message}");
        }
    }

    private void BeginAddResident()
    {
        if (!_hasLeaseControl)
        {
            Token().SetBindValue(View.StatusMessage, "You do not have permission to add residents.");
            return;
        }

        Token().SetBindValue(View.StatusMessage, "Target a player character to add as a resident...");

        _player.EnterTargetMode(HandleAddResidentTarget, new TargetModeSettings
        {
            CursorType = MouseCursor.Action,
            ValidTargets = ObjectTypes.Creature
        });
    }

    private void HandleAddResidentTarget(ModuleEvents.OnPlayerTarget targetData)
    {
        if (targetData.TargetObject is not NwCreature targetCreature)
        {
            Token().SetBindValue(View.StatusMessage, "You must target a player character.");
            return;
        }

        NwPlayer? targetPlayer = targetCreature.ControllingPlayer;
        if (targetPlayer == null || !targetCreature.IsPlayerControlled)
        {
            Token().SetBindValue(View.StatusMessage, "Target must be a player character.");
            return;
        }

        if (!CharacterService.Value.TryGetPlayerKey(targetPlayer, out Guid targetCharacterId))
        {
            Token().SetBindValue(View.StatusMessage, "Unable to determine target character key.");
            return;
        }

        PersonaId targetPersona = PersonaId.FromCharacter(new CharacterId(targetCharacterId));

        if (_residents.Any(r => r.Persona.Equals(targetPersona)))
        {
            Token().SetBindValue(View.StatusMessage, $"{targetCreature.Name} is already a resident.");
            return;
        }

        _ = AddResidentAsync(targetPersona, targetCreature.Name);
    }

    private async Task AddResidentAsync(PersonaId persona, string displayName)
    {
        if (_currentPropertyId == null)
        {
            Token().SetBindValue(View.StatusMessage, "No property loaded.");
            return;
        }

        Token().SetBindValue(View.StatusMessage, $"Adding {displayName} as a resident...");

        try
        {
            RentablePropertySnapshot? snapshot = await PropertyRepository.Value.GetSnapshotAsync(_currentPropertyId.Value);

            // Switch back to main thread after async database operation
            await NwTask.SwitchToMainThread();

            if (snapshot == null)
            {
                Token().SetBindValue(View.StatusMessage, "Property not found.");
                return;
            }

            List<PersonaId> updatedResidents = new(snapshot.Residents) { persona };

            RentablePropertySnapshot updatedSnapshot = snapshot with
            {
                Residents = updatedResidents
            };

            await PropertyRepository.Value.PersistRentalAsync(updatedSnapshot);

            // Switch back to main thread after async database operation
            await NwTask.SwitchToMainThread();

            _residents.Add(new ResidentEntry(persona, displayName));
            _residents = _residents.OrderBy(r => r.DisplayName).ToList();

            UpdateResidentList();
            Token().SetBindValue(View.StatusMessage, $"Added {displayName} as a resident.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add resident {Persona}", persona);
            await NwTask.SwitchToMainThread();
            Token().SetBindValue(View.StatusMessage, $"Failed to add resident: {ex.Message}");
        }
    }

    private async Task RemoveResidentAsync(int index)
    {
        if (_currentPropertyId == null)
        {
            Token().SetBindValue(View.StatusMessage, "No property loaded.");
            return;
        }

        if (!_hasLeaseControl)
        {
            Token().SetBindValue(View.StatusMessage, "You do not have permission to remove residents.");
            return;
        }

        if (index < 0 || index >= _residents.Count)
        {
            Token().SetBindValue(View.StatusMessage, "Invalid resident selection.");
            return;
        }

        ResidentEntry resident = _residents[index];
        Token().SetBindValue(View.StatusMessage, $"Removing {resident.DisplayName}...");

        try
        {
            RentablePropertySnapshot? snapshot = await PropertyRepository.Value.GetSnapshotAsync(_currentPropertyId.Value);

            // Switch back to main thread after async database operation
            await NwTask.SwitchToMainThread();

            if (snapshot == null)
            {
                Token().SetBindValue(View.StatusMessage, "Property not found.");
                return;
            }

            List<PersonaId> updatedResidents = snapshot.Residents
                .Where(p => !p.Equals(resident.Persona))
                .ToList();

            RentablePropertySnapshot updatedSnapshot = snapshot with
            {
                Residents = updatedResidents
            };

            await PropertyRepository.Value.PersistRentalAsync(updatedSnapshot);

            // Switch back to main thread after async database operation
            await NwTask.SwitchToMainThread();

            _residents.RemoveAt(index);
            UpdateResidentList();
            Token().SetBindValue(View.StatusMessage, $"Removed {resident.DisplayName}.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to remove resident {Persona}", resident.Persona);
            await NwTask.SwitchToMainThread();
            Token().SetBindValue(View.StatusMessage, $"Failed to remove resident: {ex.Message}");
        }
    }

    private void UpdateResidentList()
    {
        Token().SetBindValue(View.ResidentCount, _residents.Count);

        if (_residents.Count == 0)
        {
            Token().SetBindValue(View.ResidentNames, string.Empty);
        }
        else
        {
            string names = string.Join("", _residents.Select(r => r.DisplayName));
            Token().SetBindValue(View.ResidentNames, names);
        }
    }

    private static string ExtractDisplayName(PersonaId persona)
    {
        // For now, just show the persona string
        // In the future, this could look up actual character names from the database
        return persona.Type == PersonaType.Character
            ? $"Character ({persona.Value[..8]}...)"
            : persona.ToString();
    }

    private sealed record ResidentEntry(PersonaId Persona, string DisplayName);
}

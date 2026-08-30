using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipCombatNuiService))]
public sealed class ShipCombatNuiService
{
    private const string WindowId =
        "ShipCombatNui";

    private readonly Dictionary<
        string,
        NuiWindowToken>
        _tokens = new();

    private readonly NuiBind<string> _targetBind =
        new("combat_target");

    private readonly NuiBind<string> _distanceBind =
        new("combat_distance");

    private readonly NuiBind<string> _hullBind =
        new("combat_hull");

    private readonly NuiBind<string> _statusBind =
        new("combat_status");

    private readonly PhysicalShipService
        _physicalShipService;

    private readonly ShipSpellEffectService
        _shipSpellEffectService;

    private readonly ShipSpellService
        _shipSpellService;

    private readonly HelmService
        _helmService;

    private readonly ShipEncounterService
        _shipEncounterService;

    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    // -----------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------

    public ShipCombatNuiService(
        ShipEncounterService shipEncounterService,
        PhysicalShipService physicalShipService,
        ShipSpellEffectService shipSpellEffectService,
        HelmService helmService,
        ShipSpellService shipSpellService)
    {
        _shipEncounterService =
            shipEncounterService;

        _physicalShipService =
            physicalShipService;

        _shipSpellEffectService =
            shipSpellEffectService;

        _helmService =
            helmService;

        _shipSpellService =
            shipSpellService;

        shipEncounterService.EncounterStarted +=
            OnEncounterStarted;

        shipEncounterService.EncounterEnded +=
            OnEncounterEnded;

        Log.Info(
            "Ship Combat NUI Service initialized.");
    }

    // -----------------------------------------------------------------
    // Encounter Started
    // -----------------------------------------------------------------

    private void OnEncounterStarted(
        ShipEncounter encounter)
    {
        OpenForShip(
            encounter.ShipA,
            encounter.ShipB,
            encounter);

        OpenForShip(
            encounter.ShipB,
            encounter.ShipA,
            encounter);

        Log.Info(
            $"Ship Combat NUI opened for encounter: " +
            $"{encounter.ShipA.ShipName} <-> " +
            $"{encounter.ShipB.ShipName}.");
    }

    // -----------------------------------------------------------------
    // Encounter Ended
    // -----------------------------------------------------------------

    private void OnEncounterEnded(
        ShipEncounter encounter)
    {
        CloseForShip(
            encounter.ShipA);

        CloseForShip(
            encounter.ShipB);

        Log.Info(
            $"Ship Combat NUI closed for encounter: " +
            $"{encounter.ShipA.ShipName} <-> " +
            $"{encounter.ShipB.ShipName}.");
    }

    // -----------------------------------------------------------------
    // Open For Ship
    // -----------------------------------------------------------------

    private void OpenForShip(
        ShipState ship,
        ShipState targetShip,
        ShipEncounter encounter)
    {
        foreach (string playerName
                 in _physicalShipService.GetPlayersAboard(
                     ship.ShipName))
        {
            NwPlayer? player =
                NwModule.Instance.Players.FirstOrDefault(
                    p =>
                        string.Equals(
                            p.PlayerName,
                            playerName,
                            StringComparison.Ordinal));

            if (player == null)
                continue;

            Open(
                player,
                ship,
                targetShip,
                encounter);
        }
    }

    // -----------------------------------------------------------------
    // Close For Ship
    // -----------------------------------------------------------------

    private void CloseForShip(
        ShipState ship)
    {
        foreach (string playerName
                 in _physicalShipService.GetPlayersAboard(
                     ship.ShipName))
        {
            NwPlayer? player =
                NwModule.Instance.Players.FirstOrDefault(
                    p =>
                        string.Equals(
                            p.PlayerName,
                            playerName,
                            StringComparison.Ordinal));

            if (player == null)
                continue;

            Close(
                player);
        }
    }

    // -----------------------------------------------------------------
    // Open
    // -----------------------------------------------------------------

    public void Open(
        NwPlayer player,
        ShipState ship,
        ShipState targetShip,
        ShipEncounter encounter)
    {
        if (_tokens.ContainsKey(
                player.PlayerName))
        {
            Update(
                player,
                ship,
                targetShip,
                encounter);

            return;
        }

        // -------------------------------------------------------------
        // Header
        // -------------------------------------------------------------

        NuiLabel title =
            InfoLabel(
                NuiProperty<string>.CreateValue(
                    "SHIP COMBAT"),
                30.0f);

        NuiLabel subtitle =
            InfoLabel(
                NuiProperty<string>.CreateValue(
                    ship.ShipName.ToUpper()),
                24.0f);

        // -------------------------------------------------------------
        // Target information
        // -------------------------------------------------------------

        NuiLabel target =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "combat_target"));

        NuiLabel distance =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "combat_distance"));

        NuiLabel hull =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "combat_hull"));

        NuiLabel status =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "combat_status"));

        // -------------------------------------------------------------
        // Ship Magic
        // -------------------------------------------------------------

        NuiLabel spellHeader =
            SectionHeader(
                "SHIP MAGIC");

        NuiColumn spellColumn =
            new();

        NwCreature? caster =
            player.LoginCreature;

         if (caster != null)
        {
            List<ShipSpellEffectDefinition>
                availableSpells =
                    GetAvailableShipSpells(
                        caster);

            if (availableSpells.Count == 0)
            {
                spellColumn.Children.Add(
                    SectionHeader(
                        "No available ship spells."));
            }
            else
            {
                NuiRow currentRow =
                    new();

                int buttonsInRow = 0;

                foreach (
                    ShipSpellEffectDefinition definition
                    in availableSpells)
                {
                    bool canUse =
                        CanUseShipSpell(
                            definition,
                            ship,
                            targetShip,
                            encounter);

                    string buttonText =
                        definition.DisplayName.ToUpper();

                    if (!canUse)
                    {
                        buttonText +=
                            " (UNAVAILABLE)";
                    }

                    NuiButton spellButton =
                        Button(
                            buttonText,
                            $"ship_spell_{definition.SpellId}");

                    currentRow.Children.Add(
                        spellButton);

                    buttonsInRow++;

                    if (buttonsInRow >= 3)
                    {
                        spellColumn.Children.Add(
                            currentRow);

                        currentRow =
                            new();

                        buttonsInRow = 0;
                    }
                }

                if (buttonsInRow > 0)
                {
                    spellColumn.Children.Add(
                        currentRow);
                }
            }
        }
        else
        {
            spellColumn.Children.Add(
                SectionHeader(
                    "Spellcaster unavailable."));
        }

        // -------------------------------------------------------------
        // Layout
        // -------------------------------------------------------------
        NuiColumn column =
            new();

        column.Children.Add(
            title);

        column.Children.Add(
            subtitle);

        column.Children.Add(
            Spacer());

        column.Children.Add(
            SectionHeader(
                "TARGET"));

        column.Children.Add(
            target);

        column.Children.Add(
            distance);

        column.Children.Add(
            hull);

        column.Children.Add(
            Spacer());

        column.Children.Add(
            status);

        column.Children.Add(
            Spacer());

        column.Children.Add(
            spellHeader);

        column.Children.Add(
            spellColumn);

        // -------------------------------------------------------------
        // Window
        // -------------------------------------------------------------

        NuiWindow window =
            new(
                column,
                NuiProperty<string>.CreateValue(
                    "Ship Combat"));

        window.Geometry =
            new NuiRect(
                -1.0f,
                -1.0f,
                500.0f,
                380.0f);

        window.Closable =
            true;

        if (!player.TryCreateNuiWindow(
                window,
                out NuiWindowToken token,
                WindowId))
        {
            Log.Error(
                $"Failed to create ship combat NUI " +
                $"for player {player.PlayerName}.");

            return;
        }

        _tokens[player.PlayerName] =
            token;

        player.OnNuiEvent +=
            HandleCombatNuiEvent;

        Update(
            player,
            ship,
            targetShip,
            encounter);

        Log.Info(
            $"Ship Combat NUI opened for player " +
            $"{player.PlayerName}: " +
            $"{ship.ShipName} -> " +
            $"{targetShip.ShipName}.");
    }
        
    
    // -----------------------------------------------------------------
    // Get Available Ship Spells
    // -----------------------------------------------------------------

    private List<ShipSpellEffectDefinition>
    GetAvailableShipSpells(
        NwCreature caster)
{
    List<ShipSpellEffectDefinition>
        availableSpells =
            new();

    foreach (
        ShipSpellEffectDefinition definition
        in ShipSpellEffectService.GetDefinitions())
    {
        NwSpell? spell =
            NwSpell.FromSpellId(
                definition.SpellId);

        if (spell == null)
        {
            Log.Warn(
                $"Ship spell unavailable: " +
                $"Could not resolve spell ID " +
                $"{definition.SpellId}.");

            continue;
        }

        NwClass? castingClass =
            _shipSpellService.GetCastingClass(
                caster,
                spell);

        if (castingClass == null)
        {
            Log.Info(
                $"Ship spell not available to caster: " +
                $"Caster={caster.Name}, " +
                $"Spell={definition.DisplayName}, " +
                $"SpellId={definition.SpellId}.");

            continue;
        }

        availableSpells.Add(
            definition);

        Log.Info(
            $"Ship spell available: " +
            $"Caster={caster.Name}, " +
            $"Spell={definition.DisplayName}, " +
            $"SpellId={definition.SpellId}, " +
            $"Class={castingClass.ClassType}.");
    }

    Log.Info(
        $"Ship spell availability complete: " +
        $"Caster={caster.Name}, " +
        $"Available={availableSpells.Count}.");

    return availableSpells;
}
    // -----------------------------------------------------------------
    // Combat NUI Event
    // -----------------------------------------------------------------

    private void HandleCombatNuiEvent(
        ModuleEvents.OnNuiEvent obj)
    {
        if (!_tokens.ContainsKey(
                obj.Player.PlayerName))
        {
            return;
        }

        if (obj.EventType !=
            NuiEventType.MouseUp)
        {
            return;
        }

        if (!obj.ElementId.StartsWith(
                "ship_spell_",
                StringComparison.Ordinal))
        {
            return;
        }

        string spellIdText =
            obj.ElementId.Substring(
                "ship_spell_".Length);

        if (!int.TryParse(
                spellIdText,
                out int spellId))
        {
            Log.Warn(
                $"Invalid ship spell button ID: " +
                $"{obj.ElementId}");

            return;
        }

        ProcessShipSpell(
            obj.Player,
            spellId);
    }

    // -----------------------------------------------------------------
    // Process Ship Spell
    // -----------------------------------------------------------------

    private void ProcessShipSpell(
        NwPlayer player,
        int spellId)
    {
        NwCreature? caster =
            player.LoginCreature;

        if (caster == null)
        {
            Log.Warn(
                $"Ship spell rejected: " +
                $"Could not resolve caster for " +
                $"{player.PlayerName}.");

            return;
        }

        NwSpell? spell =
            NwSpell.FromSpellId(
                spellId);

        if (spell == null)
        {
            Log.Warn(
                $"Ship spell rejected: " +
                $"Could not resolve spell ID " +
                $"{spellId}.");

            return;
        }

        if (!ShipSpellEffectService.TryGetDefinition(
                spell,
                out ShipSpellEffectDefinition? definition) ||
            definition == null)
        {
            Log.Warn(
                $"Ship spell rejected: " +
                $"No sailing definition exists for " +
                $"spell ID {spellId}.");

            return;
        }

        // -------------------------------------------------------------
        // Re-check availability.
        // -------------------------------------------------------------

       NwClass? castingClass =
    _shipSpellService.GetCastingClass(
        caster,
        spell);

if (castingClass == null)
{
    player.SendServerMessage(
        $"You do not currently have " +
        $"{definition.DisplayName} available.");

    Log.Warn(
        $"Ship spell rejected: " +
        $"Player={player.PlayerName}, " +
        $"Spell={definition.DisplayName}, " +
        $"SpellId={spellId}.");

    return;
}

        // -------------------------------------------------------------
        // Definition requirements.
        // -------------------------------------------------------------

        string? shipName =
            _physicalShipService.GetShipForPlayer(
                player.PlayerName);

        if (shipName == null)
            return;

        ShipState? ship =
            _helmService.GetShip(
                shipName);

        if (ship == null)
            return;

        if (definition.RequiresEncounter)
        {
            if (!_shipEncounterService.TryGetTarget(
                    ship,
                    out ShipState? targetShip,
                    out ShipEncounter? encounter) ||
                targetShip == null ||
                encounter == null)
            {
                player.SendServerMessage(
                    "There is no enemy ship in range.");

                return;
            }
        }

        // -------------------------------------------------------------
        // Execute the sailing effect.
        // -------------------------------------------------------------

       // -------------------------------------------------------------
// Execute the sailing spell through the central effect service.
// -------------------------------------------------------------
if (_shipSpellService.IsPreparedCaster(
        castingClass))
{
    if (!_shipSpellService.ConsumeMemorizedSpell(
            caster,
            spell))
    {
        player.SendServerMessage(
            $"You have no ready memorized " +
            $"{definition.DisplayName} spell available.");

        Log.Warn(
            $"Ship spell rejected: " +
            $"No memorized slot available. " +
            $"Player={player.PlayerName}, " +
            $"Spell={definition.DisplayName}, " +
            $"SpellId={spell.Id}.");

        return;
    }
}
_shipSpellEffectService.ProcessSpell(
    player,
    caster,
    spell);

RefreshCombatWindow(
    player);
        RefreshCombatWindow(
            player);
    }

    // -----------------------------------------------------------------
    // Refresh Combat Window
    // -----------------------------------------------------------------

    private void RefreshCombatWindow(
        NwPlayer player)
    {
        string? shipName =
            _physicalShipService.GetShipForPlayer(
                player.PlayerName);

        if (shipName == null)
            return;

        ShipState? ship =
            _helmService.GetShip(
                shipName);

        if (ship == null)
            return;

        if (!_shipEncounterService.TryGetTarget(
                ship,
                out ShipState? targetShip,
                out ShipEncounter? encounter) ||
            targetShip == null ||
            encounter == null)
        {
            Close(player);
            return;
        }

        Update(
            player,
            ship,
            targetShip,
            encounter);
    }

    // -----------------------------------------------------------------
    // Update
    // -----------------------------------------------------------------

    public void Update(
        NwPlayer player,
        ShipState ship,
        ShipState? targetShip,
        ShipEncounter? encounter)
    {
        if (!_tokens.TryGetValue(
                player.PlayerName,
                out NuiWindowToken token))
        {
            return;
        }

        if (targetShip == null ||
            encounter == null)
        {
            token.SetBindValue(
                _targetBind,
                "Target: NONE");

            token.SetBindValue(
                _distanceBind,
                "Distance: --");

            token.SetBindValue(
                _hullBind,
                "Hull: --");

            token.SetBindValue(
                _statusBind,
                "Status: NO ENCOUNTER");

            return;
        }

        token.SetBindValue(
            _targetBind,
            $"Target: {targetShip.ShipName}");

        token.SetBindValue(
            _distanceBind,
            $"Distance: {encounter.Distance:0.00}");

        token.SetBindValue(
            _hullBind,
            $"Hull: {targetShip.Hull}%");

        token.SetBindValue(
            _statusBind,
            "Status: IN COMBAT");
    }

    // -----------------------------------------------------------------
    // Close
    // -----------------------------------------------------------------

    public void Close(
        NwPlayer player)
    {
        if (!_tokens.Remove(
                player.PlayerName,
                out NuiWindowToken token))
        {
            return;
        }

        token.Close();

        player.OnNuiEvent -=
            HandleCombatNuiEvent;

        Log.Info(
            $"Ship Combat NUI closed for player " +
            $"{player.PlayerName}.");
    }

    // -----------------------------------------------------------------
    // Button Helper
    // -----------------------------------------------------------------

    private static NuiButton Button(
        string text,
        string id)
    {
        return new NuiButton(
            NuiProperty<string>.CreateValue(
                text))
        {
            Id = id,
            Width = 140.0f,
            Height = 35.0f
        };
    }

    // -----------------------------------------------------------------
    // Section Header
    // -----------------------------------------------------------------

    private static NuiLabel SectionHeader(
        string text)
    {
        return new NuiLabel(
            NuiProperty<string>.CreateValue(
                text));
    }

    // -----------------------------------------------------------------
    // Info Label
    // -----------------------------------------------------------------

    private static NuiLabel InfoLabel(
        NuiProperty<string> property,
        float height = 22.0f)
    {
        return new NuiLabel(property)
        {
            Height = height
        };
    }

    // -----------------------------------------------------------------
    // Spacer
    // -----------------------------------------------------------------

    private static NuiSpacer Spacer(
        float height = 12.0f)
    {
        return new NuiSpacer
        {
            Height = height
        };
    }
    private bool CanUseShipSpell(
    ShipSpellEffectDefinition definition,
    ShipState ship,
    ShipState? targetShip,
    ShipEncounter? encounter)
{
    if (definition.RequiresEncounter &&
        encounter == null)
    {
        return false;
    }

    if (definition.RequiresEnemyTarget &&
        targetShip == null)
    {
        return false;
    }

    if (definition.MaxRange > 0.0f &&
        encounter != null &&
        encounter.Distance > definition.MaxRange)
    {
        return false;
    }

    return true;
}
}
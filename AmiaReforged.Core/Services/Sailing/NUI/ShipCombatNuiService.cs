using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipCombatNuiService))]
public sealed class ShipCombatNuiService : IDisposable
{
    private const string WindowId =
        "ShipCombatNui";

    private readonly ShipTargetSelection _selection;
    private readonly HostileBoardingService? _hostileBoarding;
    private readonly NuiBind<bool> _hostileEnabled = new("combat_hostile_enabled");
    private readonly NuiBind<string> _hostileReason = new("combat_hostile_reason");
    private NwModule? _module;
    private bool _disposed;

    private readonly Dictionary<
        string,
        NuiWindowToken>
        _tokens = new();

    private readonly NuiBind<string> _targetBind =
        new("combat_target");

    private readonly NuiBind<string> _distanceBind =
        new("combat_distance");

    private readonly NuiBind<bool> _targetEnabled = new("combat_target_enabled");
    private readonly NuiBind<string> _targetNames =
    new("combat_target_names");

    private readonly NuiBind<int> _targetCount =
    new("combat_target_count");

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
        CombatTargetSelectionService selectionService,
        ShipEncounterService shipEncounterService,
        PhysicalShipService physicalShipService,
        ShipSpellEffectService shipSpellEffectService,
        HelmService helmService,
        ShipSpellService shipSpellService, HostileBoardingService? hostileBoarding = null)
    {
        _selection = selectionService.Selection;
        _hostileBoarding = hostileBoarding;
        _physicalShipService = physicalShipService;
        physicalShipService.PlayerDetached += HandlePlayerDetached;
        _shipEncounterService =
            shipEncounterService;

        _shipSpellEffectService =
            shipSpellEffectService;

        _helmService =
            helmService;

        _shipSpellService =
            shipSpellService;

        // shipEncounterService.EncounterStarted +=
        //     OnEncounterStarted;

        //  shipEncounterService.EncounterEnded +=
        //      OnEncounterEnded;
    }

    internal void InitializeModule(NwModule module)
    {
        if (_disposed || _module != null) return;
        _module = module;
    }

    public void RegisterInteractions()
    {
        if (_disposed || _module == null) return;
        foreach (NwPlaceable station in
            NwObject.FindObjectsWithTag<NwPlaceable>(
                "combat_station"))
        {
            station.OnLeftClick -=
                HandleCombatStationClick;

            station.OnLeftClick +=
                HandleCombatStationClick;
        }
    }

    // -----------------------------------------------------------------
    // Encounter Started
    // -----------------------------------------------------------------


    // -----------------------------------------------------------------
    // Open For Ship
    // -----------------------------------------------------------------

    private void OpenForShip(
        ShipState ship,
        ShipState? targetShip,
        ShipEncounter? encounter)
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
        ShipState? targetShip,
        ShipEncounter? encounter)
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
        _selection.Refresh(player.PlayerName, ship, _shipEncounterService.GetNearbyShips(ship));
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
        // Target Selection
        // -------------------------------------------------------------

        List<ShipState> nearbyShips =
            _shipEncounterService
                .GetNearbyShips(ship)
                .ToList();

        List<NuiListTemplateCell> targetCells =
        [
            new(
        new NuiLabel(_targetNames))
    {
        Width = 200f
    },
    new(
        new NuiButton("SELECT")
        {
            Id = "btn_combat_target", Enabled = _targetEnabled
        })
    {
        Width = 80f,
        VariableSize = false
    }
        ];

        NuiList targetList =
            new(
                targetCells,
                _targetCount)
            {
                Width = 300f,
                Height = 120f
            };
        // -------------------------------------------------------------
        // Ship Magic
        // -------------------------------------------------------------

        NuiLabel spellHeader =
            SectionHeader(
                "SHIP MAGIC");

        NuiColumn spellColumn =
            new();

        NwCreature? caster =
            player.ControlledCreature;

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
            targetList);

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
            new NuiButton(
                "REFRESH")
            {
                Id = "btn_combat_refresh"
            });

        column.Children.Add(
            Spacer());

        column.Children.Add(
            spellHeader);

        column.Children.Add(
            spellColumn);

        // -------------------------------------------------------------
        // Window
        // -------------------------------------------------------------

        column.Children.Add(new NuiRow { Children = [Button("ATTACK", "btn_combat_attack"), Button("BOARD", "btn_combat_board")] });
        column.Children.Add(new NuiButton("HOSTILE BOARD") { Id = "btn_combat_hostile_board", Enabled = _hostileEnabled, Height = 35 });
        column.Children.Add(InfoLabel(_hostileReason, 44));

        NuiWindow window =
            new(
                new NuiGroup { Layout = column, Border = false, Scrollbars = NuiScrollbars.Y },
                NuiProperty<string>.CreateValue(
                    "Ship Combat"));

        window.Geometry =
            new NuiRect(
                -1.0f,
                -1.0f,
                500.0f,
                480.0f);

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
        token.SetBindValues(_targetEnabled, _selection.Rows(player.PlayerName).Select(nearbyShips.Contains));
        token.SetBindValue(
            _targetCount,
            _selection.Rows(player.PlayerName).Count);

        token.SetBindValues(
            _targetNames,
            _selection.Rows(player.PlayerName)
                .Select(s => nearbyShips.Contains(s) ? s.ShipName.ToUpper() : $"{s.ShipName} (departed)")
                .ToList());

        player.OnNuiEvent -= HandleCombatNuiEvent;
        player.OnNuiEvent += HandleCombatNuiEvent;

        Update(
            player,
            ship,
            targetShip,
            encounter);

        //Log.Info(
        //$"Ship Combat NUI opened for player " +
        //$"{player.PlayerName}: " +
        //$"{ship.ShipName} -> " +
        //$"{targetShip.ShipName}.");
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
                //Log.Info(
                //$"Ship spell not available to caster: " +
                //$"Caster={caster.Name}, " +
                //$"Spell={definition.DisplayName}, " +
                //$"SpellId={definition.SpellId}.");

                continue;
            }

            availableSpells.Add(
                definition);

            //Log.Info(
            //$"Ship spell available: " +
            //$"Caster={caster.Name}, " +
            //$"Spell={definition.DisplayName}, " +
            //$"SpellId={definition.SpellId}, " +
            //$"Class={castingClass.ClassType}.");
        }

        //Log.Info(
        //$"Ship spell availability complete: " +
        //$"Caster={caster.Name}, " +
        //$"Available={availableSpells.Count}.");

        return availableSpells;
    }
    // -----------------------------------------------------------------
    // Selected Combat Target
    // -----------------------------------------------------------------

    private ShipState? GetSelectedTarget(NwPlayer player)
    {
        string? sourceName = _physicalShipService.GetShipForPlayer(player.PlayerName);
        ShipState? source = sourceName == null ? null : _helmService.GetShip(sourceName);
        return source == null ? null : _selection.GetSelected(player.PlayerName, source, _shipEncounterService.GetNearbyShips(source));
    }
    public ShipState? GetSelectedCombatTarget(NwPlayer player) => GetSelectedTarget(player);

    private void HandlePlayerDetached(string ship, string playerName)
    {
        if (!_selection.ClearForSource(playerName, ship)) return;
        NwPlayer? player = NwModule.Instance.Players.FirstOrDefault(p => p.PlayerName == playerName);
        if (player != null) Close(player);
        else _tokens.Remove(playerName);
    }

    // -----------------------------------------------------------------
    // Combat NUI Event
    // -----------------------------------------------------------------

    private void HandleCombatNuiEvent(
        ModuleEvents.OnNuiEvent obj)
    {
        if (!_tokens.TryGetValue(obj.Player.PlayerName, out NuiWindowToken token) || token.Token != obj.Token.Token)
        {
            return;
        }

        if (obj.EventType == NuiEventType.Close)
        {
            Close(obj.Player);
            return;
        }
        if (obj.EventType != NuiEventType.Click)
        {
            return;
        }
        if (obj.ElementId is "btn_combat_attack" or "btn_combat_board" or "btn_combat_hostile_board")
        {
            string? sourceName = _physicalShipService.GetShipForPlayer(obj.Player.PlayerName);
            ShipState? source = sourceName == null ? null : _helmService.GetShip(sourceName);
            if (source == null) return;
            ShipState? target = GetSelectedCombatTarget(obj.Player);
            if (obj.ElementId == "btn_combat_attack") _ = _helmService.AttackTarget(source.ShipName, obj.Player, target);
            else if (obj.ElementId == "btn_combat_hostile_board")
            { if (_hostileBoarding != null) _ = _hostileBoarding.BoardAsync(obj.Player, source, target); }
            else _helmService.RequestBoarding(source.ShipName, obj.Player, target);
            return;
        }
        if (obj.ElementId == "btn_combat_target")
        {
            SelectCombatTarget(
                obj.Player,
                obj.ArrayIndex);

            return;
        }
        if (obj.ElementId == "btn_combat_refresh")
        {
            RebuildCombatWindow(
                obj.Player);

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
    //rebuild combat ui
    private void RebuildCombatWindow(NwPlayer player)
    {
        string? sourceName = _physicalShipService.GetShipForPlayer(player.PlayerName);
        ShipState? source = sourceName == null ? null : _helmService.GetShip(sourceName);
        if (source == null) { Close(player); return; }
        ShipState? selected = GetSelectedCombatTarget(player);
        // Retain the selection while changing the window token; prior-token clicks are rejected.
        player.OnNuiEvent -= HandleCombatNuiEvent;
        if (_tokens.Remove(player.PlayerName, out NuiWindowToken previous)) SailingWindowLifecycle.Close(previous);
        ShipEncounter? encounter = null;
        if (selected != null) _shipEncounterService.TryGetEncounter(source, selected, out encounter);
        Open(player, source, selected, encounter);
    }
    //target
    private void SelectCombatTarget(NwPlayer player, int index)
    {
        string? sourceName = _physicalShipService.GetShipForPlayer(player.PlayerName);
        ShipState? source = sourceName == null ? null : _helmService.GetShip(sourceName);
        if (source != null) _selection.Select(player.PlayerName, source, index, _shipEncounterService.GetNearbyShips(source));
        RefreshCombatWindow(player);
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

        ShipState? selectedTarget = null;

        if (definition.RequiresEncounter)
        {
            selectedTarget =
                GetSelectedCombatTarget(player);

            if (selectedTarget == null ||
                !_shipEncounterService.TryGetEncounter(
                    ship,
                    selectedTarget,
                    out ShipEncounter? encounter) ||
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
        // Execute the sailing spell.
        // -------------------------------------------------------------

        _shipSpellService.TryCastFromCombat(player, caster, spell, selectedTarget);

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

        if (shipName == null) { Close(player); return; }

        ShipState? ship =
            _helmService.GetShip(
                shipName);

        if (ship == null || !ship.IsActiveWorld) { Close(player); return; }
        List<ShipState> nearbyShips =
            _shipEncounterService
                .GetNearbyShips(ship)
                .ToList();

        _selection.Refresh(player.PlayerName, ship, nearbyShips);
        IReadOnlyList<ShipState> displayed = _selection.Rows(player.PlayerName);
        NuiWindowToken token = _tokens[player.PlayerName];

        token.SetBindValues(_targetEnabled, _selection.Rows(player.PlayerName).Select(nearbyShips.Contains));
        token.SetBindValue(
            _targetCount,
            _selection.Rows(player.PlayerName).Count);

        token.SetBindValues(
            _targetNames,
            _selection.Rows(player.PlayerName)
                .Select(s => nearbyShips.Contains(s) ? s.ShipName.ToUpper() : $"{s.ShipName} (departed)")
                .ToList());
        ShipState? targetShip = GetSelectedTarget(player);
        ShipEncounter? encounter = null;

        if (targetShip != null)
        {
            _shipEncounterService.TryGetEncounter(
                ship,
                targetShip,
                out encounter);
        }

        if (targetShip == null ||
            encounter == null)
        {
            Update(
                player,
                ship,
                null,
                null);

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

        var hostile = _hostileBoarding?.Eligibility(player, ship, GetSelectedCombatTarget(player)) ??
            HostileBoardingEligibility.Denied("Hostile boarding unavailable");
        token.SetBindValue(_hostileEnabled, hostile.Allowed);
        token.SetBindValue(_hostileReason, hostile.Reason);

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
            $"Hull: {targetShip.Hull}/{targetShip.MaximumHull}");

        token.SetBindValue(
            _statusBind,
            "Status: IN COMBAT");
    }

    // -----------------------------------------------------------------
    // Close
    // -----------------------------------------------------------------

    public void Close(NwPlayer player)
    {
        player.OnNuiEvent -= HandleCombatNuiEvent;
        _selection.Clear(player.PlayerName);
        if (_tokens.Remove(player.PlayerName, out NuiWindowToken token)) SailingWindowLifecycle.Close(token);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _physicalShipService.PlayerDetached -= HandlePlayerDetached;
        foreach (NwPlayer player in _tokens.Values.Select(t => t.Player).ToArray()) Close(player);
        _tokens.Clear();
        if (_module?.IsValid == true)
            foreach (NwPlaceable station in NwObject.FindObjectsWithTag<NwPlaceable>("combat_station")) station.OnLeftClick -= HandleCombatStationClick;
        _module = null;
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
    private void HandleCombatStationClick(
        PlaceableEvents.OnLeftClick obj)
    {
        NwPlayer player =
            obj.ClickedBy;

        //Log.Info(
        //$"Combat station clicked: " +
        //$"Player={player.PlayerName}, " +
        //$"Tag={obj.Placeable.Tag}, " +
        //$"ResRef={obj.Placeable.ResRef}.");

        string? shipName =
            _physicalShipService.GetShipForPlayer(
                player.PlayerName);

        if (shipName == null)
        {
            player.SendServerMessage(
                "You are not aboard a ship.",
                ColorConstants.Orange);

            return;
        }

        ShipState? ship =
            _helmService.GetShip(
                shipName);

        if (ship == null)
        {
            Log.Warn(
                $"Combat station clicked but ship state " +
                $"could not be found: " +
                $"Player={player.PlayerName}, " +
                $"Ship={shipName}.");

            return;
        }

        ShipState? targetShip = GetSelectedCombatTarget(player);
        ShipEncounter? encounter = null;
        if (targetShip != null) _shipEncounterService.TryGetEncounter(ship, targetShip, out encounter);

        Open(
            player,
            ship,
            targetShip,
            encounter);
    }
    public void RefreshAll()
    {
        //Log.Info(
        //$"Combat NUI RefreshAll: OpenWindows={_tokens.Count}");
        foreach (string playerName in _tokens.Keys.ToList())
        {
            NwPlayer? player =
                NwModule.Instance.Players.FirstOrDefault(
                    p =>
                        string.Equals(
                            p.PlayerName,
                            playerName,
                            StringComparison.Ordinal));

            if (player == null)
            {
                _tokens.Remove(playerName);
                _selection.Clear(playerName);
                continue;
            }

            try { RefreshCombatWindow(player); } catch (Exception ex) { Log.Error(ex, "Combat refresh failed for {0}.", playerName); }
        }
    }
}

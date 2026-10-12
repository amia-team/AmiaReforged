using System.Text;
using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
using NLog;
namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(SailingNuiService))]
public class SailingNuiService : IDisposable
{
    private const string WindowId = "sailing";
    private const float MapWorldSize =
        640.0f;

    private const int MapCells =
        16;

    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private readonly Dictionary<string, NuiWindowToken> _tokens =
        new();

    private readonly Dictionary<string, NuiGroup> _mapGroups = new();
    private readonly ShipTargetSelection _selection = new();
    private readonly ShipRegistryService _registry;
    private readonly Dictionary<string, SailingMapView> _mapViews = new();
    private readonly NuiBind<string> _targetNames = new("sailing_target_names");
    private readonly NuiBind<bool> _targetEnabled = new("sailing_target_enabled");
    private readonly NuiBind<int> _targetCount = new("sailing_target_count");

    public bool OwnsEvent(Anvil.API.Events.ModuleEvents.OnNuiEvent evt) =>
        _tokens.TryGetValue(evt.Player.PlayerName, out NuiWindowToken token) && token.Token == evt.Token.Token;

    public ShipState? GetSelectedSailingTarget(NwPlayer player, ShipState source) =>
        _selection.GetSelected(player.PlayerName, source, _shipEncounterService.GetNearbyShips(source));

    public void SelectTarget(NwPlayer player, ShipState source, int row) =>
        _selection.Select(player.PlayerName, source, row, _shipEncounterService.GetNearbyShips(source));

    // ---------------------------------------------------------------------
    // Binds
    // ---------------------------------------------------------------------
    private readonly NuiBind<string> _areaBind =
        new("ship_area");

    private readonly NuiBind<string> _positionBind =
        new("ship_position");

    private readonly NuiBind<string> _headingBind =
        new("ship_heading");

    private readonly NuiBind<string> _statusBind =
        new("ship_status");

    private readonly NuiBind<string> _hullBind =
        new("ship_hull");

    private readonly NuiBind<string> _weaponBind =
        new("ship_weapon");

    private readonly NuiBind<string> _weaponStatsBind =
        new("ship_weapon_stats");

    private readonly NuiBind<string> _weaponStatusBind =
        new("ship_weapon_status");

    private readonly NuiBind<string> _combatMessageBind =
        new("ship_combat_message");

    private readonly NuiBind<string> _encounterBind =
        new("ship_encounter");

    private readonly NuiBind<string> _encounterDistanceBind =
        new("ship_encounter_distance");

    private readonly NuiBind<string> _nearbyShipsBind =
        new("ship_nearby_ships");

    private readonly NuiBind<string> _boardingBind =
    new("ship_boarding");

    private readonly NuiBind<string> _horizonBind =
        new("ship_horizon");
    private readonly NuiBind<bool> _dockEnabledBind =
        new("dock_enabled");

    //private readonly OceanContactService
    //    _oceanContactService;

    // ---------------------------------------------------------------------
    // Services
    // ---------------------------------------------------------------------

    private readonly ShipEncounterService
    _shipEncounterService;

    private readonly ShipBoardingService
        _shipBoardingService;

    private readonly ShipCombatService
        _shipCombatService;

    private readonly ShipObstacleService
        _shipObstacleService;

    private readonly ShipNavigationService
        _shipNavigationService;

    private readonly HorizonContactService
        _horizonContactService;

    private readonly OceanContactService
        _oceanContactService;

    private readonly ChartMarkerService _chartMarkerService;

    private readonly ShipVisibilityService
    _shipVisibilityService;

    private readonly ChartLandmarkService _chartLandmarkService;

    private readonly ChartDiscoveryService _chartDiscoveryService;
    //private readonly HelmService _helmService;
    // ---------------------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------------------

    public SailingNuiService(
    ShipRegistryService registry,
    ShipEncounterService shipEncounterService,
    ShipBoardingService shipBoardingService,
    ShipCombatService shipCombatService,
    ShipObstacleService shipObstacleService,
    HorizonContactService horizonContactService,
    OceanContactService oceanContactService,
    ShipVisibilityService shipVisibilityService,
    ChartMarkerService chartMarkerService,
    ChartDiscoveryService chartDiscoveryService,
    ChartLandmarkService chartLandmarkService,
    //HelmService helmService,
    ShipNavigationService shipNavigationService)
    {
        _registry = registry;
        _shipEncounterService =
            shipEncounterService;

        _shipBoardingService =
            shipBoardingService;

        _shipCombatService =
            shipCombatService;

        _shipObstacleService =
            shipObstacleService;

        _shipNavigationService =
            shipNavigationService;

        _horizonContactService =
            horizonContactService;

        _oceanContactService =
            oceanContactService;

        _chartDiscoveryService = chartDiscoveryService;

        //_helmService = helmService;
        _shipVisibilityService = shipVisibilityService;

        _chartMarkerService = chartMarkerService;

        _chartLandmarkService = chartLandmarkService;

        //Log.Info(
        //"Sailing NUI Service initialized.");
    }
    private static string GetShipImage(Heading heading)
    {
        return heading switch
        {
            Heading.North => "n",
            Heading.NorthEast => "ne",
            Heading.East => "e",
            Heading.SouthEast => "se",
            Heading.South => "s",
            Heading.SouthWest => "sw",
            Heading.West => "w",
            Heading.NorthWest => "nw",
            _ => "e",
        };
    }

    private static string GetShipImage(ShipState ship)
    {
        string prefix =
            string.IsNullOrWhiteSpace(ship.SpritePrefix)
                ? "ship"
                : ship.SpritePrefix;

        return $"{prefix}_{GetShipImage(ship.Heading)}";
    }
    private static string GetHorizonIcon(
        ShipState ship)
    {
        return ship.ShipType switch
        {
            ShipType.Pirate => "pirate",
            ShipType.Merchant => "merchant",
            ShipType.Navy => "navy",
            _ => "unknown",
        };
    }
    // ---------------------------------------------------------------------
    // UI Helpers
    // ---------------------------------------------------------------------

    private static NuiLabel Spacer(
    float height = 8.0f)
    {
        NuiLabel spacer = new(
            NuiProperty<string>.CreateValue(
                " "));

        spacer.Height =
            height;

        return spacer;
    }

    private static NuiLabel SectionHeader(
        string text)
    {
        NuiLabel header = new(
            NuiProperty<string>.CreateValue(
                $"========== {text} =========="));

        header.Height =
            28.0f;

        return header;
    }

    private static NuiLabel InfoLabel(
        NuiProperty<string> property,
        float height = 24.0f)
    {
        NuiLabel label = new(
            property);

        label.Height =
            height;

        return label;
    }

    private static NuiButton Button(
        string text,
        string id)
    {
        NuiButton button = new(
            NuiProperty<string>.CreateValue(
                text));

        button.Id =
            id;

        button.Height =
            44.0f;

        return button;
    }

    // ---------------------------------------------------------------------
    // Open
    // ---------------------------------------------------------------------

    public void Open(
        NwPlayer player,
        ShipState ship)
    {
        // -----------------------------------------------------------------
        // Header
        // -----------------------------------------------------------------

        NuiLabel title =
            InfoLabel(
                NuiProperty<string>.CreateValue(
                    ship.ShipName.ToUpper()),
                30.0f);

        NuiLabel subtitle =
            InfoLabel(
                NuiProperty<string>.CreateValue(
                    "SHIP COMMAND"),
                24.0f);

        NuiLabel status =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "ship_status"));

        NuiLabel hull =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "ship_hull"));

        // -----------------------------------------------------------------
        // Navigation
        // -----------------------------------------------------------------

        NuiLabel navigationHeader =
            SectionHeader(
                "NAVIGATION");

        NuiLabel area =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "ship_area"));

        NuiLabel position =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "ship_position"));

        NuiLabel heading =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "ship_heading"));

        /*
         * The map uses the same 640 x 640 coordinate space
         * as the sailing system.
         *
         * The label is intentionally tall enough to display
         * the complete 16 x 16 map.
         */
        NuiRow mapCanvas = new()
        {
            Width = 800f,
            Height = 800f,
            DrawList =
            [
                new NuiDrawListImage(
            "sailing_map",
            new NuiRect(0f, 0f, 800f, 800f))
            ]
        };
        NuiGroup mapGroup = new NuiGroup
        {
            Id = "sailing_map_group",
            Width = 800f,
            Height = 800f,
            Layout = mapCanvas
        };
        NuiButton leftButton =
            Button(
                "LEFT",
                "left_button");

        NuiButton aheadButton =
            Button(
                "AHEAD",
                "ahead_button");
        NuiButton stopButton =
            Button(
             "STOP",
            "stop_button");

        NuiButton rightButton =
            Button(
                "RIGHT",
                "right_button");

        NuiButton dockButton =
        new("DOCK")
        {
            Id = "dock_button"
        };

        dockButton.Enabled =
            _dockEnabledBind;

        NuiButton asternButton =
        Button(
            "ASTERN",
            "astern_button");



        NuiButton testNavigationButton =
Button(
    "TEST AUTONOMOUS NAV",
    "test_navigation_button");

        NuiRow movementRowOne =
            new();

        movementRowOne.Children.Add(
            leftButton);

        movementRowOne.Children.Add(
            aheadButton);

        movementRowOne.Children.Add(
            rightButton);

        NuiRow movementRowTwo =
            new();

        movementRowTwo.Children.Add(
    asternButton);
        movementRowTwo.Children.Add(stopButton);

        NuiRow movementRowThree =
            new();

        movementRowThree.Children.Add(
            dockButton);
        // -----------------------------------------------------------------

        // Horizon
        // -----------------------------------------------------------------

        NuiLabel horizonHeader =
    SectionHeader(
        "HORIZON");

        NuiLabel horizon =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "ship_horizon"),
                32.0f);
        // -----------------------------------------------------------------
        // Encounter
        // -----------------------------------------------------------------


        NuiLabel encounterHeader =
            SectionHeader(
                "ENCOUNTER");

        NuiLabel encounter =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "ship_encounter"));

        NuiLabel encounterDistance =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "ship_encounter_distance"));

        // -----------------------------------------------------------------
        // Boarding
        // -----------------------------------------------------------------

        NuiLabel boardingHeader =
            SectionHeader(
                "BOARDING");

        NuiLabel boarding =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "ship_boarding"),
                32.0f);

        NuiButton boardButton =
            Button(
                "BOARD",
                "board_button");

        NuiButton acceptButton =
            Button(
                "ACCEPT",
                "accept_board_button");

        NuiButton rejectButton =
            Button(
                "REJECT",
                "reject_board_button");

        NuiRow boardingRow =
            new();

        boardingRow.Children.Add(
            boardButton);

        boardingRow.Children.Add(
            acceptButton);

        boardingRow.Children.Add(
            rejectButton);

        // -----------------------------------------------------------------
        // Weapons
        // -----------------------------------------------------------------

        NuiLabel weaponHeader =
            SectionHeader(
                "WEAPONS");

        NuiLabel weapon =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "ship_weapon"));

        NuiLabel weaponStats =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "ship_weapon_stats"),
                28.0f);

        NuiLabel weaponStatus =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "ship_weapon_status"),
                28.0f);

        NuiButton cannonButton =
            Button(
                "CANNON",
                "weapon_cannon_button");

        NuiButton ballistaButton =
            Button(
                "BALLISTA",
                "weapon_ballista_button");

        NuiButton catapultButton =
            Button(
                "CATAPULT",
                "weapon_catapult_button");

        NuiButton heavyCannonButton =
            Button(
                "HEAVY CANNON",
                "weapon_heavy_cannon_button");

        NuiRow weaponRowOne =
            new();

        weaponRowOne.Children.Add(
            cannonButton);

        weaponRowOne.Children.Add(
            ballistaButton);

        NuiRow weaponRowTwo =
            new();

        weaponRowTwo.Children.Add(
            catapultButton);

        weaponRowTwo.Children.Add(
            heavyCannonButton);

        // -----------------------------------------------------------------
        // Actions
        // -----------------------------------------------------------------

        NuiLabel actionHeader =
            SectionHeader(
                "ACTIONS");

        NuiButton hailButton =
            Button(
                "HAIL",
                "hail_button");

        NuiButton tradeButton =
            Button(
                "TRADE",
                "trade_button");

        NuiButton attackButton =
            Button(
                "ATTACK",
                "attack_button");

        NuiButton repairButton =
            Button(
                "REPAIR",
                "repair_button");

        NuiRow actionRow =
            new();

        actionRow.Children.Add(
        hailButton);

        actionRow.Children.Add(
            tradeButton);
        actionRow.Children.Add(Button("PORT MARKET", "port_market_button"));

        actionRow.Children.Add(
            attackButton);

        actionRow.Children.Add(
            repairButton);
        // -----------------------------------------------------------------
        // Combat log
        // -----------------------------------------------------------------

        NuiLabel combatHeader =
            SectionHeader(
                "COMBAT LOG");

        NuiLabel combatMessage =
            InfoLabel(
                NuiProperty<string>.CreateBind(
                    "ship_combat_message"),
                56.0f);

        NuiButton leaveButton =
            Button(
                "LEAVE HELM",
                "leave_button");
        // -----------------------------------------------------------------
        // Merchant Trade
        // -----------------------------------------------------------------


        // -----------------------------------------------------------------
        // Single-column layout
        // -----------------------------------------------------------------

        NuiColumn column =
            new();

        column.Children.Add(
            title);

        column.Children.Add(
            subtitle);

        column.Children.Add(
            Spacer());

        column.Children.Add(
            status);

        column.Children.Add(
            hull);

        column.Children.Add(
            Spacer());

        column.Children.Add(new NuiList(
            [new NuiListTemplateCell(new NuiLabel(_targetNames)) { Width = 200f },
             new NuiListTemplateCell(new NuiButton("SELECT") { Id = "sailing_target_select", Enabled = _targetEnabled }) { Width = 90f }],
            _targetCount)
        { Height = 100f });

        // Navigation
        column.Children.Add(
            navigationHeader);

        column.Children.Add(
            area);

        column.Children.Add(
            position);

        column.Children.Add(
            heading);

        column.Children.Add(mapGroup);

        column.Children.Add(
            movementRowOne);

        column.Children.Add(
            movementRowTwo);

        column.Children.Add(
        movementRowThree);

        column.Children.Add(
        testNavigationButton);

        column.Children.Add(
            Spacer(12.0f));

        column.Children.Add(Button("HAIL CONTACT", "hail_contact_button"));
        // Horizon
        column.Children.Add(
        horizonHeader);

        column.Children.Add(
        horizon);

        column.Children.Add(
        Spacer(12.0f));

        // Encounter
        column.Children.Add(
            encounterHeader);

        column.Children.Add(
            encounter);

        column.Children.Add(
            encounterDistance);

        column.Children.Add(
            Spacer(12.0f));

        // Boarding
        column.Children.Add(
            boardingHeader);

        column.Children.Add(
            boarding);

        column.Children.Add(
            boardingRow);

        column.Children.Add(
            Spacer(12.0f));

        // Weapons
        column.Children.Add(
            weaponHeader);

        column.Children.Add(
            weapon);

        column.Children.Add(
            weaponStats);

        column.Children.Add(
            weaponStatus);

        column.Children.Add(
            Spacer(8.0f));

        column.Children.Add(
            weaponRowOne);

        column.Children.Add(
            weaponRowTwo);

        column.Children.Add(
            Spacer(12.0f));

        // Actions
        column.Children.Add(
            actionHeader);

        column.Children.Add(
            actionRow);

        column.Children.Add(
            Spacer(12.0f));
        //trade
        column.Children.Add(
    Spacer(12.0f));


        // Combat
        column.Children.Add(
        combatHeader);

        column.Children.Add(
            combatMessage);

        column.Children.Add(
            Spacer(8.0f));

        column.Children.Add(
            leaveButton);



        // -----------------------------------------------------------------
        // Window
        // -----------------------------------------------------------------

        NuiWindow window =
            new(
                column,
                NuiProperty<string>.CreateValue(
                    "Sailing"));

        window.Geometry =
            new NuiRect(
                -1.0f,
                -1.0f,
                700.0f,
                960.0f);

        window.Closable =
            true;

        if (!player.TryCreateNuiWindow(
                window,
                out NuiWindowToken token,
                WindowId))
        {
            Log.Error(
                $"Failed to create sailing NUI " +
                $"for player {player.PlayerName}.");

            return;
        }

        _tokens[player.PlayerName] =
            token;
        _mapGroups[player.PlayerName] = mapGroup;

        Update(
            player,
            ship);

        //Log.Info(
        //$"Sailing NUI opened for player " +
        //$"{player.PlayerName}.");


    }

    // ---------------------------------------------------------------------
    // Update
    // ---------------------------------------------------------------------

    public void Update(
        NwPlayer player,
        ShipState ship,
        IReadOnlyCollection<ShipState>? ships = null)
    {
        ships ??= _registry.GetShips();

        if (!_tokens.TryGetValue(
                player.PlayerName,
                out NuiWindowToken token))
        {
            Log.Warn(
                $"Sailing NUI update skipped: " +
                $"Player={player.PlayerName}, " +
                "no active NUI token.");

            return;
        }

        //Log.Info(
        //$"Sailing NUI updating: " +
        //$"Player={player.PlayerName}, " +
        //$"Ship={ship.ShipName}, " +
        //$"Area={ship.AreaResRef}, " +
        //$"X={ship.X:0.00}, " +
        //$"Y={ship.Y:0.00}, " +
        //$"Heading={ship.Heading}");

        // -----------------------------------------------------------------
        // Navigation
        // -----------------------------------------------------------------

        token.SetBindValue(
            _areaBind,
            $"Area: {ship.AreaResRef}");

        token.SetBindValue(
            _positionBind,
            $"Position: X {ship.X:0}  |  Y {ship.Y:0}  |  Z {ship.Z:0}");

        token.SetBindValue(
            _headingBind,
            $"Heading: {ship.Heading}");


        // -----------------------------------------------------------------
        // Sailing Map
        // -----------------------------------------------------------------

        SailingMapView view = new(ship.ShipId, ship.AreaResRef, ship.X, ship.Y, ship.Heading, ship.SpritePrefix,
            _chartDiscoveryService.GetRevision(player), _shipVisibilityService.GetVisibleShips(ship, ships)
                .Select(c => new SailingMapContact(c.Ship.ShipId, c.Ship.X, c.Ship.Y, c.Ship.Heading, c.Ship.SpritePrefix))
                .OrderBy(c => c.ShipId, StringComparer.Ordinal).ToArray());
        if ((!_mapViews.TryGetValue(player.PlayerName, out SailingMapView? previous) || !previous.Matches(view)) &&
            _mapGroups.TryGetValue(player.PlayerName, out NuiGroup? mapGroup))
        {
            token.SetGroupLayout(mapGroup, BuildMapCanvas(player, ship, ships));
            _mapViews[player.PlayerName] = view;
        }

        // -----------------------------------------------------------------
        // Horizon
        // -----------------------------------------------------------------

        token.SetBindValue(
            _horizonBind,
            _horizonContactService.BuildHorizonString(ship));





        // -----------------------------------------------------------------
        // Hull
        // -----------------------------------------------------------------

        token.SetBindValue(
            _hullBind,
            $"Hull Integrity: {ship.Hull}/{ship.MaximumHull}");

        // -----------------------------------------------------------------
        // Weapon
        // -----------------------------------------------------------------

        ShipWeapon weapon =
            _shipCombatService.GetWeapon(
                ship);

        token.SetBindValue(
            _weaponBind,
            $"Equipped: {weapon.DisplayName}");

        token.SetBindValue(
            _weaponStatsBind,
            $"DMG {weapon.Damage}  |  " +
            $"RNG {weapon.MaxRange:0.0}  |  " +
            $"CD {weapon.Cooldown.TotalSeconds:0.0}s  |  " +
            $"ARC {weapon.Arc}");

        TimeSpan cooldown =
            _shipCombatService.GetCooldownRemaining(
                ship);

        if (cooldown > TimeSpan.Zero)
        {
            token.SetBindValue(
                _weaponStatusBind,
                $"Weapon Status: RELOADING " +
                $"({cooldown.TotalSeconds:0.0}s)");
        }
        else
        {
            token.SetBindValue(
                _weaponStatusBind,
                ship.AllowsWeapons && ship.WeaponResRef != "" ? "Weapon Status: READY" : "Weapon Status: UNARMED");
        }

        token.SetBindValue(_dockEnabledBind, ship.IsActiveWorld && !ship.IsDisabled && ship.CanDock);
        List<ShipState> nearby = _shipEncounterService.GetNearbyShips(ship).ToList();
        _selection.Refresh(player.PlayerName, ship, nearby);
        token.SetBindValues(_targetNames, _selection.Rows(player.PlayerName).Select(s => nearby.Contains(s) ? s.ShipName : $"{s.ShipName} (departed)").ToList());
        token.SetBindValues(_targetEnabled, _selection.Rows(player.PlayerName).Select(nearby.Contains));
        token.SetBindValue(_targetCount, _selection.Rows(player.PlayerName).Count);
        ShipState? selected = GetSelectedSailingTarget(player, ship);
        token.SetBindValue(_statusBind, ship.IsDisabled ? "Status: DISABLED" :
            nearby.Count > 0 ? "Status: ENCOUNTER" : ship.Underway ? "Status: UNDERWAY" : "Status: STOPPED");
        token.SetBindValue(_encounterBind, selected == null ? "Target: NONE" : $"Target: {selected.ShipName}");
        token.SetBindValue(_encounterDistanceBind, selected == null ? "Distance: --" :
            $"Distance: {ShipActionRules.Distance(ship, selected):0.0}m");
        if (_shipBoardingService.TryGetRequestForPlayer(player.PlayerName, out ShipBoardingRequest? incoming) && incoming != null)
            token.SetBindValue(_boardingBind, $"BOARDING REQUEST: {incoming.RequestingShip.ShipName}");
        else token.SetBindValue(_boardingBind, _shipBoardingService.HasRequestFromPlayer(player.PlayerName) ?
            "BOARDING REQUEST SENT" : "Boarding: NONE");
    }

    // ---------------------------------------------------------------------
    // Live Sailing Map
    // ---------------------------------------------------------------------

    private NuiRow BuildMapCanvas(
        NwPlayer player,
        ShipState ship,
        IReadOnlyCollection<ShipState>? ships = null)
    {
        ships ??= _registry.GetShips();
        //Log.Info(
        //$"BuildMapCanvas: Viewer={ship.ShipName}, ShipCount={ships.Count}");
        float playerDrawX =
            (ship.X / MapWorldSize) * 800.0f;

        float playerDrawY =
            800.0f -
            ((ship.Y / MapWorldSize) * 800.0f);

        List<NuiDrawListItem> drawList =
        [
            new NuiDrawListImage(
            "sailing_map",
            new NuiRect(
                0.0f,
                0.0f,
                800.0f,
                800.0f))
        ];

        foreach (ChartLandmark landmark in _chartLandmarkService.GetLandmarks(ship.AreaResRef))
        {
            if (string.IsNullOrWhiteSpace(landmark.Sprite) || !float.IsFinite(landmark.X) || !float.IsFinite(landmark.Y)) continue;
            drawList.Add(new NuiDrawListImage(landmark.Sprite, new NuiRect(
                landmark.X / MapWorldSize * 800f - landmark.Width / 2,
                800f - landmark.Y / MapWorldSize * 800f - landmark.Height / 2, landmark.Width, landmark.Height)));
        }
        // Fog is drawn above landmarks so undiscovered parts remain covered.

        // -------------------------------------------------------------
        // Debug fog-of-war overlay.
        // -------------------------------------------------------------
        const float cellSize = 800f / 16f;

        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                if (_chartDiscoveryService.IsDiscovered(
                        player,
                        ship.AreaResRef,
                        x,
                        y))
                {
                    continue;
                }

                drawList.Add(
                    new NuiDrawListImage(
                        "fog_cell",
                        new NuiRect(
                            x * cellSize,
                            y * cellSize,
                            cellSize,
                            cellSize)));
            }
        }


        // -------------------------------------------------------------
        // Draw permanent chart markers.
        // -------------------------------------------------------------
        foreach (ChartMarker marker in _chartMarkerService.GetMarkers(ship.AreaResRef))
        {
            int gx = Math.Clamp((int)(marker.X / 40f), 0, 15), gy = 15 - Math.Clamp((int)(marker.Y / 40f), 0, 15);
            if (!_chartDiscoveryService.IsDiscovered(player, ship.AreaResRef, gx, gy) || string.IsNullOrWhiteSpace(marker.Sprite)) continue;
            drawList.Add(new NuiDrawListImage(marker.Sprite, new NuiRect(marker.X / MapWorldSize * 800f,
                800f - marker.Y / MapWorldSize * 800f, marker.Size, marker.Size)));
        }

        //Log.Info($"BuildMapCanvas: Viewer={ship.ShipName}, ShipCount={ships.Count}");

        foreach (VisibleShipContact contact in
                 _shipVisibilityService.GetVisibleShips(ship, ships))
        {
            float drawX =
                (contact.Ship.X / MapWorldSize) * 800.0f;

            float drawY =
                800.0f -
                ((contact.Ship.Y / MapWorldSize) * 800.0f);

            drawList.Add(
                new NuiDrawListImage(
                    GetShipImage(contact.Ship),
                    new NuiRect(
                        drawX - 10.0f,
                        drawY - 10.0f,
                        20.0f,
                        20.0f)));
        }

        // -------------------------------------------------------------
        // Draw the player's ship last so it stays on top.
        // -------------------------------------------------------------
        drawList.Add(
            new NuiDrawListImage(
                GetShipImage(ship),
                new NuiRect(
                    playerDrawX - 16.0f,
                    playerDrawY - 16.0f,
                    32.0f,
                    32.0f)));

        return new NuiRow
        {
            Width = 800.0f,
            Height = 800.0f,
            DrawList = drawList
        };
    }
    // ---------------------------------------------------------------------
    // Debug / Text Sailing Map
    // ---------------------------------------------------------------------

    private string BuildSailingMap(
        ShipState ship)
    {
        char[,] map =
            new char[MapCells, MapCells];

        for (int y = 0;
             y < MapCells;
             y++)
        {
            for (int x = 0;
                 x < MapCells;
                 x++)
            {
                map[x, y] = '.';
            }
        }

        // -------------------------------------------------------------
        // Obstacles
        // -------------------------------------------------------------

        for (int y = 0;
             y < MapCells;
             y++)
        {
            for (int x = 0;
                 x < MapCells;
                 x++)
            {
                float mapX =
                    (x * 10.0f) + 5.0f;

                float mapY =
                    (y * 10.0f) + 5.0f;

                if (_shipObstacleService.GetObstacleAt(
                        ship.AreaResRef,
                        mapX,
                        mapY) != null)
                {
                    map[x, y] = '#';
                }
            }
        }

        // -------------------------------------------------------------
        // Ocean Contacts
        // -------------------------------------------------------------

        foreach (OceanContact contact
                 in _oceanContactService.GetVisibleContacts(ship))
        {
            int contactX =
                Math.Clamp(
                    (int)(contact.X / 10.0f),
                    0,
                    MapCells - 1);

            int contactY =
                Math.Clamp(
                    (int)(contact.Y / 10.0f),
                    0,
                    MapCells - 1);

            map[contactX, contactY] =
                contact.Type switch
                {
                    EncounterType.Pirate => 'P',
                    EncounterType.Merchant => 'M',
                    EncounterType.Wreck => 'W',
                    EncounterType.Whirlpool => '~',
                    EncounterType.SeaSerpent => 'S',
                    _ => '?'
                };
        }

        // -------------------------------------------------------------
        // Nearby Ships
        // -------------------------------------------------------------

        foreach (ShipState nearbyShip
                 in _shipEncounterService.GetNearbyShips(ship))
        {
            int nearbyX =
                Math.Clamp(
                    (int)(nearbyShip.X / 10.0f),
                    0,
                    MapCells - 1);

            int nearbyY =
                Math.Clamp(
                    (int)(nearbyShip.Y / 10.0f),
                    0,
                    MapCells - 1);

            map[nearbyX, nearbyY] =
                'S';
        }

        // -------------------------------------------------------------
        // Ship
        // -------------------------------------------------------------

        int shipX =
            Math.Clamp(
                (int)(ship.X / 10.0f),
                0,
                MapCells - 1);

        int shipY =
            Math.Clamp(
                (int)(ship.Y / 10.0f),
                0,
                MapCells - 1);

        map[shipX, shipY] =
            GetHeadingSymbol(
                ship.Heading);

        // -------------------------------------------------------------
        // Build map
        // -------------------------------------------------------------

        StringBuilder result =
            new();

        result.AppendLine(
            "       NORTH");

        result.AppendLine(
            "   +-----------------+");

        for (int y = MapCells - 1;
             y >= 0;
             y--)
        {
            result.Append(
                "| ");

            for (int x = 0;
                 x < MapCells;
                 x++)
            {
                result.Append(
                    map[x, y]);

                result.Append(
                    ' ');
            }

            result.AppendLine(
                "|");
        }

        result.AppendLine(
            "   +-----------------+");

        result.AppendLine(
            "     0 0 0 0 0 0 0 0");

        result.AppendLine();

        result.Append(
            $"X: {ship.X:0.0}  " +
            $"Y: {ship.Y:0.0}  " +
            $"Heading: {ship.Heading}");

        return result.ToString();
    }

    // ---------------------------------------------------------------------
    // Heading Symbol
    // ---------------------------------------------------------------------

    private static char GetHeadingSymbol(
        Heading heading)
    {
        return heading switch
        {
            Heading.North =>
                '^',

            Heading.NorthEast =>
                '/',

            Heading.East =>
                '>',

            Heading.SouthEast =>
                '\\',

            Heading.South =>
                'v',

            Heading.SouthWest =>
                '/',

            Heading.West =>
                '<',

            Heading.NorthWest =>
                '\\',

            _ =>
                'o'
        };
    }

    // ---------------------------------------------------------------------
    // Combat message
    // ---------------------------------------------------------------------

    public void ShowCombatMessage(
        NwPlayer player,
        string message)
    {
        if (!_tokens.TryGetValue(
                player.PlayerName,
                out NuiWindowToken token))
        {
            return;
        }

        token.SetBindValue(
            _combatMessageBind,
            message);
    }

    // ---------------------------------------------------------------------
    // Close
    // ---------------------------------------------------------------------

    public void Close(NwPlayer player)
    {
        _selection.Clear(player.PlayerName);
        _mapGroups.Remove(player.PlayerName);
        _mapViews.Remove(player.PlayerName);
        if (_tokens.Remove(player.PlayerName, out NuiWindowToken token)) SailingWindowLifecycle.Close(token);
    }
    public void Dispose()
    {
        foreach (NwPlayer player in _tokens.Values.Select(t => t.Player).ToArray()) Close(player);
        _tokens.Clear(); _mapGroups.Clear(); _mapViews.Clear();
    }
}

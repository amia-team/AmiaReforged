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
    private readonly Dictionary<string, NuiGroup> _contentGroups = new();
    private readonly ShipTargetSelection _selection = new();
    private readonly ShipRegistryService _registry;
    private readonly NauticalChartWindowCache _charts = new();
    private readonly Dictionary<string, ShipState> _chartShips = new();
    private readonly Dictionary<string, float> _chartSizes = new();
    private readonly NuiBind<NuiRect> _geometryBind = new("sailing_geometry");
    private readonly NuiBind<string> _motionBind = new("chart_motion"), _modeBind = new("chart_mode"),
        _destinationBind = new("chart_destination"), _waypointsBind = new("chart_waypoints"), _chartContactsBind = new("chart_contacts");
    private readonly NuiBind<string> _nextBind = new("chart_next"), _finalBind = new("chart_final"),
        _horizonBearingBind = new("chart_horizon_bearing"), _horizonNamesBind = new("chart_horizon_names"),
        _damageRangeBind = new("chart_damage_range"), _cycleArcBind = new("chart_cycle_arc"), _targetSummaryBind = new("sailing_target_summary");
    private readonly NuiBind<string>[] _logLines = Enumerable.Range(0, 4).Select(i => new NuiBind<string>($"chart_log_{i}")).ToArray();
    private readonly IslandService _islands;
    private bool _disposed;
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
    ShipNavigationService shipNavigationService, IslandService islands)
    {
        _registry = registry;
        _islands = islands;
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
                text));

        header.Height =
            28.0f;

        return header;
    }

    private static NuiLabel InfoLabel(
        NuiProperty<string> property,
        float height = 28.0f)
    {
        NuiLabel label = new(
            property);

        label.Height =
            height;
        label.Width = NauticalChartGeometry.SidebarContentWidth;
        label.Margin = 0; label.Padding = 2; label.Tooltip = property;
        label.HorizontalAlign = NuiHAlign.Left; label.VerticalAlign = NuiVAlign.Middle;

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
            36.0f;
        button.Margin = 0; button.Padding = 2; button.Tooltip = text;

        return button;
    }

    // ---------------------------------------------------------------------
    // Open
    // ---------------------------------------------------------------------

    public void Open(NwPlayer player, ShipState ship)
    {
        if (_disposed || !player.IsValid) return;
        Close(player);
        float size = NauticalChartGeometry.InitialSize(player.GetDeviceProperty(PlayerDeviceProperty.GuiWidth),
            player.GetDeviceProperty(PlayerDeviceProperty.GuiHeight), player.GetDeviceProperty(PlayerDeviceProperty.GuiScale));
        var root = CreateLayout(ship, size);
        var mapGroup = MapGroup(root);
        var window = CreateWindow(root);
        var content = (NuiGroup)window.Root;
        if (!player.TryCreateNuiWindow(window, out var token, WindowId))
        { Log.Error("Failed to create sailing NUI for player {0}.", player.PlayerName); return; }
        _tokens[player.PlayerName] = token;
        _mapGroups[player.PlayerName] = mapGroup;
        _contentGroups[player.PlayerName] = content;
        _charts.Open(player.PlayerName);
        _chartShips[player.PlayerName] = ship;
        _chartSizes[player.PlayerName] = size;
        token.SetBindValue(_geometryBind, new NuiRect(-1, -1, size + NauticalChartGeometry.WindowWidthOverhead,
            size + NauticalChartGeometry.WindowHeightOverhead));
        token.SetBindWatch(_geometryBind, true);
        player.OnNuiEvent -= HandleChartEvent; player.OnNuiEvent += HandleChartEvent;
        foreach (var bind in _logLines) token.SetBindValue(bind, "");
        Update(player, ship);
    }

    private NuiWindow CreateWindow(NuiColumn root) => new(new NuiGroup
    {
        Id = "sailing_chart_content", Layout = root, Border = false,
        Scrollbars = NuiScrollbars.None, Margin = 0, Padding = 0
    }, "Amia Sailing Beta v0.1") { Geometry = _geometryBind, Closable = true, Resizable = true };

    // All essential controls are fixed. Only the additional-actions panel and target rows scroll vertically.
    private NuiColumn CreateLayout(ShipState ship, float size)
    {
        float panelHeight = size + NauticalChartGeometry.LegendHeight;
        var map = new NuiGroup { Id = "sailing_map_group", Width = size, Height = size, Border = false,
            Margin = 0, Padding = 0, Scrollbars = NuiScrollbars.None, Scissor = true,
            Layout = new NuiRow { Width = size, Height = size, Margin = 0, Padding = 0, Scissor = true } };
        var left = new NuiColumn { Width = size, Height = panelHeight, Margin = 0, Padding = 0, Children =
        [
            map,
            InfoLabel("Gold: yours/course   Blue: player   Green: merchant"),
            InfoLabel("Red: pirate   Grey: neutral   Port: diamond   Target: ring")
        ] };
        foreach (var legend in left.Children.OfType<NuiLabel>())
        { legend.Width = size; legend.Tooltip = "Dark fill: uncharted. Rings identify Sailing selection; diamonds identify discovered ports."; }

        var panel = new NuiColumn { Width = NauticalChartGeometry.SidebarWidth, Height = panelHeight, Margin = 0, Padding = 0 };
        panel.Children.Add(InfoLabel(ship.ShipName));
        panel.Children.Add(Readouts(_headingBind, 120, _motionBind, 180));
        panel.Children.Add(Readouts(_modeBind, 170, _areaBind, 130));
        panel.Children.Add(Controls(("LEFT", "left_button"), ("AHEAD", "ahead_button"), ("RIGHT", "right_button")));
        var second = Controls(("ASTERN", "astern_button"), ("STOP", "stop_button"), ("DOCK", "dock_button"));
        second.Children.OfType<NuiButton>().Single(b => b.Id == "dock_button").Enabled = _dockEnabledBind;
        panel.Children.Add(second);
        var next = InfoLabel(_nextBind); next.Tooltip = _destinationBind;
        var final = InfoLabel(_finalBind); final.Tooltip = _destinationBind;
        panel.Children.Add(next); panel.Children.Add(final);
        var targetHeading = SectionHeader("SAILING TARGET"); targetHeading.Width = NauticalChartGeometry.SidebarContentWidth;
        targetHeading.Margin = 0; targetHeading.Padding = 2;
        panel.Children.Add(targetHeading);
        var targetName = InfoLabel(_targetNames, 44); targetName.Width = 208;
        panel.Children.Add(new NuiList([
            SailingNuiLayout.Cell(targetName, 208),
            SailingNuiLayout.Cell(new NuiButton("SELECT") { Id = "sailing_target_select", Enabled = _targetEnabled,
                Width = 74, Height = 36, Margin = 0, Padding = 2 }, 74)
        ], _targetCount) { Id = "sailing_target_list", Width = NauticalChartGeometry.SidebarContentWidth, Height = 96,
            RowHeight = 44, Border = false, Margin = 0, Padding = 0, Scrollbars = NuiScrollbars.Y });
        panel.Children.Add(InfoLabel(_targetSummaryBind));

        float essentialHeight = panel.Children.Sum(c => c.Height ?? 0);
        var details = new NuiColumn { Width = NauticalChartGeometry.SidebarContentWidth, Margin = 0, Padding = 0 };
        details.Children.Add(Controls(("HAIL", "hail_button"), ("TRADE", "trade_button")));
        details.Children.Add(Controls(("PORT MARKET", "port_market_button"), ("ATTACK", "attack_button")));
        details.Children.Add(InfoLabel(_waypointsBind));
        var contacts = InfoLabel(_chartContactsBind); contacts.Tooltip = "Only visible contacts in discovered cells are plotted.";
        details.Children.Add(contacts);
        details.Children.Add(InfoLabel(_positionBind));
        details.Children.Add(SectionHeader("HORIZON"));
        var bearing = InfoLabel(_horizonBearingBind); bearing.Tooltip = _horizonBind;
        var names = InfoLabel(_horizonNamesBind); names.Tooltip = _horizonBind;
        details.Children.Add(bearing); details.Children.Add(names);
        details.Children.Add(Controls(("HAIL CONTACT", "hail_contact_button")));
        details.Children.Add(SectionHeader("BOARDING"));
        details.Children.Add(InfoLabel(_boardingBind));
        details.Children.Add(Controls(("BOARD", "board_button"), ("ACCEPT", "accept_board_button"), ("REJECT", "reject_board_button")));
        details.Children.Add(SectionHeader("SHIP STATUS"));
        details.Children.Add(InfoLabel(_statusBind)); details.Children.Add(InfoLabel(_hullBind));
        details.Children.Add(InfoLabel(_weaponBind));
        var damage = InfoLabel(_damageRangeBind); damage.Tooltip = _weaponStatsBind;
        var cycle = InfoLabel(_cycleArcBind); cycle.Tooltip = _weaponStatsBind;
        details.Children.Add(damage); details.Children.Add(cycle); details.Children.Add(InfoLabel(_weaponStatusBind));
        details.Children.Add(Controls(("CANNON", "weapon_cannon_button"), ("BALLISTA", "weapon_ballista_button")));
        details.Children.Add(Controls(("CATAPULT", "weapon_catapult_button"), ("HEAVY CANNON", "weapon_heavy_cannon_button")));
        details.Children.Add(Controls(("REPAIR", "repair_button")));
        details.Children.Add(SectionHeader("COMMAND LOG"));
        foreach (var bind in _logLines) { var line = InfoLabel(bind); line.Tooltip = _combatMessageBind; details.Children.Add(line); }
        details.Children.Add(Controls(("TEST AUTONOMOUS NAV", "test_navigation_button")));
        details.Children.Add(Controls(("LEAVE HELM", "leave_button")));
        foreach (var heading in details.Children.OfType<NuiLabel>())
        { heading.Width = NauticalChartGeometry.SidebarContentWidth; heading.Margin = 0; heading.Padding = 2; }
        details.Height = details.Children.Sum(c => c.Height ?? 0);
        panel.Children.Add(new NuiGroup { Id = "sailing_additional_controls", Layout = details, Width = NauticalChartGeometry.SidebarWidth,
            Height = panelHeight - essentialHeight, Border = false, Scrollbars = NuiScrollbars.Y, Margin = 0, Padding = 0 });
        var instruments = new NuiGroup { Id = "sailing_instruments", Layout = panel, Width = NauticalChartGeometry.SidebarWidth,
            Height = panelHeight, Border = false, Scrollbars = NuiScrollbars.None, Margin = 0, Padding = 0 };
        float width = size + 12 + NauticalChartGeometry.SidebarWidth;
        var body = new NuiRow { Width = width, Height = panelHeight, Margin = 0, Padding = 0, Children =
            [left, new NuiSpacer { Width = 12, Height = panelHeight, Margin = 0, Padding = 0 }, instruments] };
        var title = SectionHeader("NAUTICAL CHART / NORTH UP"); title.Width = width; title.Margin = 0; title.Padding = 2;
        return new NuiColumn { Width = width, Height = panelHeight + 28, Margin = 0, Padding = 0, Children =
        [
            title, body
        ] };
    }
    private static NuiGroup MapGroup(NuiColumn root) => (NuiGroup)((NuiColumn)((NuiRow)root.Children[1]).Children[0]).Children[0];
    private static NuiRow Controls(params (string Text, string Id)[] controls)
    {
        const float gap = 4;
        float width = (NauticalChartGeometry.SidebarContentWidth - gap * (controls.Length - 1)) / controls.Length;
        var row = new NuiRow { Width = NauticalChartGeometry.SidebarContentWidth, Height = 44, Margin = 0, Padding = 0 };
        foreach (var control in controls)
        {
            if (row.Children.Count > 0) row.Children.Add(new NuiSpacer { Width = gap, Height = 40, Margin = 0, Padding = 0 });
            var button = Button(control.Text, control.Id); button.Width = width; button.Height = 40;
            row.Children.Add(button);
        }
        return row;
    }
    private static NuiRow Readouts(NuiProperty<string> first, float firstWidth, NuiProperty<string> second, float secondWidth)
    {
        var a = InfoLabel(first); a.Width = firstWidth;
        var b = InfoLabel(second); b.Width = secondWidth;
        return new NuiRow { Width = NauticalChartGeometry.SidebarContentWidth, Height = 28, Margin = 0, Padding = 0,
            Children = [a, new NuiSpacer { Width = 4, Height = 28, Margin = 0, Padding = 0 }, b] };
    }

    private void HandleChartEvent(Anvil.API.Events.ModuleEvents.OnNuiEvent evt)
    {
        // HelmService continues owning close/click actions; this subscriber only handles chart geometry.
        if (!OwnsEvent(evt) || evt.EventType != NuiEventType.Watch || evt.ElementId != _geometryBind.Key ||
            !_chartShips.TryGetValue(evt.Player.PlayerName, out var ship)) return;
        var rect = evt.Token.GetBindValue(_geometryBind);
        float minimumWidth = NauticalChartGeometry.MinimumDisplaySize + NauticalChartGeometry.WindowWidthOverhead;
        float minimumHeight = NauticalChartGeometry.MinimumDisplaySize + NauticalChartGeometry.WindowHeightOverhead;
        if (rect.Width < minimumWidth || rect.Height < minimumHeight)
        {
            evt.Token.SetBindValue(_geometryBind, new NuiRect(rect.X, rect.Y, Math.Max(rect.Width, minimumWidth), Math.Max(rect.Height, minimumHeight)));
            return;
        }
        float size = NauticalChartGeometry.ResizedSize(rect.Width, rect.Height);
        if (_chartSizes.GetValueOrDefault(evt.Player.PlayerName) == size) return;
        _chartSizes[evt.Player.PlayerName] = size;
        if (_contentGroups.TryGetValue(evt.Player.PlayerName, out var content))
        {
            var root = CreateLayout(ship, size);
            evt.Token.SetGroupLayout(content, root);
            _mapGroups[evt.Player.PlayerName] = MapGroup(root);
            _charts.Open(evt.Player.PlayerName);
        }
        if (_registry.IsCurrent(ship)) Update(evt.Player, ship);
    }

    // ---------------------------------------------------------------------
    // Update
    // ---------------------------------------------------------------------

    public void Update(
        NwPlayer player,
        ShipState ship,
        IReadOnlyCollection<ShipState>? ships = null)
    {
        if (_disposed || !player.IsValid) return;
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

        ships ??= _registry.GetShips();

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

        var visibleShips = _shipVisibilityService.GetVisibleShips(ship, ships).ToArray();
        var visibleOcean = _oceanContactService.GetVisibleContacts(ship).ToArray();
        List<ShipState> nearby = _shipEncounterService.GetNearbyShips(ship).ToList();
        _selection.Refresh(player.PlayerName, ship, nearby);
        ShipState? selected = _selection.GetSelected(player.PlayerName, ship, nearby);
        string cells = _chartDiscoveryService.CaptureCells(player, ship.AreaResRef);
        var frame = NauticalChartProjection.Capture(ship, _shipNavigationService, visibleShips, visibleOcean,
            _chartLandmarkService.GetLandmarks(ship.AreaResRef), _chartMarkerService.GetMarkers(ship.AreaResRef),
            _islands.GetIslands(), (x, y) => cells[x * 16 + y] == '1', selected?.ShipId);
        if (_charts.TryGet(player.PlayerName, out var renderer) && _mapGroups.TryGetValue(player.PlayerName, out var mapGroup))
        {
            var update = renderer.Prepare(frame, _chartSizes[player.PlayerName]);
            if (update.Layout != null) token.SetGroupLayout(mapGroup, update.Layout);
            NauticalChartRenderer.Publish(token, update.Changes);
        }

        // -----------------------------------------------------------------
        // Horizon
        // -----------------------------------------------------------------

        string horizon = _horizonContactService.BuildHorizonString(ship, visibleShips, visibleOcean);
        token.SetBindValue(_horizonBind, horizon);
        string[] horizonLines = horizon.Split('\n');
        token.SetBindValue(_horizonBearingBind, horizonLines[0]);
        token.SetBindValue(_horizonNamesBind, horizonLines.Length > 1 ? horizonLines[1] : "");





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
        token.SetBindValue(_damageRangeBind, $"DMG {weapon.Damage}  |  RNG {weapon.MaxRange:0.0}");
        token.SetBindValue(_cycleArcBind, $"CD {weapon.Cooldown.TotalSeconds:0.0}s  |  ARC {weapon.Arc}");

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
        token.SetBindValues(_targetNames, _selection.Rows(player.PlayerName).Select(s => nearby.Contains(s) ? s.ShipName : $"{s.ShipName} (departed)").ToList());
        token.SetBindValues(_targetEnabled, _selection.Rows(player.PlayerName).Select(nearby.Contains));
        token.SetBindValue(_targetCount, _selection.Rows(player.PlayerName).Count);
        token.SetBindValue(_statusBind, ship.IsDisabled ? "Status: DISABLED" :
            nearby.Count > 0 ? "Status: ENCOUNTER" : ship.Underway ? "Status: UNDERWAY" : "Status: STOPPED");
        token.SetBindValue(_encounterBind, selected == null ? "Target: NONE" : $"Target: {selected.ShipName}");
        token.SetBindValue(_encounterDistanceBind, selected == null ? "Distance: --" :
            $"Distance: {ShipActionRules.Distance(ship, selected):0.0}m");
        token.SetBindValue(_targetSummaryBind, NauticalChartReadouts.TargetSummary(ship, selected));
        if (_shipBoardingService.TryGetRequestForPlayer(player.PlayerName, out ShipBoardingRequest? incoming) && incoming != null)
            token.SetBindValue(_boardingBind, $"BOARDING REQUEST: {incoming.RequestingShip.ShipName}");
        else token.SetBindValue(_boardingBind, _shipBoardingService.HasRequestFromPlayer(player.PlayerName) ?
            "BOARDING REQUEST SENT" : "Boarding: NONE");
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
        string[] lines = NauticalChartReadouts.LogLines(message, _logLines.Length);
        for (int i = 0; i < _logLines.Length; i++) token.SetBindValue(_logLines[i], lines[i]);
    }

    // ---------------------------------------------------------------------
    // Close
    // ---------------------------------------------------------------------

    public void Close(NwPlayer player)
    {
        player.OnNuiEvent -= HandleChartEvent;
        _selection.Clear(player.PlayerName);
        _mapGroups.Remove(player.PlayerName);
        _contentGroups.Remove(player.PlayerName);
        _charts.Close(player.PlayerName); _chartShips.Remove(player.PlayerName); _chartSizes.Remove(player.PlayerName);
        if (_tokens.Remove(player.PlayerName, out NuiWindowToken token)) SailingWindowLifecycle.Close(token);
    }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        foreach (NwPlayer player in _tokens.Values.Select(t => t.Player).ToArray()) Close(player);
        _tokens.Clear(); _mapGroups.Clear(); _contentGroups.Clear(); _charts.Clear(); _chartShips.Clear(); _chartSizes.Clear();
    }
}

using System.Text; using AmiaReforged.Core.Models.Sailing; using Anvil.API; using Anvil.Services; using NLog;
namespace AmiaReforged.Core.Services.Sailing;
[ServiceBinding(typeof(SailingNuiService))] public class SailingNuiService { private const string WindowId = "sailing";
private const float MapWorldSize =
    640.0f;

private const int MapCells =
    16;

private static readonly Logger Log =
    LogManager.GetCurrentClassLogger();

private readonly Dictionary<string, NuiWindowToken> _tokens =
    new();

private NuiGroup? _mapGroup;

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


    private readonly NuiBind<string> _boardingBind =
    new("ship_boarding");

private readonly NuiBind<string> _horizonBind =
    new("ship_horizon");
private readonly NuiBind<string> _tradeMerchantBind =
    new("trade_merchant");

private readonly NuiBind<string> _tradePortBind =
    new("trade_port");

private readonly NuiBind<string> _tradeGoldBind =
    new("trade_gold");

private readonly NuiBind<string> _tradeCargoBind =
    new("trade_cargo");
private readonly NuiBind<bool> _dockEnabledBind =
    new("dock_enabled");

    private readonly Dictionary<string, string>
    _activeTradeMerchants =
        new(StringComparer.Ordinal);

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

        Log.Info(
        "Sailing NUI Service initialized.");
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
        Log.Info(
    $"Sprite lookup: {ship.ShipName}, " +
    $"Prefix={ship.SpritePrefix}, " +
    $"Result={ship.SpritePrefix}_{GetShipImage(ship.Heading)}");
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
_mapGroup = new NuiGroup
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

NuiLabel tradeMerchant =
    InfoLabel(
        _tradeMerchantBind,
        56.0f);

NuiLabel tradePort =
    InfoLabel(
        _tradePortBind,
        56.0f);

NuiLabel tradeGold =
    InfoLabel(
        _tradeGoldBind,
        56.0f);

NuiLabel tradeCargo =
    InfoLabel(
        _tradeCargoBind,
        56.0f);
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

    // Navigation
    column.Children.Add(
        navigationHeader);

    column.Children.Add(
        area);

    column.Children.Add(
        position);

    column.Children.Add(
        heading);

column.Children.Add(_mapGroup);

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

column.Children.Add(
    tradeMerchant);

column.Children.Add(
    tradePort);

column.Children.Add(
    tradeGold);

column.Children.Add(
    tradeCargo);
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

    Update(
        player,
        ship);

    Log.Info(
        $"Sailing NUI opened for player " +
        $"{player.PlayerName}.");


}

// ---------------------------------------------------------------------
// Update
// ---------------------------------------------------------------------

public void Update(
    NwPlayer player,
    ShipState ship,
    IReadOnlyCollection<ShipState>? ships = null)
{
    ships ??= Array.Empty<ShipState>();

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

    Log.Info(
        $"Sailing NUI updating: " +
        $"Player={player.PlayerName}, " +
        $"Ship={ship.ShipName}, " +
        $"Area={ship.AreaResRef}, " +
        $"X={ship.X:0.00}, " +
        $"Y={ship.Y:0.00}, " +
        $"Heading={ship.Heading}");

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

if (_mapGroup != null)
{
token.SetGroupLayout(
    _mapGroup,
    BuildMapCanvas(player, ship, ships));
}

// -----------------------------------------------------------------
// Horizon
// -----------------------------------------------------------------

token.SetBindValue(
    _horizonBind,
    _horizonContactService.BuildHorizonString(ship));



    token.SetBindValue(
        _horizonBind,
        _horizonContactService.BuildHorizonString(ship));

    // -----------------------------------------------------------------
    // Hull
    // -----------------------------------------------------------------

    token.SetBindValue(
        _hullBind,
        $"Hull Integrity: {ship.Hull}%");

    // -----------------------------------------------------------------
    // Weapon
    // -----------------------------------------------------------------

    ShipWeapon weapon =
        _shipCombatService.GetWeapon(
            ship.WeaponResRef);

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
            "Weapon Status: READY");
    }

    // -----------------------------------------------------------------
    // Encounter
    // -----------------------------------------------------------------

    if (ship.Hull <= 0)
{ token.SetBindValue( _statusBind, "Status: DISABLED");
token.SetBindValue(
    _encounterBind,
    "Target: NONE");

token.SetBindValue(
    _encounterDistanceBind,
    "Distance: --");
} else { OceanContact? nearestContact = _oceanContactService.GetClosestContact(ship);
if (nearestContact != null)
{
    float distance =
        MathF.Sqrt(
            MathF.Pow(nearestContact.X - ship.X, 2) +
            MathF.Pow(nearestContact.Y - ship.Y, 2));

    token.SetBindValue(
        _statusBind,
        "Status: CONTACT");

    token.SetBindValue(
        _encounterBind,
        $"Target: {nearestContact.Name}");

    token.SetBindValue(
        _encounterDistanceBind,
        $"Distance: {distance:0.0}m");
}
else if (_shipEncounterService.TryGetTarget(
             ship,
             out ShipState? targetShip,
             out ShipEncounter? encounter) &&
         targetShip != null &&
         encounter != null)
{
    token.SetBindValue(
        _statusBind,
        "Status: ENCOUNTER");

    token.SetBindValue(
        _encounterBind,
        $"Target: {targetShip.ShipName}");

    token.SetBindValue(
        _encounterDistanceBind,
        $"Distance: {encounter.Distance:0.00}");

    token.SetBindValue(
    _dockEnabledBind,
    ship.CanDock);
            }
else
{
    token.SetBindValue(
        _statusBind,
        ship.Underway
            ? "Status: UNDERWAY"
            : "Status: STOPPED");

    token.SetBindValue(
        _encounterBind,
        "Target: NONE");

    token.SetBindValue(
        _encounterDistanceBind,
        "Distance: --");
}
}
// -----------------------------------------------------------------
// Merchant Trade
// -----------------------------------------------------------------

if (_shipEncounterService.TryGetTarget(
        ship,
        out ShipState? tradeTarget,
        out ShipEncounter? tradeEncounter) &&
    tradeTarget != null &&
    tradeEncounter != null &&
    tradeTarget.ShipType == ShipType.Merchant)
{
    token.SetBindValue(
        _tradeMerchantBind,
        $"Merchant: {tradeTarget.ShipName}");

    token.SetBindValue(
        _tradePortBind,
        $"Port: " +
        $"{tradeTarget.CurrentTradePortId ?? "NONE"}");

    token.SetBindValue(
        _tradeGoldBind,
        $"Merchant Gold: " +
        $"{tradeTarget.MerchantGold}");

    string cargoText;

    if (tradeTarget.Cargo.Count == 0)
    {
        cargoText =
            "Cargo: Empty";
    }
    else
    {
        cargoText =
            "Cargo: " +
            string.Join(
                ", ",
                tradeTarget.Cargo.Select(
                    cargo =>
                        $"{cargo.ItemId}={cargo.Quantity}"));
    }

    token.SetBindValue(
        _tradeCargoBind,
        cargoText);
}
else
{
    token.SetBindValue(
        _tradeMerchantBind,
        "Merchant: --");

    token.SetBindValue(
        _tradePortBind,
        "Port: --");

    token.SetBindValue(
        _tradeGoldBind,
        "Merchant Gold: --");

    token.SetBindValue(
        _tradeCargoBind,
        "Cargo: --");
}
        // -----------------------------------------------------------------
        // Boarding
        // -----------------------------------------------------------------

        if (_shipBoardingService
            .TryGetRequestForPlayer(
                player.PlayerName,
                out ShipBoardingRequest?
                    incomingRequest) &&
        incomingRequest != null)
    {
        token.SetBindValue(
            _boardingBind,
            $"BOARDING REQUEST: " +
            $"{incomingRequest.RequestingShip.ShipName}");
    }
    else if (_shipBoardingService
                 .HasRequestFromPlayer(
                     player.PlayerName))
    {
        token.SetBindValue(
            _boardingBind,
            "BOARDING REQUEST SENT");
    }
    else
    {
        token.SetBindValue(
            _boardingBind,
            "Boarding: NONE");
    }
}

// ---------------------------------------------------------------------
// Live Sailing Map
// ---------------------------------------------------------------------

private NuiRow BuildMapCanvas(
    NwPlayer player,
    ShipState ship,
    IReadOnlyCollection<ShipState>? ships = null)
{
    ships ??= Array.Empty<ShipState>();
Log.Info(
    $"BuildMapCanvas: Viewer={ship.ShipName}, ShipCount={ships.Count}");
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

        // -------------------------------------------------------------
        // Draw islands.
               // -------------------------------------------------------------
     /*  foreach (ChartLandmark landmark in
                _chartLandmarkService.GetLandmarks(ship.AreaResRef))
       {
           float drawX =
               (landmark.X / MapWorldSize) * 800f;

           float drawY =
               800f -
               ((landmark.Y / MapWorldSize) * 800f);

           drawList.Add(
               new NuiDrawListImage(
                   landmark.Sprite,
                   new NuiRect(
                       drawX - 64f,
                       drawY - 64f,
                       128f,
                       128f)));
       }

               // -------------------------------------------------------------
               // Draw discovered islands.
               // -------------------------------------------------------------
               foreach (SailingObstacle obstacle in
                _shipObstacleService.GetObstacles(ship.AreaResRef))
       {
           int gridX =
               Math.Clamp((int)(obstacle.MinX / 10f), 0, 15);

          int gridY =
           15 -
           Math.Clamp((int)(obstacle.MinY / 10f), 0, 15);

           if (!_chartDiscoveryService.IsDiscovered(
                   player,
                   ship.AreaResRef,
                   gridX,
                   gridY))
           {
               continue;
           }

           float drawX =
               (obstacle.MinX / MapWorldSize) * 800f;

           float drawY =
               800f -
               ((obstacle.MaxY / MapWorldSize) * 800f);

           float width =
               ((obstacle.MaxX - obstacle.MinX) / MapWorldSize) * 800f;

           float height =
               ((obstacle.MaxY - obstacle.MinY) / MapWorldSize) * 800f;

           drawList.Add(
               new NuiDrawListImage(
                   "chart_island",
                   new NuiRect(
                       drawX,
                       drawY,
                       width,
                       height)));
       }
*/
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
        /*foreach (ChartMarker marker in
                 _chartMarkerService.GetMarkers(ship.AreaResRef))
        {
            float drawX =
                (marker.X / MapWorldSize) * 800f;

            float drawY =
                800f -
                ((marker.Y / MapWorldSize) * 800f);

            const float markerSize = 28f;

            drawList.Add(
                new NuiDrawListImage(
                    "chart_dock",
                    new NuiRect(
                        drawX,
                        drawY,
                        markerSize,
                        markerSize)));
        }*/     // -------------------------------------------------------------
                // Draw other visible ships first.
                // -------------------------------------------------------------
                // -------------------------------------------------------------
                // Draw other visible ships first.
                // -------------------------------------------------------------

        Log.Info($"BuildMapCanvas: Viewer={ship.ShipName}, ShipCount={ships.Count}");

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

public void Close(
    NwPlayer player)
{
    if (!_tokens.TryGetValue(
            player.PlayerName,
            out NuiWindowToken token))
    {
        return;
    }

    token.Close();

    _tokens.Remove(
        player.PlayerName);
}
}


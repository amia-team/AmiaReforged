using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(SailingBootstrapService))]
public class SailingBootstrapService
{
    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private readonly HelmService _helmService;

    private readonly PhysicalShipService _physicalShipService;

    // ---------------------------------------------------------------------
    // Starting Fleet
    // ---------------------------------------------------------------------

    private static readonly ShipDefinition[] StartingFleet =
    {
new(
    "Sea Sprite",
    "sloop",
    "sailing_helm",
    "sea_sprite",
    "sea_sprite_exit",
    "sea_sprite_d2",
    "sea_sprite_c",
    "ocean_01",
    50f,
    50f,
    0f,
    Heading.East,
    ShipType.Player,
    100),
new(
    "Black Pearl",
    "galleon",
    "sailing_helm_black_pearl",
    "black_pearl",
    "black_pearl_exit",
    "black_pearl_d2",
    "black_pearl_c",
    "ocean_01",
    100f,
    100f,
    0f,
    Heading.West,
    ShipType.Player,
    160),

new(
    "Stormrunner",
    "brig",
    "sailing_helm_stormrunner",
    "stormrunner",
    "stormrunner_exit",
    "stormrunner_d2",
    "stormrunner_c",
    "ocean_01",
    70f,
    40f,
    0f,
    Heading.East,
    ShipType.Player,
    90),

new(
    "Golden Gull",
    "cog",
    "sailing_helm_golden_gull",
    "golden_gull",
    "golden_gull_exit",
    "golden_gull_d2",
    "golden_gull_c",
    "ocean_01",
    120f,
    70f,
    0f,
    Heading.South,
    ShipType.Player,
    140),
    };

    // ---------------------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------------------

    public SailingBootstrapService(
        HelmService helmService,
        PhysicalShipService physicalShipService)
    {
        _helmService = helmService;
        _physicalShipService = physicalShipService;

 foreach (ShipDefinition ship in StartingFleet)
{
  _helmService.CreateShip(ship);

    _physicalShipService.RegisterPhysicalShip(ship);
}

_physicalShipService.RegisterPhysicalShipInteractions();

foreach (ShipDefinition ship in StartingFleet)
{
    _physicalShipService.SpawnPhysicalShip(ship.ShipName);
}
        // Spawn a physical ship for testing.
        //_physicalShipService.TestPhysicalShip("Sea Sprite");

        Log.Info(
            $"Sailing system initialized. Registered {StartingFleet.Length} ship(s).");
    }
}
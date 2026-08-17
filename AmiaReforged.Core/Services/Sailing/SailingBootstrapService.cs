using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(SailingBootstrapService))]
public class SailingBootstrapService
{
    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private readonly HelmService _helmService;

    private readonly PhysicalShipService
        _physicalShipService;

    public SailingBootstrapService(
        HelmService helmService,
        PhysicalShipService physicalShipService)
    {
        _helmService =
            helmService;

        _physicalShipService =
            physicalShipService;

        _helmService.CreateShip(
            "Sea Sprite",
            50.0f,
            50.0f,
            0.0f);

        _helmService.CreateShip(
            "Black Pearl",
            100.0f,
            100.0f,
            0.0f);

        _physicalShipService.TestPhysicalShip(
            "Sea Sprite");

        Log.Info(
            "Sailing system initialized. " +
            "Sea Sprite registered in ocean_01 " +
            "at X=50, Y=50, Z=0. " +
            "Black Pearl registered in ocean_01 " +
            "at X=100, Y=100, Z=0.");
    }
}
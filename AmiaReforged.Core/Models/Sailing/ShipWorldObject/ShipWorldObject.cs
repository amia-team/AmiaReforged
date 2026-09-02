using AmiaReforged.Core.Models.Sailing.Ship.Types;
using Anvil.API;

namespace AmiaReforged.Core.Models.Sailing.ShipWorldObject;

/// <summary>
/// A collection of game-world objects associated with the ship
/// </summary>
public class ShipWorldObject
{
    public ShipWorldObject(string shipName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shipName);
        ShipName = shipName;
    }

    public string ShipName { get; }
    public NwPlaceable? Helm { get; private set; }
    public NwPlaceable? Exit { get; private set; }
    public NwArea? DeckArea { get; private set; }
    public NwArea? CabinArea { get; private set; }
    public IReadOnlyDictionary<ShipArmamentSlot, NwWaypoint>? ArmamentWaypoints { get; private set; }
    public void BindShipObjects(
        NwPlaceable? helm,
        NwPlaceable? exit,
        NwArea? deckArea,
        NwArea? cabinArea,
        IReadOnlyDictionary<ShipArmamentSlot, NwWaypoint>? armamentWaypoints)
    {
        ArgumentNullException.ThrowIfNull(helm);
        ArgumentNullException.ThrowIfNull(exit);
        ArgumentNullException.ThrowIfNull(deckArea);
        ArgumentNullException.ThrowIfNull(cabinArea);
        ArgumentNullException.ThrowIfNull(armamentWaypoints);

        Helm = helm;
        Exit = exit;
        DeckArea = deckArea;
        CabinArea = cabinArea;
        ArmamentWaypoints = armamentWaypoints;
    }
}

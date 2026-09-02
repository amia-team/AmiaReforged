using AmiaReforged.Core.Models.Sailing.Ship.Types;
using Anvil.API;

namespace AmiaReforged.Core.Models.Sailing.ShipWorldObject;

/// <summary>
/// A collection of game-world objects associated with the ship
/// </summary>
public class ShipWorldObject(
    string shipName,
    NwArea deckArea,
    NwPlaceable helm,
    NwPlaceable exit,
    NwArea? cabinArea,
    IReadOnlyDictionary<ShipArmamentSlot, NwWaypoint>? armamentWaypoints)
{
    public string ShipName { get; } = shipName;
    public NwArea DeckArea { get; private set; } = deckArea;
    public NwPlaceable Helm { get; private set; } = helm;
    public NwPlaceable Exit { get; private set; } = exit;
    public NwArea? CabinArea { get; private set; } = cabinArea;
    public IReadOnlyDictionary<ShipArmamentSlot, NwWaypoint>? ArmamentWaypoints { get; private set; } = armamentWaypoints;
}

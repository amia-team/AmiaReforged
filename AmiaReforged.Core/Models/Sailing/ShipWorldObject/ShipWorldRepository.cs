using AmiaReforged.Core.Models.Sailing.Ship.Types;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Models.Sailing.ShipWorldObject;

public class ShipWorldRepository
{
    private const string DeckLocalVarName = "ship_deck";
    private const string HelmLocalVarName = "ship_helm";
    private const string ExitLocalVarName = "ship_exit";
    private const string ArmamentLocalVarName = "ship_armament";
    private const string CabinTransitionTag = "deck_to_cabin_transition";

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Gets all discovered ships.
    /// </summary>
    private readonly Dictionary<string, ShipWorldObject> _ships = new();

    /// <summary>
    /// Gets all discovered ships.
    /// </summary>
    public IReadOnlyCollection<ShipWorldObject> Ships => _ships.Values;

    public void DiscoverShips()
    {
        _ships.Clear();
        NwArea[] deckAreas = NwModule.Instance.Areas.Where(IsShipArea).ToArray();

        foreach (NwArea deckArea in deckAreas)
        {
            string shipName = GetShipName(deckArea);
            if (string.IsNullOrWhiteSpace(shipName))
                continue;

            NwPlaceable? helm = GetHelm(deckArea);
            if (helm == null)
            {
                WarnMissingRequiredShipObject(shipName, nameof(helm), HelmLocalVarName);
                continue;
            }
            NwPlaceable? exit = GetExit(deckArea);
            if (exit == null)
            {
                WarnMissingRequiredShipObject(shipName, nameof(exit), ExitLocalVarName);
                continue;
            }

            NwArea? cabinArea = GetCabinArea(deckArea);
            if (cabinArea == null)
            {
                Log.Warn("Ship '{ShipName}' is missing a cabin area. Cabin area is optional, but this might be an oversight."
                    + "If the ship has a cabin area, ensure the transition door or trigger is named '{CabinTransitionTag}'."
                    , shipName, CabinTransitionTag);
            }
            Dictionary<ShipArmamentSlot, NwWaypoint>? armamentWaypoints = GetArmamentWaypoints(deckArea);
            if (armamentWaypoints == null)
            {
                WarnMissingArmamentWaypoints();
            }

            ShipWorldObject ship = new(shipName);
            ship.BindShipObjects(helm, exit, deckArea, cabinArea, armamentWaypoints);

            if (!_ships.TryAdd(shipName, ship))
                Log.Warn("A ship named '{ShipName}' was already discovered. Skipping duplicate.", shipName);
        }
    }

    private static Dictionary<ShipArmamentSlot, NwWaypoint>? GetArmamentWaypoints(NwArea deckArea)
    {
        Dictionary<ShipArmamentSlot, NwWaypoint> waypointsBySlot = new();

        foreach (NwWaypoint waypoint in deckArea.FindObjectsOfTypeInArea<NwWaypoint>())
        {
            int slotValue = waypoint.GetObjectVariable<LocalVariableInt>(ArmamentLocalVarName).Value;

            if (!Enum.IsDefined(typeof(ShipArmamentSlot), slotValue))
                continue;

            ShipArmamentSlot slot = (ShipArmamentSlot)slotValue;
            waypointsBySlot.Add(slot, waypoint);
        }

        return waypointsBySlot.Count == 0 ? null : waypointsBySlot;
    }

    private static NwPlaceable? GetExit(NwArea deckArea) => deckArea.FindObjectsOfTypeInArea<NwPlaceable>()
        .FirstOrDefault(placeable => placeable.GetObjectVariable<LocalVariableBool>(ExitLocalVarName).HasValue);

    private static NwPlaceable? GetHelm(NwArea deckArea) => deckArea.FindObjectsOfTypeInArea<NwPlaceable>()
        .FirstOrDefault(placeable => placeable.GetObjectVariable<LocalVariableBool>(HelmLocalVarName).HasValue);

    private static NwArea? GetCabinArea(NwArea deckArea)
    {
        NwWaypoint? cabinWaypoint = deckArea.FindObjectsOfTypeInArea<NwWaypoint>()
            .FirstOrDefault(trigger => trigger.Tag == CabinTransitionTag);
        NwDoor? cabinDoor = deckArea.FindObjectsOfTypeInArea<NwDoor>()
            .FirstOrDefault(door => door.Tag == CabinTransitionTag);

        return cabinWaypoint?.TransitionTarget?.Area ?? cabinDoor?.TransitionTarget?.Area;
    }

    private static bool IsShipArea(NwArea area)
        => area.GetObjectVariable<LocalVariableString>(DeckLocalVarName).HasValue;

    private static string GetShipName(NwArea area)
        => area.GetObjectVariable<LocalVariableString>(DeckLocalVarName).Value ?? string.Empty;

    private static void WarnMissingRequiredShipObject(string shipName, string parameterName, string localVariableName)
    {
        Log.Warn(
            "Ship '{ShipName}' is missing required parameter '{ParameterName}'. " +
            "Set the area local variable '{LocalVariableName}' to TRUE on the correct object.",
            shipName, parameterName, localVariableName);
    }

    private static void WarnMissingArmamentWaypoints()
    {
        Log.Info("Waypoints for the placement of ship armaments not found." +
                 "Armaments are optional but this might be an error." +
                 "Armament slot local variable '{LocalVariableName}' values:", ArmamentLocalVarName);

        foreach (ShipArmamentSlot slot in Enum.GetValues<ShipArmamentSlot>())
        {
            Log.Info("  {SlotName} = {SlotValue}", slot, (int)slot);
        }
    }
}

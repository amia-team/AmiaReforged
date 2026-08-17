using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipEncounterService))]
public class ShipEncounterService
{
    private const float EncounterDistance = 15.0f;

    private readonly Dictionary<string, ShipEncounter>
        _activeEncounters = new();

    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    public ShipEncounterService()
    {
        Log.Info(
            $"Ship Encounter Service initialized. " +
            $"Encounter distance={EncounterDistance}.");
    }

    public void CheckEncounters(
        IReadOnlyCollection<ShipState> ships)
    {
        List<ShipState> shipList =
            ships.ToList();

        for (int i = 0; i < shipList.Count; i++)
        {
            ShipState shipA =
                shipList[i];

            for (int j = i + 1; j < shipList.Count; j++)
            {
                ShipState shipB =
                    shipList[j];

                CheckPair(
                    shipA,
                    shipB);
            }
        }
    }

    public bool IsInEncounter(
        ShipState shipA,
        ShipState shipB)
    {
        return _activeEncounters.ContainsKey(
            CreateEncounterKey(
                shipA,
                shipB));
    }

    public IReadOnlyCollection<ShipEncounter>
        GetActiveEncounters()
    {
        return _activeEncounters.Values;
    }
    public IEnumerable<ShipState> GetNearbyShips(
    ShipState ship)
{
    foreach (ShipEncounter encounter
        in _activeEncounters.Values)
    {
        if (ReferenceEquals(
                encounter.ShipA,
                ship))
        {
            yield return encounter.ShipB;
        }
        else if (ReferenceEquals(
                     encounter.ShipB,
                     ship))
        {
            yield return encounter.ShipA;
        }
    }
}


    public bool TryGetEncounter(
        ShipState shipA,
        ShipState shipB,
        out ShipEncounter? encounter)
    {
        string encounterKey =
            CreateEncounterKey(
                shipA,
                shipB);

        return _activeEncounters.TryGetValue(
            encounterKey,
            out encounter);
    }

    public bool TryGetEncounter(
        ShipState ship,
        out ShipEncounter? encounter)
    {
        foreach (ShipEncounter activeEncounter
            in _activeEncounters.Values)
        {
            if (ReferenceEquals(
                    activeEncounter.ShipA,
                    ship) ||
                ReferenceEquals(
                    activeEncounter.ShipB,
                    ship) ||
                string.Equals(
                    activeEncounter.ShipA.ShipName,
                    ship.ShipName,
                    StringComparison.Ordinal) ||
                string.Equals(
                    activeEncounter.ShipB.ShipName,
                    ship.ShipName,
                    StringComparison.Ordinal))
            {
                encounter =
                    activeEncounter;

                return true;
            }
        }

        encounter = null;

        return false;
    }

    public bool TryGetTarget(
        ShipState ship,
        out ShipState? targetShip,
        out ShipEncounter? encounter)
    {
        if (!TryGetEncounter(
                ship,
                out encounter) ||
            encounter == null)
        {
            targetShip = null;

            return false;
        }

        if (ReferenceEquals(
                encounter.ShipA,
                ship))
        {
            targetShip =
                encounter.ShipB;
        }
        else
        {
            targetShip =
                encounter.ShipA;
        }

        return true;
    }

    private void CheckPair(
        ShipState shipA,
        ShipState shipB)
    {
        string encounterKey =
            CreateEncounterKey(
                shipA,
                shipB);

        if (!string.Equals(
                shipA.AreaResRef,
                shipB.AreaResRef,
                StringComparison.OrdinalIgnoreCase))
        {
            RemoveEncounter(
                shipA,
                shipB);

            return;
        }

        float distanceX =
            shipA.X - shipB.X;

        float distanceY =
            shipA.Y - shipB.Y;

        float distance =
            MathF.Sqrt(
                distanceX * distanceX +
                distanceY * distanceY);

        if (distance <= EncounterDistance)
        {
            if (_activeEncounters.TryGetValue(
                    encounterKey,
                    out ShipEncounter? existingEncounter))
            {
                existingEncounter.Distance =
                    distance;

                existingEncounter.AreaResRef =
                    shipA.AreaResRef;

                return;
            }

            ShipEncounter encounter = new()
            {
                ShipA = shipA,
                ShipB = shipB,
                AreaResRef = shipA.AreaResRef,
                Distance = distance
            };

            _activeEncounters[encounterKey] =
                encounter;

            Log.Info(
                $"Ship encounter started: " +
                $"{shipA.ShipName} <-> " +
                $"{shipB.ShipName}, " +
                $"Area={shipA.AreaResRef}, " +
                $"Distance={distance:0.00}, " +
                $"A=({shipA.X:0.00}, {shipA.Y:0.00}), " +
                $"B=({shipB.X:0.00}, {shipB.Y:0.00})");
        }
        else
        {
            RemoveEncounter(
                shipA,
                shipB);
        }
    }

    private void RemoveEncounter(
        ShipState shipA,
        ShipState shipB)
    {
        string encounterKey =
            CreateEncounterKey(
                shipA,
                shipB);

        if (_activeEncounters.Remove(
                encounterKey,
                out ShipEncounter? encounter))
        {
            Log.Info(
                $"Ship encounter ended: " +
                $"{encounter.ShipA.ShipName} <-> " +
                $"{encounter.ShipB.ShipName}");
        }
    }

    private string CreateEncounterKey(
        ShipState shipA,
        ShipState shipB)
    {
        if (string.Compare(
                shipA.ShipName,
                shipB.ShipName,
                StringComparison.Ordinal) < 0)
        {
            return $"{shipA.ShipName}|{shipB.ShipName}";
        }

        return $"{shipB.ShipName}|{shipA.ShipName}";
    }
}


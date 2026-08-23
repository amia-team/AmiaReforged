using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(SailingAreaService))]
public class SailingAreaService
{
    private const float AreaMinX = 0.0f;
    private const float AreaMaxX = 640.0f;

    private const float AreaMinY = 0.0f;
    private const float AreaMaxY = 640.0f;

    private readonly Dictionary<
        string,
        SailingArea>
        _sailingAreas =
            new(StringComparer.OrdinalIgnoreCase);

    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    public SailingAreaService()
    {
        RegisterSailingAreas();

        Log.Info(
            "Sailing Area Service initialized. " +
            $"Registered {_sailingAreas.Count} sailing area(s).");
    }

    // -----------------------------------------------------------------
    // Registration
    // -----------------------------------------------------------------

    private void RegisterSailingAreas()
    {
        SailingArea ocean01 = new()
        {
            AreaResRef = "ocean_01",

            MinX = AreaMinX,
            MaxX = AreaMaxX,

            MinY = AreaMinY,
            MaxY = AreaMaxY,

            NorthAreaResRef = "ocean_005",
            SouthAreaResRef = "ocean_004",
            EastAreaResRef = "ocean_002",
            WestAreaResRef = "ocean_003",

            NorthEntry = CreateLocation(
                "ocean_01",
                80.0f,
                5.0f),

            SouthEntry = CreateLocation(
                "ocean_01",
                80.0f,
                155.0f),

            EastEntry = CreateLocation(
            "ocean_01",
             640.0f,
                80.0f),

            WestEntry = CreateLocation(
            "ocean_01",
                0.0f,
                 80.0f),
        };

        SailingArea ocean02 = new()
        {
            AreaResRef = "ocean_002",

            MinX = AreaMinX,
            MaxX = AreaMaxX,

            MinY = AreaMinY,
            MaxY = AreaMaxY,

            WestAreaResRef = "ocean_01",

            WestEntry = CreateLocation(
    "ocean_002",
    0.0f,
    80.0f),
        };

        SailingArea ocean03 = new()
        {
            AreaResRef = "ocean_003",

            MinX = AreaMinX,
            MaxX = AreaMaxX,

            MinY = AreaMinY,
            MaxY = AreaMaxY,

            EastAreaResRef = "ocean_01",

EastEntry = CreateLocation(
    "ocean_003",
    640.0f,
    80.0f),
        };

        SailingArea ocean04 = new()
        {
            AreaResRef = "ocean_004",

            MinX = AreaMinX,
            MaxX = AreaMaxX,

            MinY = AreaMinY,
            MaxY = AreaMaxY,

            NorthAreaResRef = "ocean_01",

NorthEntry = CreateLocation(
    "ocean_004",
    80.0f,
    640.0f),
        };

        SailingArea ocean05 = new()
        {
            AreaResRef = "ocean_005",

            MinX = AreaMinX,
            MaxX = AreaMaxX,

            MinY = AreaMinY,
            MaxY = AreaMaxY,

            SouthAreaResRef = "ocean_01",

SouthEntry = CreateLocation(
    "ocean_005",
    80.0f,
    0.0f),
        };

        _sailingAreas[
            ocean01.AreaResRef] =
            ocean01;

        _sailingAreas[
            ocean02.AreaResRef] =
            ocean02;

        _sailingAreas[
            ocean03.AreaResRef] =
            ocean03;

        _sailingAreas[
            ocean04.AreaResRef] =
            ocean04;

        _sailingAreas[
            ocean05.AreaResRef] =
            ocean05;

        Log.Info(
            "Sailing areas registered: " +
            "ocean_01, ocean_002, ocean_003, " +
            "ocean_004, ocean_005.");
    }

    // -----------------------------------------------------------------
    // Location Helper
    // -----------------------------------------------------------------

    private static SailingLocation CreateLocation(
        string areaResRef,
        float x,
        float y,
        float z = 0.0f,
        float rotation = 0.0f)
    {
        return new SailingLocation
        {
            AreaResRef =
                areaResRef,

            X =
                x,

            Y =
                y,

            Z =
                z,

            Rotation =
                rotation
        };
    }

    // -----------------------------------------------------------------
    // Lookup
    // -----------------------------------------------------------------

    public SailingArea? GetArea(
        string areaResRef)
    {
        if (string.IsNullOrWhiteSpace(
                areaResRef))
        {
            return null;
        }

        return _sailingAreas.TryGetValue(
            areaResRef,
            out SailingArea? area)
            ? area
            : null;
    }

    public bool TryGetArea(
        string areaResRef,
        out SailingArea? area)
    {
        return _sailingAreas.TryGetValue(
            areaResRef,
            out area);
    }

    public bool ContainsArea(
        string areaResRef)
    {
        return !string.IsNullOrWhiteSpace(
                   areaResRef) &&
               _sailingAreas.ContainsKey(
                   areaResRef);
    }

    public IReadOnlyCollection<SailingArea>
        GetAreas()
    {
        return _sailingAreas.Values;
    }

    // -----------------------------------------------------------------
    // Neighbors
    // -----------------------------------------------------------------

    public IEnumerable<string> GetNeighbors(
        string areaResRef)
    {
        if (!_sailingAreas.TryGetValue(
                areaResRef,
                out SailingArea? area))
        {
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(
                area.NorthAreaResRef))
        {
            yield return area.NorthAreaResRef;
        }

        if (!string.IsNullOrWhiteSpace(
                area.SouthAreaResRef))
        {
            yield return area.SouthAreaResRef;
        }

        if (!string.IsNullOrWhiteSpace(
                area.EastAreaResRef))
        {
            yield return area.EastAreaResRef;
        }

        if (!string.IsNullOrWhiteSpace(
                area.WestAreaResRef))
        {
            yield return area.WestAreaResRef;
        }
    }

    // -----------------------------------------------------------------
    // Connection Direction
    // -----------------------------------------------------------------

    public string GetConnectionDirection(
        string currentAreaResRef,
        string nextAreaResRef)
    {
        if (!_sailingAreas.TryGetValue(
                currentAreaResRef,
                out SailingArea? area))
        {
            return string.Empty;
        }

        if (string.Equals(
                area.NorthAreaResRef,
                nextAreaResRef,
                StringComparison.OrdinalIgnoreCase))
        {
            return "North";
        }

        if (string.Equals(
                area.SouthAreaResRef,
                nextAreaResRef,
                StringComparison.OrdinalIgnoreCase))
        {
            return "South";
        }

        if (string.Equals(
                area.EastAreaResRef,
                nextAreaResRef,
                StringComparison.OrdinalIgnoreCase))
        {
            return "East";
        }

        if (string.Equals(
                area.WestAreaResRef,
                nextAreaResRef,
                StringComparison.OrdinalIgnoreCase))
        {
            return "West";
        }

        return string.Empty;
    }

    // -----------------------------------------------------------------
    // Area Entry
    // -----------------------------------------------------------------

    public SailingLocation? GetEntryLocation(
        string areaResRef,
        string entryDirection)
    {
        if (!_sailingAreas.TryGetValue(
                areaResRef,
                out SailingArea? area))
        {
            return null;
        }

        return entryDirection switch
        {
            "North" =>
                area.NorthEntry,

            "South" =>
                area.SouthEntry,

            "East" =>
                area.EastEntry,

            "West" =>
                area.WestEntry,

            _ =>
                null
        };
    }
}
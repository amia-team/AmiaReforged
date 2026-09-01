using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(IslandService))]
public sealed class IslandService
{
    private readonly List<IslandLocation> _islands =
    [
        // -------------------------------------------------------------
        // Driftwood Isle
        // -------------------------------------------------------------
        new IslandLocation
        {
            Id = "driftwood_isle",
            Name = "Driftwood Isle",

            OceanArea = "ocean_01",

            OceanX = 120f,
            OceanY = 90f,

            DockRadius = 10f,

            LandingArea = "sea_islet1",
            LandingX = 24f,
            LandingY = 207f,
            LandingZ = 0f
        },

        // -------------------------------------------------------------
        // Southport
        // -------------------------------------------------------------
        new IslandLocation
        {
            Id = "southport",
            Name = "Southport",

            OceanArea = "ocean_01",

            OceanX = 90f,
            OceanY = 100f,

            DockRadius = 10f,

            // ---------------------------------------------------------
            // 
            // Replace these once the Southport area is built.
            // ---------------------------------------------------------
            LandingArea = "cordor_west",
            LandingX = 75f,
            LandingY = 45f,
            LandingZ = 0f
        }
    ];

    public IslandLocation? GetNearestIsland(
        ShipState ship)
    {
        return _islands
            .Where(i =>
                string.Equals(
                    i.OceanArea,
                    ship.AreaResRef,
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(i =>
            {
                float dx = i.OceanX - ship.X;
                float dy = i.OceanY - ship.Y;

                return dx * dx + dy * dy;
            })
            .FirstOrDefault();
    }
}
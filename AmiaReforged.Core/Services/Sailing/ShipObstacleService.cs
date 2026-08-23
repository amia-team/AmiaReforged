using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipObstacleService))]
public class ShipObstacleService
{
    private readonly Dictionary<string, List<SailingObstacle>>
        _obstaclesByArea = new();

    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    public ShipObstacleService()
    {
        RegisterObstacles();

        Log.Info(
            $"Ship Obstacle Service initialized. " +
            $"Registered {_obstaclesByArea.Values.Sum(x => x.Count)} obstacle(s).");
    }

    /// <summary>
    /// Determines whether a ship can occupy the specified
    /// position in the specified sailing area.
    /// </summary>
    public bool CanMoveTo(
        string areaResRef,
        float x,
        float y)
    {
        if (!_obstaclesByArea.TryGetValue(
                areaResRef,
                out List<SailingObstacle>? obstacles))
        {
            return true;
        }

        foreach (SailingObstacle obstacle in obstacles)
        {
            if (x >= obstacle.MinX &&
                x <= obstacle.MaxX &&
                y >= obstacle.MinY &&
                y <= obstacle.MaxY)
            {
                Log.Info(
                    $"Movement blocked by obstacle: " +
                    $"Obstacle={obstacle.Name}, " +
                    $"Area={areaResRef}, " +
                    $"X={x:0.00}, " +
                    $"Y={y:0.00}");

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns the obstacle occupying the specified position,
    /// if one exists.
    /// </summary>
    public SailingObstacle? GetObstacleAt(
        string areaResRef,
        float x,
        float y)
    {
        if (!_obstaclesByArea.TryGetValue(
                areaResRef,
                out List<SailingObstacle>? obstacles))
        {
            return null;
        }

        foreach (SailingObstacle obstacle in obstacles)
        {
            if (x >= obstacle.MinX &&
                x <= obstacle.MaxX &&
                y >= obstacle.MinY &&
                y <= obstacle.MaxY)
            {
                return obstacle;
            }
        }

        return null;
    }
    /// <summary>
/// Returns every obstacle registered in the specified sailing area.
/// </summary>
public IReadOnlyCollection<SailingObstacle> GetObstacles(
    string areaResRef)
{
    if (!_obstaclesByArea.TryGetValue(
            areaResRef,
            out List<SailingObstacle>? obstacles))
    {
        return Array.Empty<SailingObstacle>();
    }

    return obstacles;
}

private void RegisterObstacles()
{
    // =============================================================
    // Main Amia landmass
    // Ocean scale: 640 x 640
    // These are coarse collision volumes matching the chart.
    // =============================================================
/*
    RegisterObstacle(new SailingObstacle
    {
        Name = "Amia North",
        AreaResRef = "ocean_01",
        MinX = 63f,
        MaxX = 90f,
        MinY = 34f,
        MaxY = 60f
    });

    RegisterObstacle(new SailingObstacle
    {
        Name = "Amia Central",
        AreaResRef = "ocean_01",
        MinX = 55f,
        MaxX = 95f,
        MinY = 60f,
        MaxY = 90f
    });

    RegisterObstacle(new SailingObstacle
    {
        Name = "Amia South",
        AreaResRef = "ocean_01",
        MinX = 60f,
        MaxX = 88f,
        MinY = 90f,
        MaxY = 120f
    });

    RegisterObstacle(new SailingObstacle
    {
        Name = "West Peninsula",
        AreaResRef = "ocean_01",
        MinX = 46f,
        MaxX = 62f,
        MinY = 52f,
        MaxY = 122f
    });

    RegisterObstacle(new SailingObstacle
    {
        Name = "East Coast",
        AreaResRef = "ocean_01",
        MinX = 90f,
        MaxX = 103f,
        MinY = 46f,
        MaxY = 92f
    });

    RegisterObstacle(new SailingObstacle
    {
        Name = "South-east Peninsula",
        AreaResRef = "ocean_01",
        MinX = 82f,
        MaxX = 102f,
        MinY = 92f,
        MaxY = 118f
    });

    RegisterObstacle(new SailingObstacle
    {
        Name = "South-west Hook",
        AreaResRef = "ocean_01",
        MinX = 42f,
        MaxX = 56f,
        MinY = 104f,
        MaxY = 138f
    });
*/
    RegisterObstacle(new SailingObstacle
    {
        Name = "North-east Isle",
        AreaResRef = "ocean_01",
        MinX = 120f,
        MaxX = 90f,
        MinY = 18f,
        MaxY = 36f
    });
}
 
    private void RegisterObstacle(
        SailingObstacle obstacle)
    {
        if (!_obstaclesByArea.TryGetValue(
                obstacle.AreaResRef,
                out List<SailingObstacle>? obstacles))
        {
            obstacles = new List<SailingObstacle>();

            _obstaclesByArea[
                obstacle.AreaResRef] =
                obstacles;
        }

        obstacles.Add(obstacle);

        Log.Info(
            $"Sailing obstacle registered: " +
            $"Name={obstacle.Name}, " +
            $"Area={obstacle.AreaResRef}, " +
            $"X={obstacle.MinX:0.00}-{obstacle.MaxX:0.00}, " +
            $"Y={obstacle.MinY:0.00}-{obstacle.MaxY:0.00}");
    }
}
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

    private void RegisterObstacles()
    {
        // Test obstacle.
        //
        // This is deliberately temporary.
        // We will replace this with the actual
        // Amia sailing map obstacles later.

        RegisterObstacle(
            new SailingObstacle
            {
                Name = "Test Island",
                AreaResRef = "ocean_01",
                MinX = 70.0f,
                MaxX = 80.0f,
                MinY = 70.0f,
                MaxY = 80.0f
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
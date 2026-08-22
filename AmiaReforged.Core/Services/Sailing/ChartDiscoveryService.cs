using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ChartDiscoveryService))]
public sealed class ChartDiscoveryService
{
    private const int GridSize = 16;
    private const float WorldSize = 160f;
    private const int RevealRadius = 1;

    private readonly Dictionary<string, Dictionary<string, bool[,]>> _discovery = new();

    public bool IsDiscovered(
        NwPlayer player,
        string areaResRef,
        int gridX,
        int gridY)
    {
        if (!_discovery.TryGetValue(player.PlayerName, out var areas))
        {
            return false;
        }

        if (!areas.TryGetValue(areaResRef, out var grid))
        {
            return false;
        }

        return grid[gridX, gridY];
    }

    public void RevealAroundShip(
        NwPlayer player,
        ShipState ship)
    {
        bool[,] grid = GetOrCreateGrid(
            player.PlayerName,
            ship.AreaResRef);

        int centerX = Math.Clamp((int)(ship.X / (WorldSize / GridSize)), 0, GridSize - 1);
        int centerY = Math.Clamp((int)(ship.Y / (WorldSize / GridSize)), 0, GridSize - 1);

        for (int x = centerX - RevealRadius; x <= centerX + RevealRadius; x++)
        {
            for (int y = centerY - RevealRadius; y <= centerY + RevealRadius; y++)
            {
                if (x < 0 || y < 0 || x >= GridSize || y >= GridSize)
                {
                    continue;
                }

                grid[x, y] = true;
            }
        }
    }

    private bool[,] GetOrCreateGrid(
        string playerName,
        string areaResRef)
    {
        if (!_discovery.TryGetValue(playerName, out var areas))
        {
            areas = new();
            _discovery[playerName] = areas;
        }

        if (!areas.TryGetValue(areaResRef, out var grid))
        {
            grid = new bool[GridSize, GridSize];
            areas[areaResRef] = grid;
        }

        return grid;
    }
}
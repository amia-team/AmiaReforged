using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(MerchantTradeService))]
public sealed class MerchantTradeService
{
    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private readonly Dictionary<string, PortTradeDefinition>
        _ports =
            new(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "driftwood",
                    new PortTradeDefinition
                    {
                        PortId = "driftwood",
                        BuyItems =
                        {
                            "grain"
                        },
                        SellItems =
                        {
                            "timber"
                        }
                    }
                },
                {
                    "southport",
                    new PortTradeDefinition
                    {
                        PortId = "southport",
                        BuyItems =
                        {
                            "timber"
                        },
                        SellItems =
                        {
                            "grain"
                        }
                    }
                }
            };

    public void ExecuteTrade(
        ShipState ship,
        string portId)
    {
        if (ship.ShipType != ShipType.Merchant)
        {
            return;
        }

        if (!_ports.TryGetValue(
                portId,
                out PortTradeDefinition? port))
        {
            Log.Warn(
                $"No trade definition exists for port '{portId}'.");

            return;
        }

        Log.Info(
            $"Merchant '{ship.ShipName}' trading at " +
            $"{port.PortId}.");

        // ---------------------------------------------------------
        // Sell cargo that this port buys.
        // ---------------------------------------------------------

        foreach (MerchantCargo cargo in ship.Cargo)
        {
            if (!port.BuyItems.Contains(
                    cargo.ItemId,
                    StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            Log.Info(
                $"Merchant '{ship.ShipName}' sold " +
                $"{cargo.Quantity} units of {cargo.ItemId} " +
                $"at {port.PortId}.");

            cargo.Quantity = 0;
        }

        ship.Cargo.RemoveAll(
            cargo => cargo.Quantity <= 0);

        // ---------------------------------------------------------
        // Buy one test cargo.
        // ---------------------------------------------------------

        foreach (string itemId in port.SellItems)
        {
            MerchantCargo? cargo =
                ship.Cargo.FirstOrDefault(
                    c =>
                        string.Equals(
                            c.ItemId,
                            itemId,
                            StringComparison.OrdinalIgnoreCase));

            if (cargo == null)
            {
                ship.Cargo.Add(
                    new MerchantCargo
                    {
                        ItemId = itemId,
                        Quantity = 50
                    });
            }
            else
            {
                cargo.Quantity += 50;
            }

            Log.Info(
                $"Merchant '{ship.ShipName}' bought " +
                $"50 units of {itemId} " +
                $"at {port.PortId}.");

            break;
        }
    }
}
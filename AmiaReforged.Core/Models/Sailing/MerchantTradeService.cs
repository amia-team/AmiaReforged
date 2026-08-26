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

                        // What Driftwood pays the merchant.
                        BuyPrices =
                        {
                            ["grain"] = 15
                        },

                        // What Driftwood charges the merchant.
                        SellPrices =
                        {
                            ["timber"] = 10
                        }
                    }
                },

                {
                    "southport",
                    new PortTradeDefinition
                    {
                        PortId = "southport",

                        // What Southport pays the merchant.
                        BuyPrices =
                        {
                            ["timber"] = 18
                        },

                        // What Southport charges the merchant.
                        SellPrices =
                        {
                            ["grain"] = 8
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
            $"{port.PortId}. " +
            $"Gold={ship.MerchantGold}");

        // ---------------------------------------------------------
        // SELL CURRENT CARGO
        // ---------------------------------------------------------

        foreach (MerchantCargo cargo in
                 ship.Cargo.ToList())
        {
            if (!port.BuyPrices.TryGetValue(
                    cargo.ItemId,
                    out int sellPrice))
            {
                continue;
            }

            int quantity =
                cargo.Quantity;

            int revenue =
                quantity * sellPrice;

            ship.MerchantGold += revenue;

            Log.Info(
                $"Merchant '{ship.ShipName}' sold " +
                $"{quantity} {cargo.ItemId} " +
                $"at {sellPrice} each for {revenue} gold.");

            cargo.Quantity = 0;
        }

        ship.Cargo.RemoveAll(
            cargo => cargo.Quantity <= 0);

        // ---------------------------------------------------------
        // BUY NEW CARGO
        // ---------------------------------------------------------

        foreach (KeyValuePair<string, int> item in
                 port.SellPrices)
        {
            const int purchaseQuantity = 50;

            int totalCost =
                purchaseQuantity * item.Value;

            if (ship.MerchantGold <
                totalCost)
            {
                Log.Info(
                    $"Merchant '{ship.ShipName}' cannot afford " +
                    $"{purchaseQuantity} {item.Key}. " +
                    $"Cost={totalCost}, " +
                    $"Gold={ship.MerchantGold}");

                continue;
            }

            ship.MerchantGold -=
                totalCost;

            MerchantCargo? cargo =
                ship.Cargo.FirstOrDefault(
                    c =>
                        string.Equals(
                            c.ItemId,
                            item.Key,
                            StringComparison.OrdinalIgnoreCase));

            if (cargo == null)
            {
                ship.Cargo.Add(
                    new MerchantCargo
                    {
                        ItemId = item.Key,
                        Quantity = purchaseQuantity
                    });
            }
            else
            {
                cargo.Quantity +=
                    purchaseQuantity;
            }

            Log.Info(
                $"Merchant '{ship.ShipName}' bought " +
                $"{purchaseQuantity} {item.Key} " +
                $"at {item.Value} each " +
                $"for {totalCost} gold. " +
                $"RemainingGold={ship.MerchantGold}");
        }

        Log.Info(
            $"Merchant '{ship.ShipName}' trade complete. " +
            $"Gold={ship.MerchantGold}, " +
            $"Cargo={FormatCargo(ship)}");
    }

    private static string FormatCargo(
        ShipState ship)
    {
        if (ship.Cargo.Count == 0)
        {
            return "Empty";
        }

        return string.Join(
            ", ",
            ship.Cargo.Select(
                c =>
                    $"{c.ItemId}={c.Quantity}"));
    }
}
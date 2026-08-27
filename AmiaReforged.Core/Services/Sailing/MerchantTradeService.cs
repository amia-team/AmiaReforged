using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
using Anvil.API;
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
// ---------------------------------------------------------
// BUY NEW CARGO
// ---------------------------------------------------------

foreach (KeyValuePair<string, int> item in
         port.SellPrices)
{
    const int desiredPurchaseQuantity = 50;

    // ---------------------------------------------------------
    // Determine how much cargo space remains.
    // ---------------------------------------------------------

    int currentCargo =
        ship.Cargo.Sum(
            cargo => cargo.Quantity);

    int remainingCapacity =
        ship.CargoCapacity -
        currentCargo;

    if (remainingCapacity <= 0)
    {
        Log.Info(
            $"Merchant '{ship.ShipName}' has no cargo space " +
            $"remaining. " +
            $"Cargo={currentCargo}/{ship.CargoCapacity}");

        continue;
    }

    int purchaseQuantity =
        Math.Min(
            desiredPurchaseQuantity,
            remainingCapacity);

    int totalCost =
        purchaseQuantity * item.Value;

    // ---------------------------------------------------------
    // Merchant cannot afford the full purchase.
    // Determine the maximum quantity it can afford.
    // ---------------------------------------------------------

    if (ship.MerchantGold <
        totalCost)
    {
        purchaseQuantity =
            ship.MerchantGold /
            item.Value;

        if (purchaseQuantity <= 0)
        {
            Log.Info(
                $"Merchant '{ship.ShipName}' cannot afford " +
                $"{item.Key}. " +
                $"Price={item.Value}, " +
                $"Gold={ship.MerchantGold}");

            continue;
        }

        totalCost =
            purchaseQuantity *
            item.Value;
    }

    // ---------------------------------------------------------
    // Complete the purchase.
    // ---------------------------------------------------------

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
        $"Cargo={ship.Cargo.Sum(c => c.Quantity)}/" +
        $"{ship.CargoCapacity}. " +
        $"RemainingGold={ship.MerchantGold}");
}
    }
 public bool TryBuy(
    NwCreature buyer,
    ShipState buyerShip,
    ShipState merchant,
    string itemId,
    int quantity,
    out string message)
{
    message = string.Empty;

    if (quantity <= 0)
    {
        message = "Invalid quantity.";
        return false;
    }

    if (merchant.ShipType != ShipType.Merchant)
    {
        message = "That ship is not a merchant.";
        return false;
    }

    if (string.IsNullOrWhiteSpace(
            merchant.CurrentTradePortId))
    {
        message =
            "The merchant is not currently trading.";

        return false;
    }

    if (!_ports.TryGetValue(
            merchant.CurrentTradePortId,
            out PortTradeDefinition? port))
    {
        message =
            "No market exists at this port.";

        return false;
    }

    if (!port.SellPrices.TryGetValue(
            itemId,
            out int price))
    {
        message =
            $"The merchant is not selling {itemId}.";

        return false;
    }

    MerchantCargo? merchantCargo =
        merchant.Cargo.FirstOrDefault(
            cargo =>
                string.Equals(
                    cargo.ItemId,
                    itemId,
                    StringComparison.OrdinalIgnoreCase));

    if (merchantCargo == null ||
        merchantCargo.Quantity < quantity)
    {
        message =
            $"The merchant does not have " +
            $"enough {itemId}.";

        return false;
    }

    int currentCargo =
        buyerShip.Cargo.Sum(
            cargo => cargo.Quantity);

    int freeCapacity =
        buyerShip.CargoCapacity -
        currentCargo;

    if (quantity > freeCapacity)
    {
        message =
            $"Your ship only has " +
            $"{freeCapacity} cargo space available.";

        return false;
    }

    int totalCost =
        quantity * price;

    if (buyer.Gold < (uint)totalCost)
    {
        message =
            $"You need {totalCost} gold.";

        return false;
    }

    buyer.TakeGold(
        totalCost);

    merchant.MerchantGold +=
        totalCost;

    merchantCargo.Quantity -=
        quantity;

    MerchantCargo? buyerCargo =
        buyerShip.Cargo.FirstOrDefault(
            cargo =>
                string.Equals(
                    cargo.ItemId,
                    itemId,
                    StringComparison.OrdinalIgnoreCase));

    if (buyerCargo == null)
    {
        buyerShip.Cargo.Add(
            new MerchantCargo
            {
                ItemId = itemId,
                Quantity = quantity
            });
    }
    else
    {
        buyerCargo.Quantity +=
            quantity;
    }

    merchant.Cargo.RemoveAll(
        cargo => cargo.Quantity <= 0);

    message =
        $"You bought {quantity} {itemId} " +
        $"for {totalCost} gold.";

    Log.Info(
        $"Player bought cargo: " +
        $"Player={buyer.Name}, " +
        $"Ship={buyerShip.ShipName}, " +
        $"Merchant={merchant.ShipName}, " +
        $"Item={itemId}, " +
        $"Quantity={quantity}, " +
        $"Cost={totalCost}, " +
        $"Port={merchant.CurrentTradePortId}");

    return true;
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
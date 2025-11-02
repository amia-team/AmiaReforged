using System;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public sealed class NpcShopProduct
{
    public NpcShopProduct(
        string resRef,
        int price,
        int initialStock,
        int maxStock,
        int restockAmount,
        IReadOnlyList<NpcShopLocalVariable>? localVariables = null,
        SimpleModelAppearance? appearance = null)
    {
        if (string.IsNullOrWhiteSpace(resRef))
        {
            throw new ArgumentException("ResRef must not be empty.", nameof(resRef));
        }

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), price, "Price cannot be negative.");
        }

        if (maxStock <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxStock), maxStock, "Max stock must be positive.");
        }

        if (restockAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(restockAmount), restockAmount, "Restock amount must be positive.");
        }

        ResRef = resRef;
        Price = price;
        MaxStock = maxStock;
        RestockAmount = restockAmount;
        CurrentStock = Math.Clamp(initialStock, 0, maxStock);
        LocalVariables = localVariables ?? Array.Empty<NpcShopLocalVariable>();
        Appearance = appearance;
    }

    public string ResRef { get; }
    public int Price { get; }
    public int MaxStock { get; }
    public int RestockAmount { get; }
    public int CurrentStock { get; private set; }
    public IReadOnlyList<NpcShopLocalVariable> LocalVariables { get; }
    public SimpleModelAppearance? Appearance { get; }

    public bool IsOutOfStock => CurrentStock <= 0;

    public void Restock()
    {
        if (CurrentStock >= MaxStock)
        {
            return;
        }

        CurrentStock = Math.Min(MaxStock, CurrentStock + RestockAmount);
    }

    public bool TryConsume(int quantity)
    {
        if (quantity <= 0)
        {
            return false;
        }

        if (CurrentStock < quantity)
        {
            return false;
        }

        CurrentStock -= quantity;
        return true;
    }
}

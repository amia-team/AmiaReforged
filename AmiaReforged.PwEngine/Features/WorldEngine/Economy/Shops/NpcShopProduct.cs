using System;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public sealed class NpcShopProduct
{
    public NpcShopProduct(
        long id,
        string resRef,
        string displayName,
        string? description,
        int price,
        int currentStock,
        int maxStock,
        int restockAmount,
        bool isPlayerManaged,
        int sortOrder,
        int? baseItemType,
        IReadOnlyList<NpcShopLocalVariable>? localVariables = null,
        SimpleModelAppearance? appearance = null)
    {
        if (string.IsNullOrWhiteSpace(resRef))
        {
            throw new ArgumentException("ResRef must not be empty.", nameof(resRef));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name must not be empty.", nameof(displayName));
        }

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), price, "Price cannot be negative.");
        }

        if (maxStock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxStock), maxStock, "Max stock cannot be negative.");
        }

        if (restockAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(restockAmount), restockAmount, "Restock amount cannot be negative.");
        }

        Id = id;
        ResRef = resRef;
    DisplayName = displayName;
    Description = string.IsNullOrWhiteSpace(description) ? null : description;
        Price = price;
        MaxStock = maxStock;
        RestockAmount = restockAmount;
        IsPlayerManaged = isPlayerManaged;
        SortOrder = sortOrder;
        BaseItemType = baseItemType;
        CurrentStock = Math.Clamp(currentStock, 0, maxStock == 0 ? int.MaxValue : maxStock);
        LocalVariables = localVariables ?? Array.Empty<NpcShopLocalVariable>();
        Appearance = appearance;
    }

    public long Id { get; }
    public string ResRef { get; }
    public string DisplayName { get; }
    public string? Description { get; }
    public int Price { get; }
    public int CurrentStock { get; private set; }
    public int MaxStock { get; }
    public int RestockAmount { get; }
    public bool IsPlayerManaged { get; }
    public int SortOrder { get; }
    public int? BaseItemType { get; }
    public IReadOnlyList<NpcShopLocalVariable> LocalVariables { get; }
    public SimpleModelAppearance? Appearance { get; }

    public bool IsOutOfStock => CurrentStock <= 0;

    public int Restock()
    {
        if (MaxStock <= 0)
        {
            return 0;
        }

        if (CurrentStock >= MaxStock)
        {
            return 0;
        }

        int missing = MaxStock - CurrentStock;
        int added = RestockAmount <= 0 ? missing : Math.Min(RestockAmount, missing);
        CurrentStock += added;
        return added;
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

    public void ReturnToStock(int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        if (MaxStock == 0)
        {
            CurrentStock += quantity;
            return;
        }

        CurrentStock = Math.Min(MaxStock, CurrentStock + quantity);
    }

    public void SetCurrentStock(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Stock cannot be negative.");
        }

        CurrentStock = MaxStock == 0 ? quantity : Math.Min(quantity, MaxStock);
    }
}

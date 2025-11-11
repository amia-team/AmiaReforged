# Economy Subsystem

The Economy subsystem handles all financial and transactional operations in the WorldEngine.

## Overview

The Economy subsystem provides:
- **Banking** - Coinhouse accounts, deposits, withdrawals
- **Storage** - Item storage, capacity management
- **Shops** - NPC shops and player stalls

## Structure

```
Economy/
├── IEconomySubsystem.cs          ← Public interface (facade)
├── EconomySubsystem.cs           ← Implementation
├── README.md                      ← This file
│
├── Gateways/                      ← PUBLIC API
│   ├── IBankingGateway.cs        ← Banking operations
│   ├── IStorageGateway.cs        ← Storage operations
│   └── IShopGateway.cs           ← Shop operations
│
├── Implementation/                ← INTERNAL (do not reference directly)
│   ├── BankingGateway.cs
│   ├── StorageGateway.cs
│   ├── ShopGateway.cs
│   ├── Accounts/
│   ├── Banks/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   └── Domain/
│   ├── Storage/
│   └── Shops/
│
├── UI/                            ← UI components
│   └── Banking/
│       ├── BankWindowPresenter.cs
│       ├── BankWindowView.cs
│       └── BankAccountModel.cs
│
└── Tests/                         ← Tests
    ├── Banking/
    ├── Storage/
    └── Shops/
```

## Usage

### Access via WorldEngine Facade

```csharp
public class MyService
{
    private readonly IWorldEngineFacade _worldEngine;

    [Inject]
    public MyService(IWorldEngineFacade worldEngine)
    {
        _worldEngine = worldEngine;
    }

    public async Task Example()
    {
        // Banking operations
        await _worldEngine.Economy.Banking.OpenCoinhouseAccountAsync(command);
        await _worldEngine.Economy.Banking.DepositGoldAsync(command);
        var balance = await _worldEngine.Economy.Banking.GetBalanceAsync(query);

        // Storage operations
        await _worldEngine.Economy.Storage.StoreItemAsync(command);
        var items = await _worldEngine.Economy.Storage.GetStoredItemsAsync(query);

        // Shop operations
        await _worldEngine.Economy.Shops.ClaimPlayerStallAsync(command);
    }
}
```

## Gateways (Public API)

### IBankingGateway

Banking operations for coinhouse accounts.

**Operations:**
- `OpenCoinhouseAccountAsync` - Open a new account
- `DepositGoldAsync` - Deposit gold
- `WithdrawGoldAsync` - Withdraw gold
- `GetCoinhouseAccountAsync` - Get account details
- `GetCoinhouseAccountEligibilityAsync` - Check eligibility
- `GetCoinhouseBalancesAsync` - Get balances

### IStorageGateway

Item storage and capacity management.

**Operations:**
- `StoreItemAsync` - Store an item
- `RetrieveItemAsync` - Retrieve an item
- `GetStoredItemsAsync` - List stored items
- `GetStorageCapacityAsync` - Check capacity

### IShopGateway

NPC shops and player stall management.

**Operations:**
- `ClaimPlayerStallAsync` - Claim a stall
- `ReleasePlayerStallAsync` - Release a stall
- `GetPlayerStallAsync` - Get stall details
- `ListPlayerStallItemsAsync` - List items for sale

## Implementation Details

The `Implementation/` folder contains:
- Facade implementations
- Command handlers (CQRS commands)
- Query handlers (CQRS queries)
- Domain models
- Business logic

**Do not reference these directly!** Always use the public facades.

## Testing

Tests are located in `Tests/` and mirror the structure of the implementation.

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~Economy"
```

## See Also

- [FACADE_GUIDE.md](../../FACADE_GUIDE.md) - How to use the facade
- [NAVIGATION_GUIDE.md](../../NAVIGATION_GUIDE.md) - Navigation tips
- Banking UI docs in `UI/Banking/`


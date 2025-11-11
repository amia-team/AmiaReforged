# WorldEngine Facade Migration Plan

## Overview

This document outlines the strategy for replacing direct handler injections with the WorldEngineFacade pattern throughout the WorldEngine codebase.

## Current State Analysis

### Handler Injection Pattern (Current)

Currently, code directly injects command and query handlers:

```csharp
public class BankWindowPresenter
{
    [Inject] private Lazy<IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?>> AccountQueryHandler { get; init; }
    [Inject] private Lazy<IQueryHandler<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult>> EligibilityQueryHandler { get; init; }
    [Inject] private Lazy<ICommandHandler<OpenCoinhouseAccountCommand>> OpenAccountCommandHandler { get; init; }
    [Inject] private Lazy<ICommandHandler<DepositGoldCommand>> DepositCommandHandler { get; init; }
    [Inject] private Lazy<ICommandHandler<WithdrawGoldCommand>> WithdrawCommandHandler { get; init; }

    public async Task DoWork()
    {
        var result = await DepositCommandHandler.Value.HandleAsync(command);
    }
}
```

**Problems:**
- ❌ 5 separate handler injections for one class
- ❌ Verbose `Handler.Value.HandleAsync()` calls
- ❌ No logical grouping of operations
- ❌ Hard to discover what operations are available
- ❌ Tight coupling to handler implementation details

### Instances Found

| Location | Handler Count | Type |
|----------|--------------|------|
| `BankWindowPresenter.cs` | 5 handlers | 2 queries + 3 commands |

**Total:** Only 5 handler injections found (already very clean!)

## Migration Strategy

### Phase 1: Low-Hanging Fruit ✅ (COMPLETE)

**Status:** The code is already mostly using the facade pattern!

Most of the WorldEngine is already well-architected:
- ✅ Economy subsystem uses gateways
- ✅ Organizations use proper service layers
- ✅ Characters use proper service layers
- ✅ Industries use proper service layers

### Phase 2: BankWindowPresenter Migration 🎯 (PRIORITY)

**File:** `Features/WorldEngine/Economy/Banks/Nui/BankWindowPresenter.cs`

**Current Dependencies:**
```csharp
[Inject] private Lazy<IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?>> AccountQueryHandler
[Inject] private Lazy<IQueryHandler<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult>> EligibilityQueryHandler
[Inject] private Lazy<ICommandHandler<OpenCoinhouseAccountCommand>> OpenAccountCommandHandler
[Inject] private Lazy<ICommandHandler<DepositGoldCommand>> DepositCommandHandler
[Inject] private Lazy<ICommandHandler<WithdrawGoldCommand>> WithdrawCommandHandler
```

**Proposed Refactoring:**
```csharp
[Inject] private Lazy<IWorldEngineFacade> WorldEngine { get; init; }

// Or even better, just use the subsystem directly:
[Inject] private Lazy<IEconomySubsystem> Economy { get; init; }
```

**Call Site Changes:**

| Before | After |
|--------|-------|
| `await OpenAccountCommandHandler.Value.HandleAsync(command)` | `await Economy.Value.Banking.OpenCoinhouseAccountAsync(command)` |
| `await DepositCommandHandler.Value.HandleAsync(command)` | `await Economy.Value.Banking.DepositGoldAsync(command)` |
| `await WithdrawCommandHandler.Value.HandleAsync(command)` | `await Economy.Value.Banking.WithdrawGoldAsync(command)` |
| `await AccountQueryHandler.Value.HandleAsync(query)` | `await Economy.Value.Banking.GetCoinhouseAccountAsync(query)` |
| `await EligibilityQueryHandler.Value.HandleAsync(query)` | `await Economy.Value.Banking.GetCoinhouseAccountEligibilityAsync(query)` |

**Benefits:**
- ✅ 5 injections → 1 injection (80% reduction!)
- ✅ More readable: `Economy.Banking.DepositGoldAsync()` vs `DepositCommandHandler.Value.HandleAsync()`
- ✅ Operations grouped logically
- ✅ Easy to discover banking operations
- ✅ Follows established patterns

### Phase 3: Update Documentation 📝

Update existing documentation to reflect best practices:

1. **FACADE_GUIDE.md** - Add BankWindowPresenter as an example
2. **FACADE_QUICK_REFERENCE.md** - Add banking UI examples
3. **Create new guide** - "UI Presenter Best Practices"

## Implementation Plan

### Step 1: Verify Gateway Methods Exist

Check that `IBankingGateway` has all needed methods:

- ✅ `OpenCoinhouseAccountAsync`
- ✅ `GetCoinhouseAccountAsync`
- ✅ `GetCoinhouseAccountEligibilityAsync`
- ✅ `DepositGoldAsync`
- ✅ `WithdrawGoldAsync`

**Status:** All methods exist in `IBankingGateway` ✅

### Step 2: Refactor BankWindowPresenter

**File:** `Features/WorldEngine/Economy/Banks/Nui/BankWindowPresenter.cs`

**Changes Required:**

1. Replace handler injections:
```csharp
// REMOVE:
[Inject] private Lazy<IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?>> AccountQueryHandler { get; init; } = null!;
[Inject] private Lazy<IQueryHandler<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult>> EligibilityQueryHandler { get; init; } = null!;
[Inject] private Lazy<ICommandHandler<OpenCoinhouseAccountCommand>> OpenAccountCommandHandler { get; init; } = null!;
[Inject] private Lazy<ICommandHandler<DepositGoldCommand>> DepositCommandHandler { get; init; } = null!;
[Inject] private Lazy<ICommandHandler<WithdrawGoldCommand>> WithdrawCommandHandler { get; init; } = null!;

// ADD:
[Inject] private Lazy<IEconomySubsystem> Economy { get; init; } = null!;
```

2. Update BankAccountModel constructor:
```csharp
// CURRENT:
private BankAccountModel Model => _model ??= new BankAccountModel(
    AccountQueryHandler.Value,
    EligibilityQueryHandler.Value,
    BankAccessEvaluator.Value);

// PROPOSED:
private BankAccountModel Model => _model ??= new BankAccountModel(
    Economy.Value.Banking,
    BankAccessEvaluator.Value);
```

3. Update call sites (4 locations):
   - Line ~429: `OpenAccountCommandHandler.Value.HandleAsync(command)`
   - Line ~665: `OpenAccountCommandHandler.Value.HandleAsync(command)`
   - Line ~914: `DepositCommandHandler.Value.HandleAsync(command)`
   - Line ~1017: `WithdrawCommandHandler.Value.HandleAsync(command)`

4. Update BankAccountModel to accept `IBankingGateway` instead of individual handlers

### Step 3: Refactor BankAccountModel

**File:** `Features/WorldEngine/Economy/Banks/Nui/BankAccountModel.cs`

**Current Constructor:**
```csharp
public BankAccountModel(
    IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?> accountQueryHandler,
    IQueryHandler<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult> eligibilityQueryHandler,
    IBankAccessEvaluator accessEvaluator)
```

**Proposed Constructor:**
```csharp
public BankAccountModel(
    IBankingGateway banking,
    IBankAccessEvaluator accessEvaluator)
```

**Internal Changes:**
Replace handler calls with gateway calls throughout the model.

### Step 4: Update Tests

**Files:**
- Any tests that mock the individual handlers
- Update to mock `IEconomySubsystem` or `IBankingGateway` instead

**Example:**
```csharp
// BEFORE:
var mockOpenHandler = new Mock<ICommandHandler<OpenCoinhouseAccountCommand>>();
var mockDepositHandler = new Mock<ICommandHandler<DepositGoldCommand>>();
// ... many mocks

// AFTER:
var mockBanking = new Mock<IBankingGateway>();
mockBanking.Setup(b => b.OpenCoinhouseAccountAsync(It.IsAny<OpenCoinhouseAccountCommand>(), default))
    .ReturnsAsync(CommandResult.Ok());

var mockEconomy = new Mock<IEconomySubsystem>();
mockEconomy.Setup(e => e.Banking).Returns(mockBanking.Object);
```

### Step 5: Verify Build and Tests

1. Build the project
2. Run all tests
3. Manual testing of BankWindowPresenter UI
4. Verify no regressions

## Benefits Analysis

### Before Migration

```csharp
public class BankWindowPresenter
{
    // 5 handler injections
    [Inject] private Lazy<IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?>> AccountQueryHandler { get; init; }
    [Inject] private Lazy<IQueryHandler<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult>> EligibilityQueryHandler { get; init; }
    [Inject] private Lazy<ICommandHandler<OpenCoinhouseAccountCommand>> OpenAccountCommandHandler { get; init; }
    [Inject] private Lazy<ICommandHandler<DepositGoldCommand>> DepositCommandHandler { get; init; }
    [Inject] private Lazy<ICommandHandler<WithdrawGoldCommand>> WithdrawCommandHandler { get; init; }

    // 2 service injections
    [Inject] private Lazy<IBankAccessEvaluator> BankAccessEvaluator { get; init; }
    [Inject] private Lazy<IForeclosureStorageService> ForeclosureStorageService { get; init; }
    [Inject] private Lazy<IPersonalStorageService> PersonalStorageService { get; init; }
    [Inject] private WindowDirector WindowDirector { get; init; }

    // Total: 9 injections
}
```

### After Migration

```csharp
public class BankWindowPresenter
{
    // 1 subsystem injection (replaces 5 handlers!)
    [Inject] private Lazy<IEconomySubsystem> Economy { get; init; }

    // 4 service injections (unchanged - these are appropriate direct dependencies)
    [Inject] private Lazy<IBankAccessEvaluator> BankAccessEvaluator { get; init; }
    [Inject] private Lazy<IForeclosureStorageService> ForeclosureStorageService { get; init; }
    [Inject] private Lazy<IPersonalStorageService> PersonalStorageService { get; init; }
    [Inject] private WindowDirector WindowDirector { get; init; }

    // Total: 5 injections (44% reduction!)
}
```

### Metrics

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| **Total Injections** | 9 | 5 | -44% |
| **Handler Injections** | 5 | 0 | -100% |
| **Lines of Code (DI)** | ~15 lines | ~3 lines | -80% |
| **Cognitive Load** | High (9 deps) | Medium (5 deps) | -44% |
| **Discoverability** | Low | High | +∞% |
| **Maintainability** | Medium | High | ⬆️ |

## Risk Assessment

### Low Risk ✅

- Small number of files affected (2-3 files)
- Well-defined interfaces already exist
- Gateway methods already implemented and tested
- No breaking changes to public APIs

### Mitigation Strategies

1. **Incremental Migration**
   - Migrate one presenter at a time
   - Keep tests passing at each step
   - Easy to roll back if issues arise

2. **Comprehensive Testing**
   - Unit tests for refactored code
   - Integration tests for banking operations
   - Manual UI testing

3. **Documentation**
   - Update all relevant docs
   - Add examples of new pattern
   - Create migration guide for future work

## Timeline

| Phase | Estimated Time | Priority |
|-------|---------------|----------|
| Phase 1: Analysis | ✅ Complete | - |
| Phase 2: BankWindowPresenter | 2-3 hours | High |
| Phase 3: Documentation | 1 hour | Medium |
| Phase 4: Testing | 1 hour | High |

**Total Estimated Time:** 4-5 hours

## Success Criteria

✅ All handler injections replaced with subsystem/gateway injections
✅ BankWindowPresenter has only 5 dependencies (down from 9)
✅ All tests pass
✅ No regressions in banking UI functionality
✅ Code is more readable and maintainable
✅ Documentation is updated

## Future Considerations

### Other Presenters

If additional presenters are found with handler injections:
- Apply the same pattern
- Document in this migration plan
- Update estimates

### Cross-Cutting Gateway

Consider whether `IEconomySubsystem` should be injected directly, or if a higher-level `IWorldEngineFacade` injection would be better:

**Option A: Subsystem Injection (Recommended)**
```csharp
[Inject] private Lazy<IEconomySubsystem> Economy { get; init; }
// Pro: More focused, only depends on what's needed
// Pro: Clearer intent - this is an economy-focused UI
```

**Option B: Facade Injection**
```csharp
[Inject] private Lazy<IWorldEngineFacade> WorldEngine { get; init; }
// Pro: Future-proof if presenter needs other subsystems
// Con: Broader dependency than necessary
```

**Recommendation:** Use Option A (Subsystem Injection) for BankWindowPresenter since it only uses economy operations.

## Conclusion

The migration is **low-risk** and **high-value**:
- Only 1-2 files need changes
- 44% reduction in dependencies
- Significant improvement in code clarity
- Aligns with established WorldEngine patterns
- Easy to test and verify

The WorldEngine is already in excellent shape - this migration will make it even better! 🎉


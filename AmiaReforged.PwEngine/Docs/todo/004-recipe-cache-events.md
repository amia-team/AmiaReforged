# 004 — Invalidate recipe expansion caches through events

Status: **Open**
Type: **Implementation**
Audit area: **F-1**
Depends on: None.

## Current gap

Recipe-template cache invalidation is a direct controller side effect.

## Change

Subscribe to successful template changes and invalidate the expander there. Remove `InvalidateExpansionCache` from the controller and document the asynchronous freshness contract.

## Starting points

- [API/Controllers/RecipeTemplateController.cs](../../Features/WorldEngine/API/Controllers/RecipeTemplateController.cs)
- [Application/Industries/Commands/RecipeTemplateCommands.cs](../../Features/WorldEngine/Application/Industries/Commands/RecipeTemplateCommands.cs)

## Acceptance checks

- [x] Create/update/delete all trigger invalidation after success, including non-HTTP callers.
  (Handled via `CommandExecutedEvent` — any dispatcher-driven caller is covered, not just HTTP.)
- [x] A failed mutation does not invalidate.
  (Dispatcher publishes the event only when `result.Success`; handler never sees failures.)
- [x] Controller code no longer owns invalidation; tests cover the freshness contract.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

## Decision

Invalidation moved from a synchronous controller side effect to an **asynchronous domain-event reaction**.

The command dispatcher (`SharedKernel/Commands/CommandDispatcher.cs`) already publishes a
`CommandExecutedEvent<TCommand>` for every *successful* command. We subscribe to that event for the
three template-mutation commands instead of calling `RecipeTemplateExpander.Invalidate()` directly.
This gives the desired properties for free: invalidation fires only after commit, it is decoupled
from HTTP (any dispatcher-driven caller — admin panel, bulk import, tests — is covered), and a
failed mutation never invalidates.

The force-invalidate endpoint (`POST .../recipe-templates/invalidate`) still calls the expander
directly, since it is not driven by a command.

## Changed files

- `Features/WorldEngine/Application/Industries/Events/RecipeTemplateCacheInvalidationHandler.cs` (new)
  — `IEventHandler` for `CommandExecutedEvent<Create/Update/DeleteRecipeTemplateCommand>`; calls
  `RecipeTemplateExpander.Invalidate()` on each.
- `Features/WorldEngine/API/Controllers/RecipeTemplateController.cs` — removed the three
  `InvalidateExpansionCache()` calls and the helper; the force endpoint keeps a direct
  `ResolveExpander().Invalidate()`.
- `Features/WorldEngine/Subsystems/Industries/RecipeTemplateExpander.cs` — `Invalidate()` made
  `virtual` (testability; no behaviour change).
- `Features/WorldEngine/Subsystems/Industries/Tests/RecipeTemplateCacheInvalidationHandlerTests.cs` (new)
  — covers create/update/delete + combined once-per-mutation.

## Asynchronous freshness contract

- Invalidation is published by the command dispatcher **only on success** and processed on a
  background thread by `AnvilEventBusService`. The mutating request does **not** block on a full
  re-expansion.
- The very next expansion-consuming query calls `EnsureExpanded()`, which re-expands on demand if
  the cache was invalidated, so reads converge to fresh data shortly after the event is processed.
- A failed mutation does not publish the event, so the cache is left untouched (no stale rebuild).
- Ordering between handlers is not guaranteed; this handler is independent of every other subscriber.

## Verification

```
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj -c Debug
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-build \
  --filter "FullyQualifiedName~RecipeTemplateCacheInvalidationHandlerTests"
```

Result: build clean (0 errors); 4/4 handler tests pass. Full `Industries.Tests` suite: 169/169
pass. The controller no longer references `InvalidateExpansionCache` / `ResolveExpander` on the
mutation paths (grep confirms the only remaining direct expander call is the force endpoint).

See [backlog scope and completion rules](README.md).


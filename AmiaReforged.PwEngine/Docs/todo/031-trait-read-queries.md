# 031 — Dispatch trait definition and ownership reads

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Traits**
Depends on: None.

## Current gap

Definition lookup/list, character trait list, and ownership checks all read repositories directly.

## Change

Reuse existing trait queries where their result contracts match; add missing query projections for `GetTraitAsync`, `GetAllTraitsAsync`, `GetCharacterTraitsAsync`, and `HasTraitAsync`.

## Starting points

- [Subsystems/Implementations/TraitSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/TraitSubsystem.cs)

## Acceptance checks

- [x] Tests cover missing definitions, empty ownership, and inactive traits in `HasTraitAsync`.
- [x] Existing public projections remain compatible.
- [x] These four subsystem methods only dispatch/map results; they do not read repositories.

## Completion evidence

### Chosen behavior
The four subsystem read methods (`GetTraitAsync`, `GetAllTraitsAsync`, `GetCharacterTraitsAsync`, `HasTraitAsync`) now only dispatch queries and map results. The subsystem injects `IQueryDispatcher` and routes each method through the existing CQRS queries:

- `GetTraitAsync` → `GetTraitDefinitionQuery` (`IQuery<Trait?>`) → handler reads `_traitRepository`.
- `GetAllTraitsAsync` → `GetAllTraitsQuery` → handler reads `_traitRepository`.
- `GetCharacterTraitsAsync` → `GetCharacterTraitsQuery` → handler (implemented in `GetAllTraitsQueryHandler.cs`) reads both `_characterTraitRepository` and `_traitRepository`, enriching each selection with `Name` (falls back to the tag when the definition is missing).
- `HasTraitAsync` → new `HasTraitAsyncQuery`/`HasTraitAsyncQueryHandler` (reads both repos; returns `false` for a missing definition, empty ownership, or an inactive trait).

`CalculateTraitEffectsAsync` still uses the repository fields directly (out of scope for this task).

### Type-mapping note
The domain `CharacterTrait` entity (in `...Traits`) is distinct from the public projection `CharacterTrait` record (in `...Subsystems`). To avoid ambiguity in the subsystem, `DomainCharacterTrait`, `ITraitRepository`, `ICharacterTraitRepository`, and `Trait` are imported via explicit aliases, and the public projection is aliased as `PublicCharacterTrait`. The four `DispatchAsync` calls use explicit `<TQuery, TResult>` type arguments (the dispatcher cannot infer `TResult` from a single query argument).

### Changed files
- `Features/WorldEngine/Subsystems/Implementations/TraitSubsystem.cs` — injected `IQueryDispatcher`; refactored the four read methods to dispatch/map; added projection aliases.
- `Features/WorldEngine/Subsystems/Traits/Application/GetAllTraitsQueryHandler.cs` — `GetCharacterTraitsQueryHandler` now takes both repos and enriches each trait with `Name`.
- `Features/WorldEngine/Subsystems/Traits/Queries/HasTraitAsyncQuery.cs` + `HasTraitAsyncQueryHandler.cs` (new).
- `Features/WorldEngine/Subsystems/Traits/CharacterTrait.cs` (entity) — added `Name` property.
- `Features/WorldEngine/Subsystems/Traits/Tests/TraitSubsystemTests.cs` — NUnit tests for the four read methods and the required `HasTraitAsync` edge cases.

### Verification command
```
dotnet test AmiaReforged.PwEngine.csproj --filter "FullyQualifiedName~TraitSubsystemTests" --no-restore
```
Existing traits suites also run green:
```
dotnet test AmiaReforged.PwEngine.csproj --filter "FullyQualifiedName~TraitsCqrsTests|FullyQualifiedName~BackgroundTraitTests" --no-restore
```

### Observed result
- `TraitSubsystemTests`: **10/10 passed** (covers missing definitions, empty ownership, and inactive traits in `HasTraitAsync`, plus the dispatcher-flow and public-projection-shape cases).
- Existing traits suites (`TraitsCqrsTests`, `BackgroundTraitTests`): **57/57 passed** — no regressions.

See [backlog scope and completion rules](README.md).


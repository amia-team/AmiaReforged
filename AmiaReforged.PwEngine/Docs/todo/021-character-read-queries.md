# 021 — Dispatch character and context lookups

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: None.

## Current gap

Character/context lookups read the repository outside query dispatch.

## Change

Add/reuse a character lookup query and route subsystem reads and repository-backed runtime lookup through it. For synchronous context APIs, migrate callers to async or document a safe compatibility boundary; avoid blocking NWN-thread continuations.

## Starting points

- [Subsystems/Implementations/CharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs)
- [Subsystems/Characters/Runtime/RuntimeCharacterService.cs](../../Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacterService.cs)

## Acceptance checks

- [x] Existing and missing character cases preserve their documented results/exceptions.
- [x] Knowledge and industry context lookups use the same query path.
- [x] Migrated public lookup methods have no direct repository access.

## Completion evidence

**Query added.** New `GetCharacterQuery(CharacterId) : IQuery<ICharacter?>` and
`GetCharacterQueryHandler`, which reads the in-memory runtime repository via
`ICharacterRepository.GetById`. `CharacterId` has an implicit conversion to `Guid`, so the
repository call compiles directly. Both are registered with `[ServiceBinding]` for automatic
Anvil DI discovery.

**Routing.**

- `CharacterSubsystem.GetCharacterAsync` dispatches `GetCharacterQuery` (async, no blocking).
- `CharacterSubsystem.GetKnowledgeContext` / `GetIndustryContext` route through the same
  `GetCharacterQuery`; they remain synchronous public methods and block on the dispatched query
  via `.GetAwaiter().GetResult()`. This is a documented synchronous compatibility boundary: the
  in-memory handler completes without suspending, so blocking on an NWN-thread continuation does
  not await any real continuation.
- `RuntimeCharacterService.GetRuntimeCharacter` dispatches `GetCharacterQuery`
  (`CharacterId.From(creature.UUID)`) and casts to `RuntimeCharacter`, using the same boundary.

**No direct repository access.** The migrated public lookup methods (`GetKnowledgeContext`,
`GetIndustryContext`, `GetRuntimeCharacter`) read only through the query dispatcher.

**Changed files:**

- `Features/WorldEngine/Subsystems/Characters/Queries/GetCharacterQuery.cs` (new)
- `Features/WorldEngine/Subsystems/Characters/Queries/GetCharacterQueryHandler.cs` (new)
- `Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs` (injects `IQueryDispatcher`, routes the three lookups, documents the boundary)
- `Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacterService.cs` (`GetRuntimeCharacter` dispatches the query)
- `Features/WorldEngine/Subsystems/Characters/Queries/Tests/GetCharacterQueryHandlerTests.cs` (new, 2 tests)
- `Features/WorldEngine/Subsystems/Implementations/Tests/CharacterSubsystemLookupTests.cs` (new, 6 tests)

**Verification.**

Command:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-restore --filter "FullyQualifiedName~GetCharacterQueryHandlerTests|FullyQualifiedName~CharacterSubsystemLookupTests" --verbosity minimal
```

Result: passed — 8/8 (2 `GetCharacterQueryHandlerTests` for known/missing character; 6
`CharacterSubsystemLookupTests` asserting routing through `GetCharacterQuery`, preserved
known/missing/exception results, and zero direct `GetById` calls on the repository).

Full-suite confirmation:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-restore --filter 'FullyQualifiedName~WorldEngine' --verbosity minimal -m:1
```

Result: `Passed! - Failed: 0, Passed: 1779, Skipped: 0, Total: 1779`.

Full PwEngine suite:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-restore -m:1
```

Result: `Passed! - Failed: 0, Passed: 2062, Skipped: 0, Total: 2062`. Build: succeeded, 0 errors.

See [backlog scope and completion rules](README.md).


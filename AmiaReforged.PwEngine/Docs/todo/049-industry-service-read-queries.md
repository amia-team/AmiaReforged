# 049 — Dispatch independent membership and knowledge reads

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

Membership and knowledge read methods remain independently callable outside queries.

## Change

Inventory independent read callers, reuse existing membership/knowledge queries, and add only missing projections. Keep repository-backed reads used internally by handlers internal.

## Starting points

- [Subsystems/Industries/IndustryMembershipService.cs](../../Features/WorldEngine/Subsystems/Industries/IndustryMembershipService.cs)
- [Subsystems/Characters/Runtime/RuntimeCharacter.cs](../../Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacter.cs)

## Acceptance checks

- [ ] Each independent membership/knowledge read maps to a named query.
- [ ] Tests preserve missing membership, knowledge filtering, and learning-eligibility results.
- [ ] Record migrated callers and any intentionally handler-internal methods.

## Completion evidence

**Chosen behavior.** The three independent reads on the `ICharacter` runtime facade
(`RuntimeCharacter`) now route through named queries instead of calling the membership
service directly. Pure projections read the repositories inside their query handlers (the
established CQRS pattern); the one read that needs domain eligibility logic delegates to the
service and exposes only the boolean. Repository-backed reads that remain used internally by a
handler (`PlayerCodexPresenter`) are left on the service as-is — they are handler-internal, not
independent facade entry points.

**Queries added** (`Features/WorldEngine/Application/Industries/Queries/IndustryReadQueries.cs`):

- `GetKnowledgeDefinitionsQuery : IQuery<List<Knowledge>>` — handler `GetKnowledgeDefinitionsHandler`
  reads `ICharacterKnowledgeRepository.GetAllKnowledge` (maps `AllKnowledge`).
- `CanLearnKnowledgeQuery : IQuery<bool>` — handler `CanLearnKnowledgeHandler` delegates to
  `IIndustryMembershipService.CanLearnKnowledge(Guid, string)` and exposes only the boolean.

**Not added (dropped).** Two further projections were considered — a learned-knowledge-rows
query and a per-industry learned-knowledge query. They were removed before merge: they had no
production caller (the `PlayerCodexPresenter` NUI path keeps its direct service reads, see below),
and their simple names (`GetCharacterKnowledgeQuery`) collided with the Codex subsystem's
`GetCharacterKnowledgeQuery` (which returns `List<KnowledgeEntry>`), a latent ambiguous-reference
compile error once both namespaces are imported. `GetAllCharacterKnowledge` /
`GetCharacterKnowledgeForIndustry` remain reachable via the service the `PlayerCodexPresenter`
already uses, so no query layer is lost.

**Reused.** `GetCharacterIndustriesQuery` (existing) backs `AllIndustryMemberships`.

**Routing** (`Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacter.cs`):

- Constructor now takes `IQueryDispatcher`; `membershipService` was dropped because every
  membership/knowledge access is now dispatched (writes already went through `ICommandDispatcher`
  in tasks 043–048).
- `AllKnowledge()` dispatches `GetKnowledgeDefinitionsQuery`.
- `CanLearn()` dispatches `CanLearnKnowledgeQuery`.
- `AllIndustryMemberships()` dispatches `GetCharacterIndustriesQuery`.
- Reads block synchronously on the dispatched query via `.GetAwaiter().GetResult()` — the same
  documented synchronous compatibility boundary used elsewhere (task 021): the in-memory handlers
  complete without suspending, so no real NWN-thread continuation is blocked.
- `RuntimeCharacter.For` resolves `IQueryDispatcher` from Anvil DI.

**Handler-internal reads left in place.** `PlayerCodexPresenter` still reads `GetMemberships`,
`GetAllCharacterKnowledge` and `GetCharacterKnowledgeForIndustry` directly from the service; these
are NUI refresh paths, not independent entry points, so they stay as repository-backed service
reads.

**Changed files:**

- `Features/WorldEngine/Application/Industries/Queries/IndustryReadQueries.cs` (new — 2 queries + handlers)
- `Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacter.cs` (dispatch reads, drop `membershipService`)
- `Features/WorldEngine/SharedKernel/Tests/RuntimeCharacterTests.cs` (`IQueryDispatcher` ctor arg + `MockQueryDispatcher` wiring reads to the real service)
- `Features/WorldEngine/Subsystems/Industries/Tests/IndustryReadQueriesHandlerTests.cs` (new, 5 tests)

**Verification.**

Command:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-restore --filter "FullyQualifiedName~IndustryReadQueriesHandlerTests|FullyQualifiedName~RuntimeCharacterTests" --verbosity minimal
```

Result: passed — 16/16 (5 `IndustryReadQueriesHandlerTests`: definitions for known/missing
character, and learning eligibility true / missing-membership-false / unknown-knowledge-false; 11
`RuntimeCharacterTests` exercising the dispatched `AllKnowledge` / `CanLearn` / `AllIndustryMemberships`
through the real service).

Full WorldEngine suite:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-restore --filter 'FullyQualifiedName~WorldEngine' --verbosity minimal -m:1
```

Result: `Passed! - Failed: 0, Passed: 1864, Skipped: 0, Total: 1864`.

Full PwEngine suite:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-restore -m:1
```

Result: `Passed! - Failed: 0, Passed: 2147, Skipped: 0, Total: 2147`. Build: succeeded, 0 errors.

See [backlog scope and completion rules](README.md).

See [backlog scope and completion rules](README.md).


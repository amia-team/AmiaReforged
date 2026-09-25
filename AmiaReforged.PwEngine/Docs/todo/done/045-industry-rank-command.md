# 045 — Dispatch industry rank advancement

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

Runtime rank-up calls the membership service directly.

## Change

Add a rank-up command/handler and migrate independent callers while retaining existing requirements and result semantics.

## Starting points

- [Subsystems/Industries/IndustryMembershipService.cs](../../Features/WorldEngine/Subsystems/Industries/IndustryMembershipService.cs)
- [Subsystems/Characters/Runtime/RuntimeCharacter.cs](../../Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacter.cs)

## Acceptance checks

- [ ] An eligible member advances one rank and receives the existing event once.
- [ ] Ineligible/missing membership fails without a mutation.
- [ ] Runtime rank-up traverses command dispatch.

## Completion evidence

**Chosen behavior.** Added a dispatch boundary for industry rank advancement. `RankUpCommand` carries the target `CharacterId` + `IndustryTag`; `RankUpHandler` delegates to the existing `IIndustryMembershipService.RankUp(Guid, string)`, which performs the prerequisite checks (tier ceiling, knowledge points, maxed-out), increments the level, persists the membership, and publishes `ProficiencyGainedEvent` exactly once. The handler maps the service's `RankUpResult` onto `CommandResult.Data` (`result`, `success`) following the same contract as `LearnKnowledgeCommand`. Independent callers now go through the dispatcher instead of the service directly.

**Changed files.**
- `Features/WorldEngine/Application/Industries/Commands/RankUpCommand.cs` (new) — command + handler.
- `Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacter.cs` — `RankUp` now dispatches `RankUpCommand` and maps the result back; falls back to `IndustryNotFound` if the result is absent.
- `Features/WorldEngine/SharedKernel/Tests/RuntimeCharacterTests.cs` — `MockDispatcher` now stubs `RankUpCommand` against the real service so the dispatch boundary is exercised.

**Post-review simplification.** A maintainability review confirmed the change was correct and consistent with the task-044 pattern but flagged the handler's `try/catch` as dead weight: `CommandDispatcher.DispatchAsync` already wraps every handler invocation in a `try/catch` returning a generic `CommandResult.Fail`, so the handler's catch was redundant and its message never surfaced. Removed it (no behavioral change — the dispatcher owns exception handling), leaving the handler as pure delegation + result mapping.

**Acceptance checks.**
- [x] An eligible member advances one rank and receives the existing event once. — `RankUp_ShouldPublish_ProficiencyGainedEvent` (service) and `Should_Rank_Up_In_Industry` (via dispatch) both pass; event published once.
- [x] Ineligible/missing membership fails without a mutation. — service returns `InsufficientProficiencyLevel`/`InsufficientKnowledge`/`IndustryNotFound` before any `membershipRepository.Update`; handler returns `CommandResult.Fail`.
- [x] Runtime rank-up traverses command dispatch. — `RuntimeCharacter.RankUp` sends `RankUpCommand` through `ICommandDispatcher`.

**Verification.**
```sh
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-restore
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-build --no-restore --filter 'FullyQualifiedName~RankUp|FullyQualifiedName~Industry|FullyQualifiedName~RuntimeCharacter' --verbosity minimal -m:1
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-build --no-restore --filter 'FullyQualifiedName~WorldEngine' --verbosity minimal -m:1
```
Observed result: build succeeded (0 errors); the filtered run passed 98/98; the full `WorldEngine` filter passed **1,856/1,856, 0 failed/skipped**, matching the audit baseline.

See [backlog scope and completion rules](README.md).


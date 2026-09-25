# 044 — Dispatch knowledge learning

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

Knowledge learning persists through direct service calls; `LearnRecipeCommand` only validates recipe availability.

## Change

Add a knowledge-learning command wrapping the existing domain/service behavior and migrate independent runtime callers. Keep recipe validation separate and preserve prerequisite checks.

## Starting points

- [Subsystems/Industries/IndustryMembershipService.cs](../../Features/WorldEngine/Subsystems/Industries/IndustryMembershipService.cs)
- [Subsystems/Characters/Runtime/RuntimeCharacter.cs](../../Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacter.cs)

## Acceptance checks

- [x] Eligible knowledge learning persists and publishes the existing domain event once.
- [x] Missing prerequisites and already-known knowledge do not create another record.
- [x] Independent learning entry points dispatch; returned learning outcomes remain usable by their callers.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

## Completion evidence

**Behavior decided:** knowledge learning now goes through a command/dispatch boundary that wraps
the existing domain logic. The handler performs no checks of its own — it delegates entirely to
`IIndustryMembershipService.LearnKnowledge(characterId, knowledgeTag)`, which keeps all prerequisite
checks (rank, points, already-known), point deduction, persistence of `CharacterKnowledge`, and the
single `RecipeLearnedEvent` publish. The handler only maps the returned `LearningResult` onto the
`CommandResult` contract (stored under `Data["result"]` / `Data["success"]`) so runtime callers keep
using the outcome. Recipe learning validation stays in `LearnRecipeCommand` (separate, untouched).

**Changed files:**
- `Features/WorldEngine/Application/Industries/Commands/LearnKnowledgeCommand.cs` (new) — `LearnKnowledgeCommand` record + `LearnKnowledgeHandler`.
- `Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacter.cs` — `Learn` now dispatches `LearnKnowledgeCommand` via the injected `ICommandDispatcher` instead of calling the service directly; maps the outcome back to `LearningResult`.
- `Features/WorldEngine/SharedKernel/Tests/RuntimeCharacterTests.cs` — `MockDispatcher` routes `LearnKnowledgeCommand` through the real membership service.
- `Features/WorldEngine/Subsystems/Industries/Tests/LearnKnowledgeCommandTests.cs` (new) — handler tests: success persists + publishes once, already-known does not double-record, unknown knowledge fails, outcome is readable by callers.

**Verification:**
```bash
dotnet build AmiaReforged.PwEngine.csproj -c Debug
dotnet test AmiaReforged.PwEngine.csproj -c Debug --filter "FullyQualifiedName~LearnKnowledgeCommandTests|FullyQualifiedName~RuntimeCharacterTests"
```
Both succeed. Broader surface also green: `--filter "FullyQualifiedName~Industries|FullyQualifiedName~Characters"` → 235 passed, 0 failed.

**Observed result:** build clean; new handler tests and existing RuntimeCharacter learning/rank-up tests pass; 235 Industries+Characters tests pass with no regressions.

See [backlog scope and completion rules](README.md).


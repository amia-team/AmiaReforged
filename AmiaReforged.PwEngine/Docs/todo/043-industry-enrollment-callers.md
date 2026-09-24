# 043 — Route runtime industry enrollment through the existing command

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

`RuntimeCharacter` still invokes direct membership insertion, despite the new enrollment command.

## Change

Migrate independent runtime enrollment entry points to `EnrollInIndustryCommand`. Compare existing service initialization behavior before replacing it; preserve required defaults and events without double insertion.

## Starting points

- [Subsystems/Characters/Runtime/RuntimeCharacter.cs](../../Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacter.cs)
- [Subsystems/Industries/IndustryMembershipService.cs](../../Features/WorldEngine/Subsystems/Industries/IndustryMembershipService.cs)
- [Application/Industries/Commands/EnrollInIndustryCommand.cs](../../Features/WorldEngine/Application/Industries/Commands/EnrollInIndustryCommand.cs)

## Acceptance checks

- [ ] Runtime enrollment traverses the dispatcher and creates one membership.
- [ ] Duplicate/unknown character/unknown industry behavior is explicit and tested.
- [ ] Independent callers no longer use direct `AddMembership` to bypass dispatch.

## Completion evidence

### Decision
- Enrollment starts at `ProficiencyLevel.Novice`, not `Layman`. `Layman` is a special "not a professional of any kind in this industry" state, so runtime `JoinIndustry` must never produce it. The command handler was corrected from `Layman` to `Novice` to match the pre-existing runtime/default behavior.
- All runtime enrollment now flows through `EnrollInIndustryCommand`, so existence checks, duplicate detection, the `MemberJoinedIndustryEvent`, and single-membership creation live in one place (the handler via the dispatcher).

### Changed files
- `Features/WorldEngine/Application/Industries/Commands/EnrollInIndustryCommand.cs` — starting level `Layman` -> `Novice`.
- `Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacter.cs` — `JoinIndustry` now dispatches `EnrollInIndustryCommand` (constructor + `For()` factory inject `ICommandDispatcher`); removed the direct `AddMembership`.
- `Features/Chat/Commands/KnowledgeDevCommand.cs` — `JoinIndustry` now dispatches the command (method made `async Task`); removed the direct repo `Add`/`SaveChanges`.
- `Features/WorldEngine/SharedKernel/Tests/RuntimeCharacterTests.cs` — constructor calls inject a dispatcher mock that persists enrollment via the real membership service, so existing setup-based tests keep working.

### Scope note
- `Features/WorldEngine/SharedKernel/Tests/Helpers/TestCharacter.cs` (a test-only fixture, not a production runtime caller) still uses `membershipService.AddMembership`. It was left as-is: routing a test double through the dispatcher would require injecting a real dispatcher into every test-construction site. It does not bypass dispatch in production.

### Verification
Command:
```
dotnet build AmiaReforged.PwEngine.csproj
dotnet test AmiaReforged.PwEngine.csproj
```
Result: build succeeds (no new errors/warnings from this change); full suite passes — `Failed: 0, Passed: 2134, Skipped: 0`.
Runtime-enrollment-specific: `--filter "FullyQualifiedName~RuntimeCharacter"` passes 15/15.

### Acceptance checks
- [x] Runtime enrollment traverses the dispatcher and creates one membership (`RuntimeCharacter.JoinIndustry` -> `DispatchAsync<EnrollInIndustryCommand>`).
- [x] Duplicate/unknown character/unknown industry behavior is explicit and tested (handled in `EnrollInIndustryHandler`; the command was already covered by dispatcher behavior tests; the runtime path now inherits it).
- [x] Independent production callers no longer use direct `AddMembership`/repo `Add` to bypass dispatch (`RuntimeCharacter`, `KnowledgeDevCommand`).

See [backlog scope and completion rules](README.md).


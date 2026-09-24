# 048 — Make standalone proficiency awards dispatchable

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

The proficiency service mutates a supplied membership but has no standalone persistence/dispatch boundary.

## Change

Added an award command/handler around the existing `IProficiencyProgressionService` and migrated the two independent callers (`CraftItemHandler`, `NwnStageRewardGranter`). The domain logic in the service is unchanged.

### Decision

- **Kept `IProficiencyProgressionService.AwardProficiencyXp`** as the domain service; the new handler delegates to it (allowed — it is no longer an independent entry point once both callers are migrated).
- **`AwardProficiencyCommand`** (record) + **`AwardProficiencyHandler`** (`[ServiceBinding(typeof(ICommandHandler<AwardProficiencyCommand>))]`) injected with `IIndustryMembershipRepository`, `IProficiencyProgressionService`, `ICommandDispatcher`, and `IEventBus`. Fails (`CommandResult.Fail`) on `Points <= 0`; loads the membership by character + industry tag and fails with an explicit "not a member" result when absent; delegates to the calculator; persists the mutated membership on any successful award; and publishes the new `ProficiencyXpAwardedEvent` domain event. Returns a flat `resultData` dict (`success`, `proficiencyXpLevel`, `proficiencyXpRemaining`, `proficiencyXpRequired`, `proficiencyLevelsGained`, `proficiencyAtTierCeiling`, `message`). On success the dispatcher also publishes the generic `CommandExecutedEvent<AwardProficiencyCommand>`.
- **`CraftItemHandler`** now injects `ICommandDispatcher` only (dropped `_proficiencyService` and `_membershipRepository` fields), routes the proficiency award through `await _commandDispatcher.DispatchAsync(new AwardProficiencyCommand{...})`, and reads the returned data back into the same `resultData` keys. The redundant membership null-check in the craft handler is removed (the command owns membership loading). Progression-point awards still go through the existing `AwardProgressionCommand`. Awards exactly once.
- **`NwnStageRewardGranter`** drops its `Lazy<IIndustryMembershipService>` and `Lazy<IProficiencyProgressionService>` and instead injects `IIndustryMembershipRepository` + `ICommandDispatcher`. `GrantProficiencyXp` is now async and dispatches one `AwardProficiencyCommand` per `ProficiencyReward`, letting the handler load the membership, award, persist, and publish. The `Lazy` wrappers existed to break a DI cycle; `IIndustryMembershipRepository`/`ICommandDispatcher` introduce none.
- **New event:** `ProficiencyXpAwardedEvent(MemberId, IndustryTag, NewLevel, XpRemaining, XpRequired, LevelsGained, IsAtTierCeiling, OccurredAt)`.
- **New (test):** `Features/WorldEngine/Subsystems/Industries/Tests/AwardProficiencyCommandTests.cs` — in-memory `IIndustryMembershipRepository`, real `ProficiencyProgressionService`, `InMemoryEventBus`, and `CommandDispatcher`; covers XP rollover, tier-ceiling hard gate, persistence of a successful award, missing membership, invalid XP, generic `CommandExecutedEvent` + `ProficiencyXpAwardedEvent` on direct dispatch, and no domain event when blocked.
- Updated `AwardProgressionCommandTests.Craft_WhenSuccessful_ThenAwardsProgressionPointsExactlyOnce` for the new `CraftItemHandler` constructor (dropped `proficiencyService`/`membershipRepository`).

### Independent callers audited
`CraftItemHandler` (`CraftItemCommand.cs`) and `NwnStageRewardGranter` (`NwnStageRewardGranter.cs`) were the only direct callers of `AwardProficiencyXp`. Grep for `AwardProficiencyXp` confirms these two plus the service definition and existing unit tests. No other callers to migrate.

### Verification

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --filter 'FullyQualifiedName~AwardProficiency|FullyQualifiedName~AwardProgression' --no-restore -v q -m:1
```

Full suite: `dotnet test ... --filter 'FullyQualifiedName~WorldEngine'`.

### Notes

- Persisting on any successful award (not only level-ups) makes the command self-contained for independent callers; the craft path previously persisted only on level gain, so accumulated XP below a level threshold is now durable too.
- The `ProficiencyXpAwardedEvent` is published only on success (never when blocked at a tier ceiling or max level).

## Starting points

- [Subsystems/Industries/ProficiencyProgressionService.cs](../../Features/WorldEngine/Subsystems/Industries/ProficiencyProgressionService.cs)
- [Application/Industries/Commands/CraftItemCommand.cs](../../Features/WorldEngine/Application/Industries/Commands/CraftItemCommand.cs)
- [Subsystems/Codex/Application/NwnStageRewardGranter.cs](../../Features/WorldEngine/Subsystems/Codex/Application/NwnStageRewardGranter.cs)

## Acceptance checks

- [x] Tests verify XP rollover, tier ceilings, and persistence of a successful award.
- [x] Missing membership and invalid XP have explicit results.
- [x] Independent callers use the command; existing crafting and quest rewards still award once.

## Completion evidence

### Decisions
- Kept `IProficiencyProgressionService.AwardProficiencyXp` as the domain service; the new handler delegates to it. Service file unchanged.
- Persist the mutated membership on any successful award (self-contained command), not only on level gains.
- Publish `ProficiencyXpAwardedEvent` only on success (never when blocked at tier ceiling / max level).
- `NwnStageRewardGranter` now injects `IIndustryMembershipRepository` + `ICommandDispatcher` (no `Lazy` wrappers); the DI cycle is broken by routing through the command rather than holding industry services directly.

### Changed / added files
- `Subsystems/Industries/Events/ProficiencyXpAwardedEvent.cs` (new domain event)
- `Application/Industries/Commands/AwardProficiencyCommand.cs` (new record + handler)
- `Application/Industries/Commands/CraftItemCommand.cs` (routes proficiency award through the command)
- `Subsystems/Codex/Application/NwnStageRewardGranter.cs` (dispatches the command per proficiency)
- `Subsystems/Industries/Tests/AwardProficiencyCommandTests.cs` (new tests)
- `Subsystems/Industries/KnowledgeSubsystem/Tests/AwardProgressionCommandTests.cs` (updated `CraftItemHandler` constructor)

### Verification command
```sh
dotnet test /home/amia/projects/AmiaReforged/AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-restore -v q -m:1
```

### Observed result
- Targeted run (`AwardProficiencyCommandTests`): `Passed! - Failed: 0, Passed: 10, Skipped: 0, Total: 10`.
- Related run (`AwardProgressionCommandTests`, `CraftItemHandler`, `ProficiencyProgressionServiceTests`, `ProficiencyRankUpTests`): `Passed! - Failed: 0, Passed: 34, Skipped: 0, Total: 34`.
- Full `AmiaReforged.PwEngine` suite: `Passed! - Failed: 0, Passed: 2105, Skipped: 0, Total: 2105`.

See [backlog scope and completion rules](README.md).


# 050 — Separate progression reads from initialization

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

Progression read methods use `GetOrCreate`, so blindly wrapping them as queries would preserve hidden writes.

## Change

Provide side-effect-free progression/cap/cost queries for independent callers. Move required initialization into the existing registration/award write path or a narrow initialization command.

## Starting points

- [Subsystems/Industries/KnowledgeSubsystem/KnowledgeProgressionService.cs](../../Features/WorldEngine/Subsystems/Industries/KnowledgeSubsystem/KnowledgeProgressionService.cs)
- [Subsystems/Industries/KnowledgeSubsystem/IKnowledgeProgressionRepository.cs](../../Features/WorldEngine/Subsystems/Industries/KnowledgeSubsystem/IKnowledgeProgressionRepository.cs)

## Acceptance checks

- [ ] Querying a missing character's progression does not insert a row.
- [ ] Existing progression, cap, and next-point cost results are preserved.
- [ ] Independent callers dispatch queries; initialization is explicit and tested.

## Completion evidence

## Decision

Reads are side-effect-free queries; initialization stays on the explicit write paths.
- `GetProgression`, `GetEffectiveSoftCap`, `GetEffectiveHardCap`, `GetProgressionCostForNextPoint` now read via `repository.GetByCharacterId` (pure lookup) and never insert. A missing row yields `null` (for `GetProgression`) or configured defaults (caps/cost treat a missing row as zero economy KP earned).
- Write paths (`AwardProgressionPoints`, `GrantLevelUpKnowledgePoint`) still initialize via `GetOrCreate` — that is the "required initialization in the award write path".
- `GetProgression` is now `KnowledgeProgression?`; nullability propagated to `ICharacterKnowledgeContext`, `RuntimeCharacter`, and `GlyphWorldEngineApi` (returns zeros on `null` without creating a row).

## Changed files

- `Features/WorldEngine/Subsystems/Industries/KnowledgeSubsystem/IKnowledgeProgressionService.cs` — read signatures documented as side-effect-free; `GetProgression` returns nullable.
- `Features/WorldEngine/Subsystems/Industries/KnowledgeSubsystem/KnowledgeProgressionService.cs` — reads route through `GetByCharacterId`; `GetProgressionCostForNextPoint` reuses the already-fetched progression (1 repo read).
- `Features/Glyph/Runtime/GlyphWorldEngineApi.cs` — handles `null` from the read.
- `Features/WorldEngine/Subsystems/Characters/ICharacterKnowledgeContext.cs` + `RuntimeCharacter.cs` — `GetProgression()` nullable.
- `Features/WorldEngine/SharedKernel/Tests/Helpers/TestCharacter.cs`, `.../Characters/Queries/Tests/GetCharacterQueryHandlerTests.cs`, `.../Implementations/Tests/CharacterSubsystemLookupTests.cs` — test-double `GetProgression()` overrides made nullable.
- `Features/WorldEngine/Subsystems/Industries/KnowledgeSubsystem/Tests/KnowledgeProgressionQueryTests.cs` (new) — asserts reads do not insert and return correct defaults/stored values.

## Verification

```
dotnet build --no-restore            # Build succeeded, 0 warnings (no CS8766/CS8765)
dotnet test --no-build              # Failed: 0, Passed: 2149, Skipped: 0
```

Observed: new `KnowledgeProgressionQueryTests` confirms `GetProgression` returns `null` and `GetByCharacterId` stays null after the read (no insert), and that caps/cost return configured defaults for a missing character while existing rows are returned unchanged.

## Review

Thermo-nuclear review (subagent `reviewer`): **APPROVE** with two minor P2 follow-ups, both applied above — redundant-read simplification in `GetProgressionCostForNextPoint` and test-double nullability hygiene.

See [backlog scope and completion rules](README.md).


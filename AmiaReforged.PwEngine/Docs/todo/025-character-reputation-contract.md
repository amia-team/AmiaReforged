# 025 — Define organization reputation storage and semantics

Status: **Done**
Type: **Decision**
Audit area: **F-7 Characters**
Depends on: None.

## Current gap

`IReputationRepository.GetReputation` always returned a fabricated default `Reputation` (Level 0) for every lookup, and `CharacterSubsystem.AdjustReputationAsync` returned a fail result. Character reputation adjustment was therefore unsupported, and the read path silently reported a "default" success.

## Change

Choose the authoritative organization-reputation store and missing-record/adjustment semantics. Distinguish this API from Codex faction reputation; only share storage if identities and semantics actually match.

## Starting points

- `Features/WorldEngine/Subsystems/Characters/IReputationRepository.cs` — **deleted** (see Completion evidence).
- `Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs` — reputation methods removed (see Completion evidence).
- Authoritative store reviewed: `Features/WorldEngine/Subsystems/Codex/Domain/Entities/FactionReputation.cs` and `Features/WorldEngine/Subsystems/Codex/Application/Reputation/AdjustReputationCommand.cs`.

## Acceptance checks

- [x] Record the target identity mapping, retention, default, and allowed adjustment behavior.
- [x] Identify the concrete repository changes and any schema migration required.
- [x] Resolve whether this public feature is implemented or explicitly retired; do not silently retain a fake success/default.

## Decision

**The Character-subsystem organization-reputation feature is explicitly retired.**

**Identity mapping (why the two systems do not share storage).** The retired API was keyed by `OrganizationId` (a GUID value object, `Features/WorldEngine/SharedKernel/OrganizationId.cs`). The Codex faction-reputation system is keyed by `FactionId` (a string resref, `Features/WorldEngine/Subsystems/Codex/Domain/ValueObjects/FactionId.cs`). These are distinct identities with no 1:1 mapping — an `OrganizationId` GUID is not a `FactionId` string — so the two must **not** share storage. The only semantic overlap (a per-character, per-group standing) is not sufficient to merge stores whose identities and lifecycles differ.

**Authoritative store.** The Codex `PlayerCodex` aggregate (persisted via `IPlayerCodexRepository` `LoadAsync`/`SaveAsync`, entity `FactionReputation` with `ReputationScore`, change history, and standing thresholds) is the authoritative faction-reputation feature. It is the store actually exercised by the game: the dialogue `ChangeReputation` action and the `ReputationAbove`/`ReputationBelow` condition evaluators route through `CodexQueryService.GetReputationAsync(cid, FactionId)` and `AdjustReputationCommand`.

**Missing-record / default semantics (retired).** The old store returned a fabricated default `Reputation { Level = 0 }` for any missing record — a silent fake success. Retiring removes this; there is no longer an organization-reputation read path that can lie.

**Adjustment semantics (retired).** The old `AdjustReputationAsync` returned `CommandResult.Fail` and had no mutation support. Retiring removes the dead method. Real mutation lives in the Codex `AdjustReputationCommand` handler.

**No callers.** There were zero internal callers of `CharacterSubsystem.GetReputationAsync`/`AdjustReputationAsync` and of the `ReputationRepository` service binding. The feature was public surface with no backing store, no persistence, and no consumers.

**Retention.** Nothing is retained. The stub repository, the bare `Reputation` model, and both `ICharacterSubsystem` methods were removed so no fake success/default survives.

## Completion evidence

**Chosen behavior:** retire the organization-reputation API (see Decision above). No schema migration is required — the store was an unpopulated in-memory `Dictionary` with no persistence or schema.

**Changed files:**
- `Features/WorldEngine/Subsystems/ICharacterSubsystem.cs` — removed `GetReputationAsync` and `AdjustReputationAsync`.
- `Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs` — removed both methods, the `_reputationRepository` field, and the constructor parameter; dropped unused usings.
- `Features/WorldEngine/Subsystems/Characters/IReputationRepository.cs` — deleted (stub repository + service binding).
- `Features/WorldEngine/Subsystems/Characters/CharacterData/Reputation.cs` — deleted (bare `Reputation` model, now unused).
- `Features/WorldEngine/Subsystems/Implementations/Tests/CharacterSubsystemLookupTests.cs` — removed `NullReputationRepository` and its constructor argument (kept the `CharacterData` using, still required by `SkillData`/`KnowledgeProgression`/`CharacterStatistics`).

**Verification command:**
```
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj -c Debug
```

**Observed result:** build succeeded, `0 Error(s)`. Confirmed (via `grep`) there are no remaining references to `IReputationRepository`, `ReputationRepository`, the deleted `Reputation` type, or the two removed methods; the remaining `Reputation`/`ReputationType` references are unrelated NWN-legacy `CreatureTypeFilter.Reputation` calls.

See [backlog scope and completion rules](README.md).


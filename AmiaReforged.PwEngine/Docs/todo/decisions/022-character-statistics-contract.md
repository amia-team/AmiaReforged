# 022 — Resolve unsupported character statistics fields

Status: **Done**
Type: **Decision and contract**
Audit area: **F-7 Characters**
Depends on: None.

## Current gap

Statistics report rank-ups as quests, industries joined as crafted items, and the current time as last seen; only play time is writable.

## Change

Adopted the smallest truthful statistics contract: only total **play time** has a backing data source. The fields `QuestsCompleted`, `ItemsCrafted`, and `LastSeen` had no truthful backing data (rank-ups were mislabeled as quests, industries joined as crafted items, and "now" as last seen), so they were **removed** from the projection rather than misrepresented.

### Decision

- `CharacterStats` is now a single-field record: `CharacterStats(int PlayTime)` (see `ICharacterSubsystem.cs`).
- Read returns `null` when no statistics record exists; otherwise projects only `PlayTime`.
- Write supports only `PlayTime`; all other stored counters (`TimesRankedUp`, `IndustriesJoined`, `TimesDied`, `KnowledgePoints`) are left untouched because they have no supported projection.
- No other consumers of `CharacterStats` exist outside the subsystem, so reshaping the record is safe.

## Completion evidence

- Changed files:
  - `Features/WorldEngine/Subsystems/ICharacterSubsystem.cs` — `CharacterStats` reduced to `PlayTime` + updated contract docs.
  - `Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs` — removed direct `_statRepository` reads/writes; forwards to query/command handlers.
- Verification command:
  ```sh
  dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-build --no-restore --filter 'FullyQualifiedName~WorldEngine' --verbosity minimal -m:1
  ```
- Observed result: build succeeded; **1800 passed, 0 failed** (baseline 1685; +15 new statistics tests). Acceptance checks met: every returned field has a documented real source (`PlayTime`) or explicit `null` representation; reads no longer mislabel rank-ups/industries/time; read/write contract and consumer compatibility captured in `GetCharacterStatsQueryHandlerTests`, `UpdateCharacterStatsCommandHandlerTests`, and `CharacterSubsystemLookupTests`.

## Starting points

- [Subsystems/Implementations/CharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs)
- [Subsystems/ICharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/ICharacterSubsystem.cs)

## Acceptance checks

- [ ] Every returned field has a documented real source or explicit unavailable representation.
- [ ] Reads no longer label rank-ups as quests or industries joined as crafted items.
- [ ] The read/write contract and consumer compatibility are captured in focused tests.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).


# 023 — Dispatch character statistics reads

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: [022 — Resolve unsupported character statistics fields](022-character-statistics-contract.md).

## Current gap

`GetCharacterStatsAsync` reads statistics directly.

## Change

Added a statistics query/handler implementing task 022's contract and made the subsystem a dispatch wrapper.

### New files

- `Features/WorldEngine/Subsystems/Characters/Queries/GetCharacterStatsQuery.cs` — `GetCharacterStatsQuery(CharacterId) : IQuery<CharacterStats?>`.
- `Features/WorldEngine/Subsystems/Characters/Queries/GetCharacterStatsQueryHandler.cs` — projects only `PlayTime`; returns `null` when the record is missing.

`CharacterSubsystem.GetCharacterStatsAsync` now calls `_queries.DispatchAsync<GetCharacterStatsQuery, CharacterStats?>` and no longer touches the statistics repository directly.

## Starting points

- [Subsystems/Implementations/CharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs)

## Acceptance checks

- [ ] Tests cover a known character and a missing statistics record.
- [ ] The query performs no writes and returns the agreed truthful projection.
- [ ] The subsystem forwards cancellation and no longer reads the statistics repository directly.

## Completion evidence

- Changed files:
  - New query + handler (see above).
  - `Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs` — forwards cancellation via `ct` and no longer reads the statistics repository directly.
- New test double: `Features/WorldEngine/Subsystems/Characters/Tests/InMemoryCharacterStatRepository.cs`.
- Tests: `Features/WorldEngine/Subsystems/Characters/Queries/Tests/GetCharacterStatsQueryHandlerTests.cs` — covers a known character (play-time projection), a missing character (`null`), and confirms unsupported stored counters are not projected.
- Verification command:
  ```sh
  dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-build --no-restore --filter 'FullyQualifiedName~WorldEngine' --verbosity minimal -m:1
  ```
- Observed result: build succeeded; **1800 passed, 0 failed**. Acceptance checks met: tests cover a known character and a missing record; the query performs no writes and returns the truthful projection; the subsystem forwards cancellation and no longer reads the statistics repository directly.

See [backlog scope and completion rules](README.md).


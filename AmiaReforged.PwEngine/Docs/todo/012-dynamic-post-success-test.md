# 012 — Verify successful dynamic-quest posting

Status: **Open**  
Type: **Verification**  
Audit area: **F-6 verification**  
Depends on: [010 — Publish dynamic-quest domain events on the bus](010-dynamic-quest-domain-events.md).

## Current gap

Existing command-level coverage verifies failure for a missing template but does not prove the successful post path through the real command dispatcher.

## Test decision

Add one focused successful-post test to the existing Codex command behavior tests.

Prefer:

`Features/WorldEngine/SharedKernel/Tests/Codex/Application/CodexPlayerStateBehavior.cs`

Do not create mocks for repository behavior.

Use:

- `InMemoryDynamicQuestRepository`
- real `DynamicQuestService`
- real `PostDynamicQuestHandler`
- real `CommandDispatcher`
- `InMemoryEventBus` so published domain and generic command events are inspectable

No `CodexEventProcessor` forwarding is needed to verify posting because `QuestPostedEvent` does not mutate `PlayerCodex`.

## Arrange

Create a minimal valid active `DynamicQuestTemplate`:

- non-empty `Title`
- non-empty `Description`
- a real `TemplateId`
- `CreatedAt` set
- rely on existing defaults for claim mode/rewards/timers unless the test needs otherwise

Save it with:

```csharp
await dynRepo.SaveTemplateAsync(template);
```

Use the same shared `InMemoryEventBus` instance in both:

- `DynamicQuestService`
- `CommandDispatcher`

This is required so the test sees both the domain event and `CommandExecutedEvent<PostDynamicQuestCommand>`.

## Act

Dispatch:

```csharp
PostDynamicQuestCommand
```

through the real `CommandDispatcher`.

Do not call `PostDynamicQuestHandler.HandleAsync` directly.

## Required assertions

Assert all of the following:

1. `CommandResult.Success` is true.
2. The result contains the created posting ID (`PostDynamicQuestHandler` returns it as `postingId`).
3. `InMemoryDynamicQuestRepository.GetPostingAsync(...)` returns that posting.
4. The persisted posting:
   - references the input template ID;
   - copies the template title/description.
5. `InMemoryEventBus.PublishedEvents` contains exactly one matching `QuestPostedEvent`.
6. That event carries:
   - `CharacterId == PostedBy`
   - the created posting ID
   - the template ID
   - the template title
7. The bus contains exactly one successful `CommandExecutedEvent<PostDynamicQuestCommand>`.
8. No `PlayerCodex` entry is required or created merely because a posting was published.

Do not assert exact timestamps; assert they are populated/sensible if needed.

## Invariants

- Posting persistence happens before `QuestPostedEvent` publication.
- `QuestPostedEvent` is bus-observable.
- `QuestPostedEvent` is **not** forwarded into `CodexEventProcessor`.
- The generic command event is produced by `CommandDispatcher`, not manually by the handler/service.

## Non-goals

Do not test in this task:

- claim behavior;
- sharing;
- expiry;
- inactive/invalid templates;
- EF persistence;
- NWN integration.

Those have separate coverage/tasks.

## Acceptance checks

- [ ] A valid active template yields a persisted posting.
- [ ] The command returns success through the real dispatcher.
- [ ] Exactly one matching `QuestPostedEvent` is observable.
- [ ] Exactly one successful generic command-executed event is observable.
- [ ] The test does not rely on mocked handler/repository calls.

## Verification

Run the focused test first, then:

```bash
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --filter "FullyQualifiedName~Codex" --no-build --verbosity minimal
```

## Completion evidence

Record the test name and exact test command/result.

See [backlog scope and completion rules](README.md).

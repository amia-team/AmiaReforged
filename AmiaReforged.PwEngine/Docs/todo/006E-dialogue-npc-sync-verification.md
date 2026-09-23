# 006E — Verify dialogue NPC synchronization end-to-end

Status: **Deferred**
Type: **Verification**
Audit area: **F-1**
Depends on: **006B, 006C, 006D**

## Goal

Prove that dialogue-tree CRUD causes the correct runtime NPC synchronization through the event path, and that rejected commands do not produce synchronization side effects.

This task must verify both:

1. application/event orchestration without requiring NWN runtime objects;
2. real or NWN-aware binding behavior for the concrete `DialogueNpcHook`.

Do not treat one layer as proof of the other.

## Preconditions

Before starting:

- 006A provides `IDialogueNpcSynchronizer`;
- 006B provides the successful-command event subscriber;
- 006C removes direct controller NPC calls;
- 006D defines shared `SpeakerTag` ownership semantics.

If 006D selects behavior that current `DialogueNpcHook` does not implement, complete the authorized follow-up implementation before marking 006E done.

## Verification layer 1 — Subscriber behavior

Use a fake/mock synchronizer.

The test must not depend on real:

```text
NwCreature
NwObject
NwModule
NwTask
```

### Test A — create with tag

Given a successful:

```csharp
CreateDialogueTreeCommand
```

with:

```text
DialogueTreeId = tree_a
SpeakerTag = npc_bob
```

when the successful command event reaches the subscriber,

verify:

```text
RegisterAsync("npc_bob", "tree_a")
```

is called exactly once.

### Test B — create without tag

Given:

```text
SpeakerTag = null
```

or whitespace,

verify no register call occurs.

### Test C — update changes tag

Given:

```text
DialogueTreeId = tree_a
new SpeakerTag = npc_alice
```

verify:

```text
UpdateAsync("tree_a", "npc_alice")
```

is called exactly once.

The event subscriber does not need the old tag.

### Test D — update clears tag

Given:

```text
DialogueTreeId = tree_a
new SpeakerTag = null
```

verify:

```text
UpdateAsync("tree_a", null)
```

is called.

Do not incorrectly skip the operation just because the new tag is empty; clearing a tag must remove the previous binding.

### Test E — delete

Given:

```text
DialogueTreeId = tree_a
```

verify:

```text
UnregisterAsync("tree_a")
```

is called exactly once.

## Verification layer 2 — rejection behavior

The acceptance requirement says rejected changes do not trigger synchronization.

The strongest proof is through `CommandDispatcher`.

### Required test shape

Arrange a dialogue CRUD command handler that returns:

```csharp
CommandResult.Fail(...)
```

Then dispatch through:

```csharp
CommandDispatcher.DispatchAsync(...)
```

Verify:

- no `CommandExecutedEvent<TCommand>` is published;
- the synchronization subscriber is not invoked.

Do not merely call the subscriber with an artificial:

```text
CommandExecutedEvent(Result.Success = false)
```

and claim rejection is covered.

That is not the production flow.

### Rejection cases worth covering

At minimum one failed dialogue command.

Preferably cover representative failures already implemented:

```text
duplicate create
update missing tree
delete missing tree
```

if the test setup can do so without excessive integration overhead.

## Verification layer 3 — concrete DialogueNpcHook behavior

This layer verifies the runtime ownership/binding logic itself.

Use the existing NWN test harness if available.

If the repository cannot instantiate NWN creatures in automated tests, document a precise manual procedure and observed result.

### Scenario 1 — create binds matching NPCs

Setup:

```text
NPC tag: npc_bob
Tree ID: tree_a
Tree SpeakerTag: npc_bob
```

After synchronization completes:

verify the matching NPC contains:

```text
we_dialogue_tree = tree_a
```

and is hooked for custom conversation handling.

### Scenario 2 — changing speaker tag removes old binding

Initial:

```text
tree_a -> npc_bob
```

Change to:

```text
tree_a -> npc_alice
```

After synchronization completes:

verify:

```text
npc_bob no longer belongs to tree_a
npc_alice has we_dialogue_tree = tree_a
```

Also verify hook state follows the ownership contract from 006D.

### Scenario 3 — clearing speaker tag

Initial:

```text
tree_a -> npc_bob
```

Update:

```text
tree_a -> null
```

Verify old `tree_a` ownership is removed.

### Scenario 4 — delete

Initial:

```text
tree_a -> npc_bob
```

Delete `tree_a`.

Verify only bindings owned by `tree_a` are removed, interpreted according to 006D.

### Scenario 5 — shared tag behavior

This scenario is mandatory because 006D exists specifically to define it.

If 006D chose exclusive ownership:

```text
tree_a -> npc_bob
tree_b -> npc_bob
```

must be rejected according to the chosen validation contract.

Verify the correct rejection location and error shape.

If 006D chose shared ownership:

create the competing trees and verify the exact deterministic winner/fallback behavior defined by the contract.

Do not write a vague "does not crash" assertion.

## Verification layer 4 — controller boundary

Prove the controller no longer owns NPC synchronization.

### Structural checks

```sh
grep -Rni "DialogueNpcHook" \
  AmiaReforged.PwEngine/Features/WorldEngine/API/Controllers
```

Expected:

```text
no matches
```

Check removed helper names:

```sh
grep -RniE \
  "TryRegisterNpcsAsync|TryUpdateNpcRegistrationAsync|TryUnregisterNpcsAsync" \
  AmiaReforged.PwEngine/Features/WorldEngine/API/Controllers
```

Expected:

```text
no matches
```

### Controller behavior

Controller tests should still prove:

```text
POST -> dispatch create -> expected HTTP result
PUT -> dispatch update -> expected HTTP result
DELETE -> dispatch delete -> expected HTTP result
```

They should not mock or inspect the NPC hook.

## Verification layer 5 — asynchronous production semantics

Because:

```text
AnvilEventBusService.PublishAsync()
```

queues work asynchronously, tests must not assume:

```text
await facade.ExecuteAsync(...)
```

means the subscriber finished.

### Acceptable automated strategies

Use one of:

- `InMemoryEventBus` for deterministic subscriber unit/integration behavior;
- direct handler invocation for handler-specific tests;
- an explicit test synchronization primitive around production event processing;
- polling with a bounded timeout in integration tests if no better hook exists.

### Unacceptable strategy

Do not add arbitrary:

```csharp
await Task.Delay(500);
```

and call the behavior verified.

If a bounded wait/poll is necessary, poll a concrete observable condition with a timeout and clear failure message.

## Suggested test organization

Possible test files:

```text
Features/WorldEngine/Subsystems/Dialogue/Tests/
  DialogueNpcSynchronizationHandlerTests.cs
  DialogueNpcHookSynchronizationTests.cs
```

If command-dispatch rejection tests fit better under SharedKernel integration tests, use the existing convention.

## Full regression command

After targeted tests pass:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --no-build \
  --no-restore \
  --filter 'FullyQualifiedName~WorldEngine' \
  --verbosity minimal \
  -m:1
```

If a fresh checkout requires build:

```sh
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj
```

then run the test command.

## Acceptance checks

### Event orchestration

- [ ] successful create with tag registers the correct tree/tag;
- [ ] successful create without tag does not register;
- [ ] successful update forwards new tag;
- [ ] clearing tag triggers removal through update;
- [ ] successful delete unregisters the correct tree;
- [ ] rejected commands do not produce synchronization events/effects.

### Runtime behavior

- [ ] create binds matching NPCs;
- [ ] changing a speaker tag removes the old binding and installs the new one;
- [ ] clearing a tag removes the old binding;
- [ ] delete removes only the binding behavior authorized by 006D;
- [ ] shared-tag scenario behaves exactly as 006D specifies;
- [ ] NWN calls remain main-thread-safe.

### Boundary

- [ ] `DialogueController` contains no direct NPC-hook reference;
- [ ] controller tests do not mock the NPC hook;
- [ ] production asynchronous event semantics are documented/tested correctly.

### Regression

- [ ] targeted dialogue synchronization tests pass;
- [ ] WorldEngine regression suite passes.

## Completion evidence

### 006D contract tested

```text
Ownership model:
```

### Targeted tests added

```text
- ...
```

### Manual NWN procedure, if required

Record exact steps:

```text
1.
2.
3.
```

Observed result:

```text
...
```

### Structural grep

Command:

```sh
...
```

Observed output:

```text
...
```

### Regression command

```sh
...
```

### Observed result

```text
...
```

### Changed test files

```text
- ...
```

### Remaining limitations

Document any behavior that could not be automated and why.

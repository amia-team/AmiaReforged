# 006C — Remove NPC synchronization from DialogueController

Status: **Open**
Type: **Implementation**
Audit area: **F-1**
Depends on: **006B**

## Goal

Make `DialogueController` a pure HTTP/CQRS boundary for dialogue-tree CRUD.

After this task, the controller must not directly resolve or invoke `DialogueNpcHook` or any NPC synchronization service.

## Current direct side effects to remove

In:

```text
AmiaReforged.PwEngine/
  Features/WorldEngine/
    API/Controllers/DialogueController.cs
```

the controller currently performs runtime NPC synchronization after successful commands.

Remove calls equivalent to:

```csharp
await TryRegisterNpcsAsync(...)
await TryUpdateNpcRegistrationAsync(...)
await TryUnregisterNpcsAsync(...)
```

and remove the private helper methods themselves.

## Why this is safe only after 006B

006B must already provide event-driven equivalents:

```text
successful CreateDialogueTreeCommand
  -> CommandExecutedEvent<CreateDialogueTreeCommand>
  -> NPC synchronization subscriber

successful UpdateDialogueTreeCommand
  -> CommandExecutedEvent<UpdateDialogueTreeCommand>
  -> NPC synchronization subscriber

successful DeleteDialogueTreeCommand
  -> CommandExecutedEvent<DeleteDialogueTreeCommand>
  -> NPC synchronization subscriber
```

Do not remove the controller calls before that wiring exists.

## Exact implementation outline

### Step 1 — Confirm 006B is present

Before editing the controller, verify the repository contains an event subscriber for all three dialogue CRUD command types.

Do not proceed based only on a TODO comment or partially implemented handler.

### Step 2 — Remove post-command side effects from Create

Current shape is roughly:

```text
execute CreateDialogueTreeCommand
if failure -> HTTP error
TryRegisterNpcsAsync(...)
query created tree
return 201
```

New shape:

```text
execute CreateDialogueTreeCommand
if failure -> HTTP error
query created tree
return 201
```

Do not wait for NPC synchronization before returning the HTTP response.

### Step 3 — Remove post-command side effects from Update

Current shape includes direct runtime re-registration.

New shape:

```text
execute UpdateDialogueTreeCommand
if failure -> HTTP error
query updated tree
return 200
```

The event subscriber now owns synchronization.

### Step 4 — Remove post-command side effects from Delete

Current shape includes direct unregister.

New shape:

```text
execute DeleteDialogueTreeCommand
if failure -> HTTP error
return 204
```

The event subscriber now owns cleanup.

### Step 5 — Delete helper methods

Delete controller-private methods used only for NPC synchronization, including equivalents of:

```text
TryRegisterNpcsAsync
TryUpdateNpcRegistrationAsync
TryUnregisterNpcsAsync
```

### Step 6 — Remove imports/logger only if now unused

Likely candidates:

```csharp
using Anvil;
using NLog;
```

and:

```csharp
private static readonly Logger Log = ...
```

Remove only if no other controller code uses them.

Do not perform unrelated style cleanup.

## Required semantic result

The controller should know only that:

```text
dialogue create/update/delete command succeeded or failed
```

It should not know:

- that dialogue NPCs are represented by `NwCreature`;
- that `DialogueNpcHook` exists;
- how tree IDs are stamped into local variables;
- how tags are registered;
- how event hooks are attached or removed;
- how main-thread switching works.

## Production timing note

The event bus is asynchronous.

Therefore after this task:

```text
HTTP response success
```

means:

```text
the command completed successfully and the synchronization event was queued
```

not:

```text
all matching NPCs have already been rebound
```

Do not add `Task.Delay`, polling, blocking waits, or controller-level subscriber synchronization just to preserve the previous immediate side effect.

The backlog explicitly accepts asynchronous event freshness.

## Controller tests

Update/add tests so the controller verifies only its CQRS responsibility.

Useful assertions:

### Create

- controller dispatches `CreateDialogueTreeCommand`;
- successful result returns 201;
- controller does not resolve concrete `DialogueNpcHook`.

### Update

- controller dispatches `UpdateDialogueTreeCommand`;
- successful result returns 200;
- controller has no direct NPC synchronization call.

### Delete

- controller dispatches `DeleteDialogueTreeCommand`;
- successful result returns 204;
- controller has no direct NPC synchronization call.

Do not mock `DialogueNpcHook` in controller tests after this task.

## Structural regression checks

Run:

```sh
grep -Rni "DialogueNpcHook" \
  AmiaReforged.PwEngine/Features/WorldEngine/API/Controllers
```

Expected result:

```text
no matches
```

Also check old helper names:

```sh
grep -RniE \
  "TryRegisterNpcsAsync|TryUpdateNpcRegistrationAsync|TryUnregisterNpcsAsync" \
  AmiaReforged.PwEngine/Features/WorldEngine/API/Controllers
```

Expected result:

```text
no matches
```

## Non-goals

Do not:

- change CRUD response codes;
- change validation;
- change DTOs;
- change database entities;
- change event subscriber behavior;
- change `DialogueNpcHook`;
- make HTTP requests wait for event completion;
- refactor unrelated controller helper methods.

## Acceptance checks

- [ ] `DialogueController` no longer references `DialogueNpcHook`.
- [ ] Create no longer directly registers NPCs.
- [ ] Update no longer directly changes NPC registration.
- [ ] Delete no longer directly unregisters NPCs.
- [ ] NPC synchronization helper methods are removed from the controller.
- [ ] CQRS command dispatch and HTTP response behavior remain intact.
- [ ] Controller tests do not construct/mock the NPC hook.
- [ ] Structural grep shows no NPC-hook reference under API controllers.

## Suggested verification

Build:

```sh
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj
```

Target controller tests:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --no-build \
  --filter 'FullyQualifiedName~ControllerCqrsTests' \
  --verbosity minimal \
  -m:1
```

Then WorldEngine tests:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --no-build \
  --no-restore \
  --filter 'FullyQualifiedName~WorldEngine' \
  --verbosity minimal \
  -m:1
```

## Expected changed files

Likely:

```text
Features/WorldEngine/API/Controllers/DialogueController.cs
```

possibly:

```text
Features/WorldEngine/API/Tests/ControllerCqrsTests.cs
```

or a dialogue-controller-specific test file.

Keep the change small.

## Completion evidence

### Removed controller behavior

Record exactly which direct NPC synchronization calls/helpers were deleted.

### Structural grep

```sh
...
```

Observed output:

```text
...
```

### Test command

```sh
...
```

### Observed result

Record exact result.

### Changed files

```text
- ...
```

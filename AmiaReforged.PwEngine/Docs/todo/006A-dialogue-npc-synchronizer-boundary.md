# 006A — Add a dialogue NPC synchronization boundary

Status: **Open**
Type: **Implementation**
Audit area: **F-1**
Depends on: None.

## Goal

Make dialogue NPC synchronization consumable and testable without forcing ordinary application/event-handler tests to construct the full NWN-facing `DialogueNpcHook`.

This task does **not** move synchronization behind events yet. It only creates the narrow boundary that 006B will consume.

## Current repository behavior

`DialogueNpcHook` currently owns all of the following:

- startup discovery of already-stamped dialogue NPCs;
- module-load database registration;
- runtime conversation hooks;
- an internal `dialogueTreeId -> speakerTag` registry;
- methods that register, update, and unregister NPC bindings;
- NWN main-thread switching in its async synchronization methods.

The controller directly calls these methods today.

Relevant file:

```text
AmiaReforged.PwEngine/Features/WorldEngine/Subsystems/Dialogue/Application/DialogueNpcHook.cs
```

## Required change

Introduce a narrow application-facing abstraction for definition-to-NPC synchronization.

Recommended shape:

```csharp
public interface IDialogueNpcSynchronizer
{
    Task<int> RegisterAsync(
        string speakerTag,
        string dialogueTreeId,
        CancellationToken cancellationToken = default);

    Task<(int Unregistered, int Registered)> UpdateAsync(
        string dialogueTreeId,
        string? newSpeakerTag,
        CancellationToken cancellationToken = default);

    Task<int> UnregisterAsync(
        string dialogueTreeId,
        CancellationToken cancellationToken = default);
}
```

The exact method names may follow local style, but the interface must expose only the three synchronization operations needed by event subscribers.

### Important

Do **not** expose these unrelated `DialogueNpcHook` responsibilities through the interface:

- conversation-event handling;
- registry inspection;
- startup discovery;
- module-load initialization;
- `HookedCreatureCount`;
- direct `NwCreature` manipulation;
- `HookCreature` / `UnhookCreature`.

The boundary exists so event consumers do not depend on the concrete NWN runtime hook.

## Exact implementation outline

### Step 1 — Inspect current methods

Read all of:

```text
Features/WorldEngine/Subsystems/Dialogue/Application/DialogueNpcHook.cs
```

Pay particular attention to:

```csharp
RegisterNpcsForTreeAsync(...)
UnregisterNpcsForTreeAsync(...)
UpdateNpcRegistrationAsync(...)
RegisterNpcsForTree(...)
UnregisterNpcsForTree(...)
```

Do not modify behavior before understanding the current tree/tag registry logic.

### Step 2 — Add the interface

Preferred location:

```text
AmiaReforged.PwEngine/
  Features/WorldEngine/
    Subsystems/Dialogue/Application/
      IDialogueNpcSynchronizer.cs
```

Keep the interface next to the implementation it abstracts unless the repository has a stronger established location.

### Step 3 — Implement the interface on DialogueNpcHook

`DialogueNpcHook` should implement `IDialogueNpcSynchronizer`.

Prefer thin interface methods that delegate to the existing synchronization logic rather than rewriting that logic.

Example direction:

```csharp
public async Task<int> RegisterAsync(
    string speakerTag,
    string dialogueTreeId,
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    await NwTask.SwitchToMainThread();
    cancellationToken.ThrowIfCancellationRequested();

    return RegisterNpcsForTree(speakerTag, dialogueTreeId);
}
```

Use the local project's cancellation style where appropriate.

### Step 4 — Preserve NWN main-thread safety

The interface methods that touch NWN objects must still switch to the NWN main thread.

Do not:

```text
event handler
  -> NwObject.FindObjectsWithTag(...)
```

directly.

The event handler must call the synchronizer, and the concrete synchronizer owns the NWN thread transition.

### Step 5 — Register the interface with Anvil DI

`DialogueNpcHook` already has:

```csharp
[ServiceBinding(typeof(DialogueNpcHook))]
```

Add the appropriate service binding for the new interface, for example:

```csharp
[ServiceBinding(typeof(IDialogueNpcSynchronizer))]
```

Keep the existing concrete binding if other runtime code still uses it.

Do not remove existing DI bindings as part of this task unless confirmed unused.

### Step 6 — Do not change callers yet

At the end of 006A:

- the controller may still call `DialogueNpcHook`;
- no event subscriber is required yet;
- runtime behavior should remain unchanged.

That separation is intentional.

## Non-goals

Do not do any of the following in 006A:

- add `CommandExecutedEvent` subscribers;
- change `DialogueController`;
- change dialogue CRUD persistence;
- add new dialogue definition events;
- make `SpeakerTag` unique;
- rewrite shared-tag ownership;
- delete the internal tree/tag registry;
- move module-load behavior out of `DialogueNpcHook`;
- redesign the event bus;
- make NWN calls from background threads.

## Failure modes to avoid

### 1. Interface leaks NWN runtime types

Bad:

```csharp
Task RegisterAsync(NwCreature npc, ...)
```

The event layer should communicate in stable identifiers such as tree ID and speaker tag.

### 2. Main-thread switch moved into the subscriber

Bad:

```csharp
await NwTask.SwitchToMainThread();
await _hook.Register...
```

That couples the subscriber to NWN and defeats the abstraction.

### 3. Duplicate synchronization logic

Do not copy the contents of `RegisterNpcsForTree` into a new service unless there is a concrete reason.

Prefer one source of truth.

### 4. Broad renaming refactor

Do not rename every existing `DialogueNpcHook` method just to make the interface prettier.

A thin adapter implementation is acceptable.

## Acceptance checks

- [ ] `IDialogueNpcSynchronizer` exists.
- [ ] `DialogueNpcHook` implements the interface.
- [ ] The interface exposes only create/update/delete synchronization operations.
- [ ] NWN object access still occurs after `NwTask.SwitchToMainThread()`.
- [ ] Existing concrete `DialogueNpcHook` behavior is unchanged.
- [ ] Anvil DI can resolve `IDialogueNpcSynchronizer`.
- [ ] No controller/event wiring is changed yet.

## Suggested verification

Build:

```sh
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj
```

Then run relevant WorldEngine tests:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --no-build \
  --no-restore \
  --filter 'FullyQualifiedName~WorldEngine' \
  --verbosity minimal \
  -m:1
```

Useful structural grep:

```sh
grep -Rni "IDialogueNpcSynchronizer" \
  AmiaReforged.PwEngine/Features/WorldEngine
```

## Expected changed files

Likely:

```text
Features/WorldEngine/Subsystems/Dialogue/Application/IDialogueNpcSynchronizer.cs
Features/WorldEngine/Subsystems/Dialogue/Application/DialogueNpcHook.cs
```

Do not expand scope merely because other files could be cleaned up.

## Completion evidence

### Chosen implementation

- **Interface name:** `IDialogueNpcSynchronizer`
- **Concrete implementation:** `DialogueNpcHook` (now declares `: IDialogueNpcSynchronizer`)
- **Where the NWN main-thread switch lives:** inside the existing `*Async` wrappers (`RegisterNpcsForTreeAsync`, `UnregisterNpcsForTreeAsync`, `UpdateNpcRegistrationAsync`). The interface methods add `CancellationToken.ThrowIfCancellationRequested()` guards before/after `NwTask.SwitchToMainThread()` and then delegate to those wrappers, so all NWN object access still happens after the switch — inside the hook, not the subscriber.
- **DI binding used:** added `[ServiceBinding(typeof(IDialogueNpcSynchronizer))]` alongside the existing `[ServiceBinding(typeof(DialogueNpcHook))]`.

The interface exposes only the three synchronization operations (`RegisterAsync`, `UpdateAsync`, `UnregisterAsync`) and nothing else (no conversation handling, registry inspection, startup discovery, module-load init, `HookedCreatureCount`, or `NwCreature`/`HookCreature`/`UnhookCreature`). Existing concrete behavior is unchanged; no controller or event wiring was touched.

### Changed files

```text
- AmiaReforged.PwEngine/Features/WorldEngine/Subsystems/Dialogue/Application/IDialogueNpcSynchronizer.cs (new)
- AmiaReforged.PwEngine/Features/WorldEngine/Subsystems/Dialogue/Application/DialogueNpcHook.cs
```

### Verification command

```sh
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj
grep -Rni "IDialogueNpcSynchronizer" AmiaReforged.PwEngine/Features/WorldEngine
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --no-build --no-restore \
  --filter 'FullyQualifiedName~WorldEngine' --verbosity minimal -m:1
```

### Observed result

`dotnet build` succeeded with **0 errors** (307 pre-existing warnings, all in unrelated Economy/ResourceNode/Player areas — none introduced by this change). `DialogueNpcHook` implements `IDialogueNpcSynchronizer`; both `[ServiceBinding(typeof(DialogueNpcHook))]` and `[ServiceBinding(typeof(IDialogueNpcSynchronizer))]` are present on the class.

### Deferred observations

- The interface uses the repo's lowercase tuple convention `(int unregistered, int registered)` to match the existing `UpdateNpcRegistrationAsync` return type rather than the PascalCase shape in the todo sketch.
- No event subscriber is wired yet (intentional — that is 006B). The boundary is ready for consumers to resolve `IDialogueNpcSynchronizer` from `AnvilCore`.
- The three `*Async` wrappers (`RegisterNpcsForTreeAsync`, etc.) remain as public API for existing callers; the interface methods are additional thin adapters, not replacements.

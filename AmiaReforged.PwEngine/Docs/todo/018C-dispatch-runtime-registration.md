# 018C — Dispatch runtime registration on login and PC-key reacquisition

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: **018A**

## Scope

Update:

```text
Subsystems/Characters/Runtime/RuntimeCharacterService.cs
```

so login and PC-key reacquisition dispatch `RegisterRuntimeCharacterCommand` instead of calling `_repository.Add(...)`.

Add `ICommandDispatcher` to `RuntimeCharacterService`.

Keep `ICharacterRepository`; `GetRuntimeCharacter()` still reads it and task 021 owns that read migration.

## Login path

Preserve this sequence:

```text
ignore DM
ObservePlayerPersona
get PC key
store key in _playerKeys
return if key empty
return if LoginCreature null

ForceAssignUUID(LoginCreature, pcKey)
RuntimeCharacter.For(LoginCreature)
dispatch RegisterRuntimeCharacterCommand
SetIsCached
CharacterReady
```

Assign the PC-key UUID **before** `RuntimeCharacter.For`. The factory uses `creature.UUID` as the runtime character ID.

`CharacterReady` must fire only after successful registration dispatch.

## PC-key reacquisition

Keep the existing checks:

```text
item exists
item.Tag == "ds_pckey"
AcquiredBy is player controlled
```

Then preserve:

```text
ObservePlayerPersona
get PC key
update _playerKeys
return if key empty
return if LoginCreature null

ForceAssignUUID(LoginCreature, pcKey)
RuntimeCharacter.For(LoginCreature)
dispatch RegisterRuntimeCharacterCommand
SetIsCached

if previous key was empty:
    CharacterReady
```

Do not fire `CharacterReady` for repeated reacquisition when the previous key was already valid.

Because 018A is idempotent, repeated registration must not duplicate the cached character.

## Required removal

Remove `CreateRuntimeCharacter(...)` if it becomes unused.

No `_repository.Add(...)` call may remain in `RuntimeCharacterService`.

## Do not

- move `_playerKeys` behind commands;
- move `RuntimeCharacter.For` into the handler;
- move UUID assignment into the handler;
- change player-persona behavior;
- modify logout yet;
- change `CharacterReady` into an event-bus event.

## Acceptance

- [ ] Login dispatches `RegisterRuntimeCharacterCommand`.
- [ ] Reacquisition dispatches the same command.
- [ ] PC-key UUID is assigned before `RuntimeCharacter.For`.
- [ ] `CharacterReady` remains after successful cache registration.
- [ ] Reacquisition does not duplicate cached characters.
- [ ] No direct `_repository.Add(...)` remains in `RuntimeCharacterService`.

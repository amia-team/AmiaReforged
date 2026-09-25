# 018D — Dispatch runtime removal on logout and verify lifecycle ordering

Status: **Done**
Type: **Implementation / Verification**
Audit area: **F-7 Characters**
Depends on: **018B, 018C**

## Scope

Update logout in:

```text
Subsystems/Characters/Runtime/RuntimeCharacterService.cs
```

to dispatch `RemoveRuntimeCharacterCommand`.

## Preserve logout ordering

Required order:

```text
ignore DM

if _playerKeys contains non-empty key:
    CharacterLeaving(CharacterId.From(key))

TouchPlayerPersona(player)
remove player from _playerKeys

if LoginCreature null:
    return

dispatch RemoveRuntimeCharacterCommand(
    CharacterId.From(LoginCreature.UUID))

clear PcCachedLvar
```

`CharacterLeaving` must stay before `_playerKeys.Remove(...)` and runtime-character removal.

`QuestObjectiveResolutionService` subscribes to `CharacterLeaving` to tear down quest sessions, so do not reorder it.

Await command dispatch; do not fire-and-forget removal.

Remove `DeleteRuntimeCharacter(...)` if unused.

## Repository boundary

`RuntimeCharacterService` may still use `_repository.GetById(...)` inside `GetRuntimeCharacter()`.

That is intentional. Task 021 owns runtime character reads.

It must not call:

```text
_repository.Add(...)
_repository.Delete(...)
_repository.DeleteById(...)
```

## Verification

```sh
grep -nE '_repository\.(Add|Delete|DeleteById)\('   AmiaReforged.PwEngine/Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacterService.cs
```

Expected: no matches.

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj   --no-build   --filter 'FullyQualifiedName~RuntimeCharacter'   --verbosity minimal   -m:1
```

Then:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj   --no-build   --no-restore   --filter 'FullyQualifiedName~WorldEngine'   --verbosity minimal   -m:1
```

## Do not

- migrate `GetRuntimeCharacter()`; task 021 owns it;
- change `_playerKeys` ownership;
- modify `QuestObjectiveResolutionService`;
- convert `CharacterReady` / `CharacterLeaving` to event-bus events;
- merge this with persistent registration task 017.

## Acceptance

- [ ] Logout dispatches `RemoveRuntimeCharacterCommand`.
- [ ] `CharacterLeaving` fires before key/cache removal.
- [ ] `CharacterReady` still fires after registration.
- [ ] `RuntimeCharacterService` performs no direct repository mutations.
- [ ] Repository reads remain unchanged for task 021.
- [ ] Targeted and WorldEngine tests pass.

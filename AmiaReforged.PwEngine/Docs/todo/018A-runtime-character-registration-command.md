# 018A — Add runtime character registration command

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: None.

## Scope

Move runtime repository **add** semantics behind a command.

Add under:

```text
Subsystems/Characters/Runtime/Commands/
  RegisterRuntimeCharacterCommand.cs
  RegisterRuntimeCharacterCommandHandler.cs
```

## Command

```csharp
RegisterRuntimeCharacterCommand(ICharacter Character)
```

Do not pass `NwPlayer` or `NwCreature`. `RuntimeCharacterService` remains responsible for constructing `RuntimeCharacter`.

## Handler

Inject `ICharacterRepository`.

Behavior:

```text
id = Character.GetId()
if repository.Exists(id):
    return success
repository.Add(Character)
return success
```

Registration must be idempotent. Do not replace an existing cached character.

Register with the normal `ICommandHandler<RegisterRuntimeCharacterCommand>` / `ICommandHandlerMarker` pattern.

Do not manually publish events; `CommandDispatcher` already publishes successful `CommandExecutedEvent<T>`.

## Tests

Without live NWN objects:

- new character → `Add` once, success;
- existing character → no `Add`, success.

## Do not

- modify `RuntimeCharacterService` yet;
- modify `RuntimeCharacterRepository`;
- move `RuntimeCharacter.For`;
- add runtime-character queries.

## Acceptance

- [ ] Registration command/handler exists.
- [ ] Duplicate registration is a successful no-op.
- [ ] Handler tests require no NWN objects.

# 018B — Add runtime character removal command

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: None.

## Scope

Move runtime repository **removal** semantics behind a command.

Add:

```text
Subsystems/Characters/Runtime/Commands/
  RemoveRuntimeCharacterCommand.cs
  RemoveRuntimeCharacterCommandHandler.cs
```

## Command

```csharp
RemoveRuntimeCharacterCommand(CharacterId CharacterId)
```

## Handler

Inject `ICharacterRepository`.

Behavior:

```csharp
repository.DeleteById(command.CharacterId.Value);
return CommandResult.Ok();
```

Removal is idempotent. A missing runtime character is not an error.

Register with the normal command-handler DI pattern. Do not manually publish events.

## Tests

Without live NWN objects:

- removal calls `DeleteById` with the correct GUID;
- command succeeds even when the repository contains no matching character.

## Do not

- modify logout behavior yet;
- modify `_playerKeys`;
- modify `RuntimeCharacterRepository`;
- add runtime-character queries.

## Acceptance

- [ ] Removal command/handler exists.
- [ ] Removal is idempotent.
- [ ] Handler tests require no NWN objects.

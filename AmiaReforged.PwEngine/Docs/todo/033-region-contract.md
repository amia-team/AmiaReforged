# 033 — Reconcile region facade and definition models

Status: **Completed**
Type: **Decision and contract**
Audit area: **F-7 Regions**
Depends on: None.

## Current gap

`RegionInfo` and the facade update record expose Description/Type, which the backing `RegionDefinition` does not contain; multiple update-command types already exist.

## Change

Choose one supported facade projection/update contract using real region fields. Update consumers or add approved backing fields; document which existing CQRS command/query types will be reused.

## Starting points

- [Subsystems/IRegionSubsystem.cs](../../Features/WorldEngine/Subsystems/IRegionSubsystem.cs)
- [Subsystems/Regions/RegionDefinition.cs](../../Features/WorldEngine/Subsystems/Regions/RegionDefinition.cs)

## Acceptance checks

- [ ] Every facade field has defined storage or an explicit compatibility/default rule.
- [ ] Document how the facade update maps to the existing application command, including unsupported fields.
- [ ] The selected contract is compilable and does not introduce another ambiguous update command.

## Decision (contract)

**Chosen projection: add approved backing fields to `RegionDefinition`.**

`RegionInfo` (facade) exposes `Tag, Name, Description, Type`. `RegionDefinition` (domain) previously
only stored `Tag, Name, Areas, DefaultChaos`. Rather than fabricate `Description`/`Type`, both are now
first-class optional fields on `RegionDefinition`:

- `Description` — `string?`, free-form text.
- `Type` — `RegionType?`, classification.

**Facade projection map** (`RegionSubsystem.ToRegionInfo`, task 034):

| RegionInfo field | Source | Rule |
| --- | --- | --- |
| `Tag` | `def.Tag.Value` | direct |
| `Name` | `def.Name` | direct |
| `Description` | `def.Description` | compatibility default: `?? string.Empty` |
| `Type` | `def.Type` | compatibility default: `?? RegionType.Special` |

Every facade field therefore has defined storage or an explicit compatibility/default rule.

**Reused CQRS types (no new commands/queries invented):**

- Reads: `GetRegionDefinitionQuery` (by tag, case-insensitive) and `SearchRegionDefinitionsQuery`
  (empty term = all) — consumed directly by task 034.
- Writes: the application command `AmiaReforged.PwEngine.Features.WorldEngine.Application.Regions.Commands.UpdateRegionCommand`
  (`Tag` + full `RegionDefinition`) is the persistence command. The facade-facing partial
  `UpdateRegionCommand` declared in `IRegionSubsystem` (`Subsystems` namespace, fields
  `RegionTag/Name/Description/Type`) is the request shape and is translated into the application
  command in task 035 (load current definition, apply partial changes, upsert).

> Naming note: **three** types share the name `UpdateRegionCommand` across namespaces, and each
> call site currently has exactly one in scope so it compiles (no additional ambiguous command is
> introduced — acceptance satisfied):
> 1. `...Subsystems.UpdateRegionCommand` (`IRegionSubsystem.cs`, fields `RegionTag/Name/Description/Type`) —
>    the facade request shape, currently a stub (implemented by task 035).
> 2. `...Application.Regions.Commands.UpdateRegionCommand` (`Tag` + full `RegionDefinition`) — used by
>    `RegionController.Update` and handled by `UpdateRegionHandler`.
> 3. `...Subsystems.Regions.Commands.UpdateRegionCommand` (`RegionTag Tag/Name/Areas`) — handled by
>    `UpdateRegionCommandHandler`, exercised by `RegionsCqrsTests`.
> Callers must qualify the name when more than one is in scope.

**Admin panel round-trip:** `RegionController.RegionDto` gained `Description`/`Type`; `FromDto`
populates the domain fields and `ToDto` emits them, so import/export/upsert preserve the new fields.

## Changed files

- `Features/WorldEngine/Subsystems/Regions/RegionDefinition.cs` — added `Description`, `Type`.
- `Features/WorldEngine/API/Controllers/RegionController.cs` — DTO round-trip for `Description`/`Type`
  (region alias added to avoid clashing with the subsystem `UpdateRegionCommand`).
- `Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs` — facade projection (task 034).
- `Features/WorldEngine/Subsystems/IRegionSubsystem.cs` — `RegionInfo` shape unchanged.

## Verification

Command:

```bash
dotnet build AmiaReforged.sln --nologo
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --filter "FullyQualifiedName~Region"
```

Result: build succeeded (0 errors); 57 region tests passed. The contract compiles and introduces no
new ambiguous update command.

See [backlog scope and completion rules](README.md).


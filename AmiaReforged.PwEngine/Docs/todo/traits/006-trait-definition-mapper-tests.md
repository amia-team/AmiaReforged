# Ticket 006 — Add TraitDefinitionMapper serialization contract tests

## Goal

Protect the serialization semantics owned by:

`Features/WorldEngine/Subsystems/Traits/TraitDefinitionMapper.cs`

This task tests the mapper only. It must not instantiate EF Core or PostgreSQL.

## Constraints

- Do not refactor production code.
- Do not modify production behavior.
- Do not instantiate `PwEngineContext`.
- Do not use EF Core.
- Do not use PostgreSQL.
- Do not use Testcontainers.
- Do not add persistence repository tests.
- Do not touch NWN/Anvil runtime objects.
- Use existing NUnit conventions.
- Do not assert raw JSON whitespace or property ordering.
- Do not expand this task into neighboring persistence coverage.

## Create

Create:

`Features/WorldEngine/Subsystems/Traits/Tests/TraitDefinitionMapperTests.cs`

Instantiate `TraitDefinitionMapper` directly.

## Required tests

### `RoundTrip_PreservesTraitDefinitionData`

Create a `Trait` containing non-default values for:

- `Tag`
- `Name`
- `Description`
- `PointCost`
- `Category`
- `DeathBehavior`
- `RequiresUnlock`
- `DmOnly`
- at least two `Effects`
- `AllowedRaces`
- `AllowedClasses`
- `ForbiddenRaces`
- `ForbiddenClasses`
- `ConflictingTraits`
- `PrerequisiteTraits`

Call:

1. `ToPersistent`
2. `ToDomain`

Assert the reconstructed domain trait preserves every value listed above.

For effects, assert:

- `EffectType`
- `Target`
- `Magnitude`
- `Description`

### `ToDomain_WhenJsonCollectionsAreEmpty_ReturnsEmptyCollections`

Construct a `PersistedTraitDefinition` with all JSON collection fields set to `"[]"`.

Assert all corresponding domain collections are empty.

### `ToDomain_WhenJsonCollectionsAreWhitespace_ReturnsEmptyCollections`

Set:

- `EffectsJson`
- all race/class lists
- conflict list
- prerequisite list

to whitespace strings.

Assert all corresponding domain collections are empty.

### `ToDomain_WhenJsonIsMalformed_ReturnsEmptyCollections`

Use malformed JSON for every JSON-backed collection.

Assert:

- Mapping does not throw.
- Each malformed collection becomes empty.

### `ToDomain_WhenEffectTypeIsUnknown_MapsEffectTypeToNone`

Provide an `EffectsJson` array containing one effect with numeric:

```text
EffectType = 999
```

and known target/magnitude/description values.

Assert:

- Exactly one effect is returned.
- `EffectType == TraitEffectType.None`.
- Target, magnitude, and description are preserved.

## Acceptance

Run:

```bash
dotnet test --filter FullyQualifiedName~TraitDefinitionMapperTests
```

All tests pass.

No production files are changed.

# Ticket 007 — Add pure HasTraitExecutor execution tests

## Goal

Cover the pure Glyph behavior in:

`Features/Glyph/Runtime/Nodes/Traits/HasTraitExecutor.cs`

This executor reads only the Glyph execution context and resolved input. It does not require an NWN object.

Do not test `GlyphTraitHookService` or any NWN-bound trait integration in this task.

## Constraints

- Do not refactor production code.
- Do not modify executor behavior.
- Do not touch NWN/Anvil runtime objects.
- Do not add Glyph hook-service tests.
- Do not introduce new test infrastructure.
- Follow the existing Glyph runtime-test style.
- Use FluentAssertions, matching neighboring Glyph tests.
- Do not expand this task into other Glyph trait executors.

## Create

Create:

`Features/Glyph/Runtime/Tests/HasTraitExecutorTests.cs`

Follow the style in:

`Features/Glyph/Runtime/Tests/ConstantsExecutorTests.cs`

## Required tests

### `ExecuteAsync_WhenTraitListContainsTag_ReturnsTrue`

Context variable:

```csharp
["character_traits"] = new List<string> { "brave", "hero" }
```

Resolve `"trait_tag"` to `"hero"`.

Assert:

```csharp
result.OutputValues["has_trait"]
```

is `true`.

### `ExecuteAsync_WhenTraitTagDiffersOnlyByCase_ReturnsTrue`

Stored trait:

```text
Hero
```

Input:

```text
hero
```

Assert `true`.

### `ExecuteAsync_WhenTraitIsAbsent_ReturnsFalse`

Stored traits do not contain requested tag.

Assert `false`.

### `ExecuteAsync_WhenCharacterTraitsVariableIsMissing_ReturnsFalse`

Do not put `"character_traits"` into `context.Variables`.

Assert `false`.

### `ExecuteAsync_WhenCharacterTraitsVariableHasWrongType_ReturnsFalse`

Set:

```csharp
context.Variables["character_traits"] = "hero";
```

Assert `false`.

### `ExecuteAsync_WhenTraitTagInputIsNull_ReturnsFalse`

Use a normal non-empty list of actual trait tags.

Resolve `"trait_tag"` to `null`.

Assert `false`.

### `CreateDefinition_DescribesHasTraitNode`

Assert:

- `TypeId == HasTraitExecutor.NodeTypeId`
- category is `"Traits"`
- input contains `trait_tag`
- `trait_tag` input type is `GlyphDataType.String`
- output contains `has_trait`
- `has_trait` output type is `GlyphDataType.Bool`

## Acceptance

Run:

```bash
dotnet test --filter FullyQualifiedName~HasTraitExecutorTests
```

All tests pass.

No production files are changed.

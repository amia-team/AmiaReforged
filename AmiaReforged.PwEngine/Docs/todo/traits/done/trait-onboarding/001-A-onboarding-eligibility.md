# Subtask 001-A — Add Trait Onboarding Eligibility Check

## Goal

Add one reusable, testable eligibility check that determines whether a character has completed trait selection.

This task contains no NUI and no NWN event wiring.

## Required Rule

A character has completed trait selection when at least one of the character's trait records is confirmed:

```csharp
characterTraits.Any(t => t.IsConfirmed)
```

The onboarding prompt is eligible when:

```csharp
!characterTraits.Any(t => t.IsConfirmed)
```

Do not use trait budget.

Do not use `characterTraits.Count == 0`.

## Required Implementation

Add a small Traits-domain helper/service under:

`Features/WorldEngine/Subsystems/Traits/`

Name it:

`TraitOnboardingEligibility`

Expose exactly:

```csharp
public static bool ShouldPrompt(IReadOnlyCollection<CharacterTrait> characterTraits)
```

Behavior:

```text
no trait rows                         => true
only unconfirmed trait rows           => true
one confirmed trait                   => false
mixed confirmed + unconfirmed traits  => false
```

## Tests

Create:

`Features/WorldEngine/Subsystems/Traits/Tests/TraitOnboardingEligibilityTests.cs`

Add exactly these tests:

- `ShouldPrompt_WhenNoTraitsExist_ReturnsTrue`
- `ShouldPrompt_WhenOnlyUnconfirmedTraitsExist_ReturnsTrue`
- `ShouldPrompt_WhenConfirmedTraitExists_ReturnsFalse`
- `ShouldPrompt_WhenConfirmedAndUnconfirmedTraitsExist_ReturnsFalse`

Use normal `CharacterTrait` instances.

Do not instantiate NWN objects.

## Constraints

- Do not modify `CharacterTrait`.
- Do not modify `TraitBudget`.
- Do not modify `TraitSelectionService`.
- Do not modify confirmation behavior.
- Do not query repositories inside `TraitOnboardingEligibility`.
- Do not add DI.
- Do not add persistence.
- Do not add event handlers.
- Do not add extra eligibility rules.

## Acceptance

```bash
dotnet test --filter FullyQualifiedName~TraitOnboardingEligibilityTests
```

passes.

No unrelated production files are modified.

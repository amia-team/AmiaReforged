using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Tests;

/// <summary>
/// Direct coverage for the TraitOnboardingEligibility domain check.
/// </summary>
[TestFixture]
public class TraitOnboardingEligibilityTests
{
    private static CharacterTrait UnconfirmedTrait() => new()
    {
        Id = Guid.NewGuid(),
        CharacterId = new CharacterId(Guid.NewGuid()),
        TraitTag = new TraitTag("trait_test_unconfirmed"),
        IsConfirmed = false
    };

    private static CharacterTrait ConfirmedTrait() => new()
    {
        Id = Guid.NewGuid(),
        CharacterId = new CharacterId(Guid.NewGuid()),
        TraitTag = new TraitTag("trait_test_confirmed"),
        IsConfirmed = true
    };

    [Test]
    public void ShouldPrompt_WhenNoTraitsExist_ReturnsTrue()
    {
        // Arrange
        IReadOnlyCollection<CharacterTrait> characterTraits = Array.Empty<CharacterTrait>();

        // Act & Assert
        Assert.That(TraitOnboardingEligibility.IsEligible(characterTraits), Is.True);
    }

    [Test]
    public void ShouldPrompt_WhenOnlyUnconfirmedTraitsExist_ReturnsTrue()
    {
        // Arrange
        IReadOnlyCollection<CharacterTrait> characterTraits = new[]
        {
            UnconfirmedTrait(),
            UnconfirmedTrait()
        };

        // Act & Assert
        Assert.That(TraitOnboardingEligibility.IsEligible(characterTraits), Is.True);
    }

    [Test]
    public void ShouldPrompt_WhenConfirmedTraitExists_ReturnsFalse()
    {
        // Arrange
        IReadOnlyCollection<CharacterTrait> characterTraits = new[]
        {
            ConfirmedTrait()
        };

        // Act & Assert
        Assert.That(TraitOnboardingEligibility.IsEligible(characterTraits), Is.False);
    }

    [Test]
    public void ShouldPrompt_WhenConfirmedAndUnconfirmedTraitsExist_ReturnsFalse()
    {
        // Arrange
        IReadOnlyCollection<CharacterTrait> characterTraits = new[]
        {
            ConfirmedTrait(),
            UnconfirmedTrait()
        };

        // Act & Assert
        Assert.That(TraitOnboardingEligibility.IsEligible(characterTraits), Is.False);
    }
}

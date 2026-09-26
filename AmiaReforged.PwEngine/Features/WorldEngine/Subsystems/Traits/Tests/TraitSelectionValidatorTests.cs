using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Tests;

/// <summary>
/// Unit tests for TraitSelectionValidator covering duplicate, race/class
/// eligibility, and budget (non-)enforcement rules.
/// </summary>
[TestFixture]
public class TraitSelectionValidatorTests
{
    private static Trait NewTrait(string tag) =>
        new()
        {
            Tag = tag,
            Name = tag,
            Description = tag,
            PointCost = 1
        };

    private static CharacterTrait Selected(string tag) =>
        new()
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(Guid.NewGuid()),
            TraitTag = new TraitTag(tag),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = false
        };

    #region Duplicate Selection

    [Test]
    public void CanSelect_WhenTraitAlreadySelected_ReturnsFalse()
    {
        // Arrange
        Trait trait = NewTrait("brave");
        ICharacterInfo character = TestCharacterInfo.From("Human", "Fighter");
        List<CharacterTrait> currentSelections = [Selected("brave")];
        TraitBudget budget = TraitBudget.CreateDefault();

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(trait, character, currentSelections, budget);

        // Assert
        Assert.That(canSelect, Is.False);
    }

    #endregion

    #region Race Eligibility

    [Test]
    public void CanSelect_WhenRaceIsInAllowedRaces_ReturnsTrue()
    {
        // Arrange
        Trait trait = new Trait
        {
            Tag = "noble",
            Name = "noble",
            Description = "noble",
            PointCost = 1,
            AllowedRaces = new List<string> { "Human" }
        };
        ICharacterInfo character = TestCharacterInfo.From("Human");
        TraitBudget budget = TraitBudget.CreateDefault();
        List<CharacterTrait> currentSelections = new();

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(trait, character, currentSelections, budget);

        // Assert
        Assert.That(canSelect, Is.True);
    }

    [Test]
    public void CanSelect_WhenRaceIsNotInAllowedRaces_ReturnsFalse()
    {
        // Arrange
        Trait trait = new Trait
        {
            Tag = "noble",
            Name = "noble",
            Description = "noble",
            PointCost = 1,
            AllowedRaces = new List<string> { "Elf" }
        };
        ICharacterInfo character = TestCharacterInfo.From("Human");
        TraitBudget budget = TraitBudget.CreateDefault();
        List<CharacterTrait> currentSelections = new();

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(trait, character, currentSelections, budget);

        // Assert
        Assert.That(canSelect, Is.False);
    }

    [Test]
    public void CanSelect_WhenRaceIsForbidden_ReturnsFalse()
    {
        // Arrange
        Trait trait = new Trait
        {
            Tag = "noble",
            Name = "noble",
            Description = "noble",
            PointCost = 1,
            ForbiddenRaces = new List<string> { "Human" }
        };
        ICharacterInfo character = TestCharacterInfo.From("Human");
        TraitBudget budget = TraitBudget.CreateDefault();
        List<CharacterTrait> currentSelections = new();

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(trait, character, currentSelections, budget);

        // Assert
        Assert.That(canSelect, Is.False);
    }

    [Test]
    public void CanSelect_WhenRaceIsBothAllowedAndForbidden_ReturnsFalse()
    {
        // Arrange
        Trait trait = new Trait
        {
            Tag = "noble",
            Name = "noble",
            Description = "noble",
            PointCost = 1,
            AllowedRaces = new List<string> { "Human" },
            ForbiddenRaces = new List<string> { "Human" }
        };
        ICharacterInfo character = TestCharacterInfo.From("Human");
        TraitBudget budget = TraitBudget.CreateDefault();
        List<CharacterTrait> currentSelections = new();

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(trait, character, currentSelections, budget);

        // Assert
        Assert.That(canSelect, Is.False);
    }

    #endregion

    #region Class Eligibility

    [Test]
    public void CanSelect_WhenAnyCharacterClassIsAllowed_ReturnsTrue()
    {
        // Arrange
        Trait trait = new Trait
        {
            Tag = "arcane",
            Name = "arcane",
            Description = "arcane",
            PointCost = 1,
            AllowedClasses = new List<string> { "Wizard" }
        };
        ICharacterInfo character = TestCharacterInfo.From("Fighter", "Wizard");
        TraitBudget budget = TraitBudget.CreateDefault();
        List<CharacterTrait> currentSelections = new();

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(trait, character, currentSelections, budget);

        // Assert
        Assert.That(canSelect, Is.True);
    }

    [Test]
    public void CanSelect_WhenNoCharacterClassIsAllowed_ReturnsFalse()
    {
        // Arrange
        Trait trait = new Trait
        {
            Tag = "arcane",
            Name = "arcane",
            Description = "arcane",
            PointCost = 1,
            AllowedClasses = new List<string> { "Wizard" }
        };
        ICharacterInfo character = TestCharacterInfo.From("Fighter", "Rogue");
        TraitBudget budget = TraitBudget.CreateDefault();
        List<CharacterTrait> currentSelections = new();

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(trait, character, currentSelections, budget);

        // Assert
        Assert.That(canSelect, Is.False);
    }

    [Test]
    public void CanSelect_WhenAnyCharacterClassIsForbidden_ReturnsFalse()
    {
        // Arrange
        Trait trait = new Trait
        {
            Tag = "arcane",
            Name = "arcane",
            Description = "arcane",
            PointCost = 1,
            ForbiddenClasses = new List<string> { "Wizard" }
        };
        ICharacterInfo character = TestCharacterInfo.From("Fighter", "Wizard");
        TraitBudget budget = TraitBudget.CreateDefault();
        List<CharacterTrait> currentSelections = new();

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(trait, character, currentSelections, budget);

        // Assert
        Assert.That(canSelect, Is.False);
    }

    [Test]
    public void CanSelect_WhenClassIsBothAllowedAndForbidden_ReturnsFalse()
    {
        // Arrange
        Trait trait = new Trait
        {
            Tag = "arcane",
            Name = "arcane",
            Description = "arcane",
            PointCost = 1,
            AllowedClasses = new List<string> { "Wizard" },
            ForbiddenClasses = new List<string> { "Wizard" }
        };
        ICharacterInfo character = TestCharacterInfo.From("Wizard");
        TraitBudget budget = TraitBudget.CreateDefault();
        List<CharacterTrait> currentSelections = new();

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(trait, character, currentSelections, budget);

        // Assert
        Assert.That(canSelect, Is.False);
    }

    #endregion

    #region Budget (Not Enforced During Selection)

    [Test]
    public void CanSelect_WhenTraitCostsMoreThanAvailableBudget_StillReturnsTrue()
    {
        // Arrange - an otherwise-valid unrestricted trait that costs far more
        // than the default budget's 2 available points.
        Trait trait = new Trait
        {
            Tag = "legendary",
            Name = "legendary",
            Description = "legendary",
            PointCost = 10
        };
        ICharacterInfo character = TestCharacterInfo.From("Human", "Fighter");
        List<CharacterTrait> currentSelections = [];
        TraitBudget budget = TraitBudget.CreateDefault();

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(trait, character, currentSelections, budget);

        // Assert - budget is enforced at confirmation, not selection.
        Assert.That(canSelect, Is.True);
    }

    #endregion
}

using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Effects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.TraitSystem;

/// <summary>
/// BDD-style tests defining the behavior of the background trait system.
/// These tests capture domain logic before implementation.
/// </summary>
[TestFixture]
public class BackgroundTraitTests
{
    #region Trait Budget and Point Management

    [Test]
    public void NewCharacter_ShouldHave_TwoFreeTraitPoints()
    {
        // Arrange & Act
        TraitBudget budget = TraitBudget.CreateDefault();

        // Assert
        Assert.That(budget.BasePoints, Is.EqualTo(2));
        Assert.That(budget.TotalPoints, Is.EqualTo(2));
        Assert.That(budget.AvailablePoints, Is.EqualTo(2));
        Assert.That(budget.EarnedPoints, Is.EqualTo(0));
        Assert.That(budget.SpentPoints, Is.EqualTo(0));
    }

    [Test]
    public void SelectingPositiveTrait_ShouldConsume_TraitPoints()
    {
        // Arrange
        TraitBudget budget = TraitBudget.CreateDefault();
        const int positiveTraitCost = 1;

        // Act
        TraitBudget afterSpending = budget.AfterSpending(positiveTraitCost);

        // Assert
        Assert.That(afterSpending.SpentPoints, Is.EqualTo(1));
        Assert.That(afterSpending.AvailablePoints, Is.EqualTo(1));
    }

    [Test]
    public void SelectingNegativeTrait_ShouldIncrease_AvailableTraitPoints()
    {
        // Arrange
        TraitBudget budget = TraitBudget.CreateDefault();
        const int negativeTraitCost = -1; // Negative cost grants points

        // Act
        TraitBudget afterSpending = budget.AfterSpending(negativeTraitCost);

        // Assert
        Assert.That(afterSpending.SpentPoints, Is.EqualTo(-1));
        Assert.That(afterSpending.AvailablePoints, Is.EqualTo(3)); // 2 base + 1 from negative trait
    }

    [Test]
    public void CannotSelectTrait_WhenInsufficientPoints()
    {
        // Arrange
        TraitBudget budget = TraitBudget.CreateDefault();
        const int expensiveTraitCost = 3; // Costs more than available

        // Act & Assert
        Assert.That(budget.CanAfford(expensiveTraitCost), Is.False);
    }

    [Test]
    public void TraitBudget_ShouldCalculate_CorrectAvailablePoints()
    {
        // Arrange: 2 base points, 1 earned point, spent 2 points
        TraitBudget budget = new TraitBudget
        {
            EarnedPoints = 1,
            SpentPoints = 2
        };

        // Assert
        Assert.That(budget.TotalPoints, Is.EqualTo(3)); // 2 base + 1 earned
        Assert.That(budget.AvailablePoints, Is.EqualTo(1)); // 3 total - 2 spent
    }

    #endregion

    #region Trait Selection and Deselection

    [Test]
    public void CanSelectTrait_WhenWithinBudget_AndEligible()
    {
        // Arrange
        Trait braveTrait = new Trait
        {
            Tag = "brave",
            Name = "Brave",
            Description = "Fearless in combat",
            PointCost = 1
        };

        ICharacterInfo character = TestCharacterInfo.From("Human", "Fighter");

        TraitBudget budget = TraitBudget.CreateDefault();
        List<CharacterTrait> selectedTraits = [];

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(braveTrait, character, selectedTraits, budget);

        // Assert
        Assert.That(canSelect, Is.True);
    }

    [Test]
    public void CanDeselectUnconfirmedTrait_AndRecoverPoints()
    {
        // Arrange
        CharacterTrait unconfirmedTrait = new CharacterTrait
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(Guid.NewGuid()),
            TraitTag = new TraitTag("brave"),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = false
        };

        // Act
        bool canDeselect = TraitSelectionValidator.CanDeselect(unconfirmedTrait);

        // Assert
        Assert.That(canDeselect, Is.True);
    }

    [Test]
    public void CannotDeselectConfirmedTrait()
    {
        // Arrange - Confirmed traits are permanent and cannot be deselected by players
        CharacterTrait confirmedTrait = new CharacterTrait
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(Guid.NewGuid()),
            TraitTag = new TraitTag("brave"),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true
        };

        // Act
        bool canDeselect = TraitSelectionValidator.CanDeselect(confirmedTrait);

        // Assert
        Assert.That(canDeselect, Is.False, "Confirmed traits are permanent and cannot be deselected by players");
    }

    [Test]
    public void SelectingMultipleTraits_ShouldAccumulate_PointCosts()
    {
        // Arrange
        Trait trait1 = new Trait { Tag = "brave", Name = "Brave", Description = "Brave", PointCost = 1 };
        Trait trait2 = new Trait { Tag = "strong", Name = "Strong", Description = "Strong", PointCost = 1 };

        TraitBudget budget = TraitBudget.CreateDefault();

        // Act
        TraitBudget afterFirst = budget.AfterSpending(trait1.PointCost);
        TraitBudget afterSecond = afterFirst.AfterSpending(trait2.PointCost);

        // Assert
        Assert.That(afterSecond.SpentPoints, Is.EqualTo(2));
        Assert.That(afterSecond.AvailablePoints, Is.EqualTo(0));
    }

    #endregion

    #region Trait Confirmation and Permanence

    [Test]
    public void UnconfirmedTraits_CanBeChanged_BeforeFinalization()
    {
        // Arrange
        ITraitRepository traitRepo = InMemoryTraitRepository.Create();
        traitRepo.Add(new Trait
        {
            Tag = "brave",
            Name = "Brave",
            Description = "Fearless",
            PointCost = 1
        });

        ICharacterTraitRepository charTraitRepo = InMemoryCharacterTraitRepository.Create();
        TraitSelectionService service = TraitSelectionService.Create(charTraitRepo, traitRepo);

        Guid characterId = Guid.NewGuid();
        ICharacterInfo character = TestCharacterInfo.From("Human", "Fighter");
        Dictionary<string, bool> unlockedTraits = new();

        // Act - Select, then deselect before confirming
        bool selected = service.SelectTrait(characterId, "brave", character, unlockedTraits);
        bool deselected = service.DeselectTrait(characterId, "brave");
        List<CharacterTrait> traits = service.GetCharacterTraits(characterId);

        // Assert
        Assert.That(selected, Is.True);
        Assert.That(deselected, Is.True);
        Assert.That(traits, Is.Empty);
    }

    [Test]
    public void ConfirmingTraits_MakesThemPermanent()
    {
        // Arrange
        ITraitRepository traitRepo = InMemoryTraitRepository.Create();
        traitRepo.Add(new Trait
        {
            Tag = "brave",
            Name = "Brave",
            Description = "Fearless",
            PointCost = 1
        });

        ICharacterTraitRepository charTraitRepo = InMemoryCharacterTraitRepository.Create();
        TraitSelectionService service = TraitSelectionService.Create(charTraitRepo, traitRepo);

        Guid characterId = Guid.NewGuid();
        ICharacterInfo character = TestCharacterInfo.From("Human", "Fighter");
        Dictionary<string, bool> unlockedTraits = new();

        // Act - Select trait and confirm
        service.SelectTrait(characterId, "brave", character, unlockedTraits);
        bool confirmed = service.ConfirmTraits(characterId);
        List<CharacterTrait> traits = service.GetCharacterTraits(characterId);
        bool canDeselect = service.DeselectTrait(characterId, "brave");

        // Assert
        Assert.That(confirmed, Is.True);
        Assert.That(traits, Has.Count.EqualTo(1));
        Assert.That(traits[0].IsConfirmed, Is.True);
        Assert.That(canDeselect, Is.False, "Confirmed traits are permanent and cannot be deselected by players");
    }

    [Test]
    public void CannotConfirmTraits_WhenBudgetIsNegative()
    {
        // Arrange
        ITraitRepository traitRepo = InMemoryTraitRepository.Create();
        traitRepo.Add(new Trait
        {
            Tag = "expensive",
            Name = "Expensive Trait",
            Description = "Costs too much",
            PointCost = 5
        });

        ICharacterTraitRepository charTraitRepo = InMemoryCharacterTraitRepository.Create();
        TraitSelectionService service = TraitSelectionService.Create(charTraitRepo, traitRepo);

        Guid characterId = Guid.NewGuid();
        ICharacterInfo character = TestCharacterInfo.From("Human", "Fighter");
        Dictionary<string, bool> unlockedTraits = new();

        // Act - Select expensive trait (costs 5, budget is 2) and try to confirm
        service.SelectTrait(characterId, "expensive", character, unlockedTraits);
        bool confirmed = service.ConfirmTraits(characterId);
        List<CharacterTrait> traits = service.GetCharacterTraits(characterId);

        // Assert
        Assert.That(confirmed, Is.False, "Should not confirm when budget is negative");
        Assert.That(traits, Has.Count.EqualTo(1));
        Assert.That(traits[0].IsConfirmed, Is.False, "Trait should remain unconfirmed");
    }

    [Test]
    public void ConfirmedTraits_PersistAcrossLogins()
    {
        // Arrange
        ITraitRepository traitRepo = InMemoryTraitRepository.Create();
        traitRepo.Add(new Trait
        {
            Tag = "brave",
            Name = "Brave",
            Description = "Fearless",
            PointCost = 1
        });

        ICharacterTraitRepository charTraitRepo = InMemoryCharacterTraitRepository.Create();
        TraitSelectionService service = TraitSelectionService.Create(charTraitRepo, traitRepo);

        Guid characterId = Guid.NewGuid();
        ICharacterInfo character = TestCharacterInfo.From("Human", "Fighter");
        Dictionary<string, bool> unlockedTraits = new();

        // Act - Select, confirm, then simulate "login" by querying again
        service.SelectTrait(characterId, "brave", character, unlockedTraits);
        service.ConfirmTraits(characterId);

        // Simulate new login by retrieving persisted traits
        List<CharacterTrait> traitsAfterLogin = service.GetCharacterTraits(characterId);

        // Assert
        Assert.That(traitsAfterLogin, Has.Count.EqualTo(1));
        Assert.That(traitsAfterLogin[0].IsConfirmed, Is.True);
        Assert.That(traitsAfterLogin[0].TraitTag.Value, Is.EqualTo("brave"));
    }

    #endregion

    #region Trait Eligibility and Prerequisites

    [Test]
    public void BaseTraits_ShouldAlwaysBeAvailable()
    {
        // Arrange - Base trait that doesn't require unlock
        Trait baseTrait = new Trait
        {
            Tag = "brave",
            Name = "Brave",
            Description = "Fearless",
            PointCost = 1,
            RequiresUnlock = false
        };

        ICharacterInfo character = TestCharacterInfo.From("Human", "Fighter");

        TraitBudget budget = TraitBudget.CreateDefault();
        List<CharacterTrait> selectedTraits = [];
        Dictionary<string, bool> unlockedTraits = new();

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(
            baseTrait, character, selectedTraits, budget, unlockedTraits);

        // Assert
        Assert.That(canSelect, Is.True);
    }

    [Test]
    public void EarnedTraits_RequireUnlock_BeforeSelection()
    {
        // Arrange - Trait that requires unlock
        Trait earnedTrait = new Trait
        {
            Tag = "hero",
            Name = "Hero",
            Description = "Heroic deed",
            PointCost = 2,
            RequiresUnlock = true
        };

        ICharacterInfo character = TestCharacterInfo.From("Human", "Fighter");

        TraitBudget budget = TraitBudget.CreateDefault();
        List<CharacterTrait> selectedTraits = [];
        Dictionary<string, bool> unlockedTraits = new(); // Not unlocked

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(
            earnedTrait, character, selectedTraits, budget, unlockedTraits);

        // Assert
        Assert.That(canSelect, Is.False);
    }

    [Test]
    public void CannotSelectTrait_WithoutMeetingPrerequisites()
    {
        // Arrange - Trait with prerequisites
        Trait advancedTrait = new Trait
        {
            Tag = "expert_drinker",
            Name = "Expert Drinker",
            Description = "Can hold liquor",
            PointCost = 1,
            PrerequisiteTraits = ["alcoholic"]
        };

        ICharacterInfo character = TestCharacterInfo.From("Human", "Fighter");

        TraitBudget budget = TraitBudget.CreateDefault();
        List<CharacterTrait> selectedTraits = []; // No prerequisites selected
        Dictionary<string, bool> unlockedTraits = new();

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(
            advancedTrait, character, selectedTraits, budget, unlockedTraits);

        // Assert
        Assert.That(canSelect, Is.False);
    }

    [Test]
    public void UnlockingTrait_MakesItAvailableForSelection()
    {
        // Arrange - Trait that requires unlock, but is now unlocked
        Trait earnedTrait = new Trait
        {
            Tag = "hero",
            Name = "Hero",
            Description = "Heroic deed",
            PointCost = 2,
            RequiresUnlock = true
        };

        ICharacterInfo character = TestCharacterInfo.From("Human", "Fighter");

        TraitBudget budget = TraitBudget.CreateDefault();
        List<CharacterTrait> selectedTraits = [];
        Dictionary<string, bool> unlockedTraits = new() { { "hero", true } }; // Unlocked!

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(
            earnedTrait, character, selectedTraits, budget, unlockedTraits);

        // Assert
        Assert.That(canSelect, Is.True);
    }

    [Test]
    public void PrerequisiteCheck_ShouldValidate_OtherSelectedTraits()
    {
        // Arrange - Chain: Alcoholic -> Expert Drinker
        Trait prerequisiteTrait = new Trait
        {
            Tag = "alcoholic",
            Name = "Alcoholic",
            Description = "Addicted to drink",
            PointCost = -1
        };

        Trait advancedTrait = new Trait
        {
            Tag = "expert_drinker",
            Name = "Expert Drinker",
            Description = "Can hold liquor",
            PointCost = 1,
            PrerequisiteTraits = ["alcoholic"]
        };

        ICharacterInfo character = TestCharacterInfo.From("Human", "Fighter");

        TraitBudget budget = TraitBudget.CreateDefault();

        // Character has selected the prerequisite
        List<CharacterTrait> selectedTraits =
        [
            new CharacterTrait
            {
                Id = Guid.NewGuid(),
                CharacterId = CharacterId.From(Guid.NewGuid()),
                TraitTag = new TraitTag("alcoholic"),
                DateAcquired = DateTime.UtcNow,
                IsConfirmed = false
            }
        ];

        Dictionary<string, bool> unlockedTraits = new();

        // Act
        bool canSelect = TraitSelectionValidator.CanSelect(
            advancedTrait, character, selectedTraits, budget, unlockedTraits);

        // Assert
        Assert.That(canSelect, Is.True);
    }

    #endregion

    #region Death Mechanics and Trait Lifecycle

    [Test]
    public void StandardTraits_PersistThroughDeath()
    {
        // Arrange
        ITraitRepository traitRepo = InMemoryTraitRepository.Create();
        traitRepo.Add(new Trait
        {
            Tag = "brave",
            Name = "Brave",
            Description = "Fearless",
            PointCost = 1,
            DeathBehavior = TraitDeathBehavior.Persist
        });

        ICharacterTraitRepository charTraitRepo = InMemoryCharacterTraitRepository.Create();
        Guid characterId = Guid.NewGuid();

        CharacterTrait braveTrait = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag("brave"),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true,
            IsActive = true
        };
        charTraitRepo.Add(braveTrait);

        TraitDeathHandler deathHandler = TraitDeathHandler.Create(charTraitRepo, traitRepo);

        // Act - Character dies
        bool permadeath = deathHandler.ProcessDeath(CharacterId.From(characterId));
        List<CharacterTrait> traitsAfterDeath = charTraitRepo.GetByCharacterId(CharacterId.From(characterId));

        // Assert
        Assert.That(permadeath, Is.False, "Standard traits should not cause permadeath");
        Assert.That(traitsAfterDeath, Has.Count.EqualTo(1), "Trait should still exist");
        Assert.That(traitsAfterDeath[0].IsActive, Is.True, "Trait should remain active");
        Assert.That(traitsAfterDeath[0].TraitTag.Value, Is.EqualTo("brave"));
    }

    [Test]
    public void HeroTrait_BonusesResetOnDeath()
    {
        // Hero keeps the trait but loses accumulated bonuses
        // Arrange
        ITraitRepository traitRepo = InMemoryTraitRepository.Create();
        traitRepo.Add(new Trait
        {
            Tag = "hero",
            Name = "Hero",
            Description = "Heroic character",
            PointCost = 2,
            DeathBehavior = TraitDeathBehavior.ResetOnDeath
        });

        ICharacterTraitRepository charTraitRepo = InMemoryCharacterTraitRepository.Create();
        Guid characterId = Guid.NewGuid();

        CharacterTrait heroTrait = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag("hero"),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true,
            IsActive = true,
            CustomData = "{\"bonuses\": [\"strength+1\", \"constitution+1\"]}" // Accumulated bonuses
        };
        charTraitRepo.Add(heroTrait);

        TraitDeathHandler deathHandler = TraitDeathHandler.Create(charTraitRepo, traitRepo);

        // Act - Character dies
        bool permadeath = deathHandler.ProcessDeath(CharacterId.From(characterId));
        List<CharacterTrait> traitsAfterDeath = charTraitRepo.GetByCharacterId(CharacterId.From(characterId));

        // Assert
        Assert.That(permadeath, Is.False, "Hero death should not cause permadeath");
        Assert.That(traitsAfterDeath, Has.Count.EqualTo(1), "Hero trait should still exist");
        Assert.That(traitsAfterDeath[0].IsActive, Is.False, "Hero trait should be inactive after death");
        Assert.That(traitsAfterDeath[0].CustomData, Is.Null, "Custom data (bonuses) should be cleared");
    }

    [Test]
    public void HeroTrait_CanRebuildBonuses_AfterDeath()
    {
        // Arrange
        ITraitRepository traitRepo = InMemoryTraitRepository.Create();
        traitRepo.Add(new Trait
        {
            Tag = "hero",
            Name = "Hero",
            Description = "Heroic character",
            PointCost = 2,
            DeathBehavior = TraitDeathBehavior.ResetOnDeath
        });

        ICharacterTraitRepository charTraitRepo = InMemoryCharacterTraitRepository.Create();
        Guid characterId = Guid.NewGuid();

        CharacterTrait heroTrait = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag("hero"),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true,
            IsActive = false, // Inactive after death
            CustomData = null
        };
        charTraitRepo.Add(heroTrait);

        TraitDeathHandler deathHandler = TraitDeathHandler.Create(charTraitRepo, traitRepo);

        // Act - Reactivate the trait (simulating bonus rebuilding)
        deathHandler.ReactivateResettableTraits(CharacterId.From(characterId));
        List<CharacterTrait> traitsAfterReactivation = charTraitRepo.GetByCharacterId(CharacterId.From(characterId));

        // Assert
        Assert.That(traitsAfterReactivation, Has.Count.EqualTo(1));
        Assert.That(traitsAfterReactivation[0].IsActive, Is.True, "Hero trait should be reactivated");
        Assert.That(traitsAfterReactivation[0].CustomData, Is.Null, "Custom data starts fresh");
    }

    [Test]
    public void VillainDeath_ByHeroHand_TriggersPermadeath()
    {
        // Arrange
        ITraitRepository traitRepo = InMemoryTraitRepository.Create();
        traitRepo.Add(new Trait
        {
            Tag = "villain",
            Name = "Villain",
            Description = "Evil character with permadeath risk",
            PointCost = 2,
            DeathBehavior = TraitDeathBehavior.Permadeath
        });

        ICharacterTraitRepository charTraitRepo = InMemoryCharacterTraitRepository.Create();
        Guid characterId = Guid.NewGuid();

        CharacterTrait villainTrait = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag("villain"),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true,
            IsActive = true
        };
        charTraitRepo.Add(villainTrait);

        TraitDeathHandler deathHandler = TraitDeathHandler.Create(charTraitRepo, traitRepo);

        // Act - Character dies by hero's hand
        bool permadeath = deathHandler.ProcessDeath(CharacterId.From(characterId), killedByHero: true);

        // Assert
        Assert.That(permadeath, Is.True, "Villain killed by Hero should trigger permadeath");
    }

    [Test]
    public void VillainDeath_ByNonHero_AllowsRespawn()
    {
        // Arrange
        ITraitRepository traitRepo = InMemoryTraitRepository.Create();
        traitRepo.Add(new Trait
        {
            Tag = "villain",
            Name = "Villain",
            Description = "Evil character with permadeath risk",
            PointCost = 2,
            DeathBehavior = TraitDeathBehavior.Permadeath
        });

        ICharacterTraitRepository charTraitRepo = InMemoryCharacterTraitRepository.Create();
        Guid characterId = Guid.NewGuid();

        CharacterTrait villainTrait = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag("villain"),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true,
            IsActive = true
        };
        charTraitRepo.Add(villainTrait);

        TraitDeathHandler deathHandler = TraitDeathHandler.Create(charTraitRepo, traitRepo);

        // Act - Character dies by non-hero (monster, trap, etc)
        bool permadeath = deathHandler.ProcessDeath(CharacterId.From(characterId), killedByHero: false);

        // Assert
        Assert.That(permadeath, Is.False, "Villain killed by non-Hero should allow respawn");
    }

    [Test]
    public void TraitWithResetBehavior_ClearsCustomData_OnDeath()
    {
        // Arrange
        ITraitRepository traitRepo = InMemoryTraitRepository.Create();
        traitRepo.Add(new Trait
        {
            Tag = "tracked_trait",
            Name = "Tracked Trait",
            Description = "Trait with custom tracking data",
            PointCost = 1,
            DeathBehavior = TraitDeathBehavior.ResetOnDeath
        });

        ICharacterTraitRepository charTraitRepo = InMemoryCharacterTraitRepository.Create();
        Guid characterId = Guid.NewGuid();

        CharacterTrait trackedTrait = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag("tracked_trait"),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true,
            IsActive = true,
            CustomData = "{\"someData\": \"important value\"}"
        };
        charTraitRepo.Add(trackedTrait);

        TraitDeathHandler deathHandler = TraitDeathHandler.Create(charTraitRepo, traitRepo);

        // Act - Character dies
        deathHandler.ProcessDeath(CharacterId.From(characterId));
        List<CharacterTrait> traitsAfterDeath = charTraitRepo.GetByCharacterId(CharacterId.From(characterId));

        // Assert
        Assert.That(traitsAfterDeath, Has.Count.EqualTo(1));
        Assert.That(traitsAfterDeath[0].CustomData, Is.Null, "CustomData should be cleared on death");
        Assert.That(traitsAfterDeath[0].IsActive, Is.False, "Trait should be deactivated");
    }

    #endregion

    #region Trait Categories and Types

    [Test]
    public void PositiveTrait_ShouldHave_PositivePointCost()
    {
        // Arrange
        Trait positiveTrait = new Trait
        {
            Tag = "brave",
            Name = "Brave",
            Description = "Fearless",
            PointCost = 1
        };

        // Assert
        Assert.That(positiveTrait.PointCost, Is.GreaterThan(0), "Positive traits cost points");
    }

    [Test]
    public void NegativeTrait_ShouldHave_NegativePointCost()
    {
        // Negative cost means it grants points
        // Arrange
        Trait negativeTrait = new Trait
        {
            Tag = "cowardly",
            Name = "Cowardly",
            Description = "Easily frightened",
            PointCost = -1
        };

        // Assert
        Assert.That(negativeTrait.PointCost, Is.LessThan(0), "Negative traits grant points");
    }

    [Test]
    public void TraitDefinition_ShouldLoad_FromJson()
    {
        // Arrange - Create a temporary directory structure with Traits subfolder
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string traitsDir = Path.Combine(tempDir, "Traits");
        Directory.CreateDirectory(traitsDir);

        string jsonContent = """
        {
            "Tag": "brave",
            "Name": "Brave",
            "Description": "Fearless in combat",
            "PointCost": 1,
            "RequiresUnlock": false,
            "DeathBehavior": 0,
            "Effects": [
                {
                    "EffectType": 1,
                    "Target": "Intimidate",
                    "Magnitude": 2
                }
            ],
            "AllowedRaces": [],
            "AllowedClasses": [],
            "ForbiddenRaces": [],
            "ForbiddenClasses": [],
            "ConflictingTraits": ["cowardly"],
            "PrerequisiteTraits": []
        }
        """;

        string jsonFile = Path.Combine(traitsDir, "brave.json");
        File.WriteAllText(jsonFile, jsonContent);

        // Set up environment and service
        string? originalPath = Environment.GetEnvironmentVariable("RESOURCE_PATH");
        Environment.SetEnvironmentVariable("RESOURCE_PATH", tempDir);

        ITraitRepository repo = InMemoryTraitRepository.Create();
        TraitDefinitionLoadingService service = new(repo);

        try
        {
            // Act
            service.Load();
            Trait? loadedTrait = repo.Get("brave");

            // Assert
            Assert.That(loadedTrait, Is.Not.Null, "Trait should be loaded");
            Assert.That(loadedTrait!.Tag, Is.EqualTo("brave"));
            Assert.That(loadedTrait.Name, Is.EqualTo("Brave"));
            Assert.That(loadedTrait.Description, Is.EqualTo("Fearless in combat"));
            Assert.That(loadedTrait.PointCost, Is.EqualTo(1));
            Assert.That(loadedTrait.RequiresUnlock, Is.False);
            Assert.That(loadedTrait.DeathBehavior, Is.EqualTo(TraitDeathBehavior.Persist));
            Assert.That(loadedTrait.Effects, Has.Count.EqualTo(1));
            Assert.That(loadedTrait.ConflictingTraits, Contains.Item("cowardly"));
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("RESOURCE_PATH", originalPath);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Test]
    public void TraitEffects_ShouldDefine_MechanicalBehavior()
    {
        // Effects like skill bonuses, attribute mods, death rules
        // Arrange
        Trait traitWithEffects = new Trait
        {
            Tag = "athletic",
            Name = "Athletic",
            Description = "Physically capable",
            PointCost = 1,
            Effects =
            [
                TraitEffect.SkillModifier("Athletics", 2),
                TraitEffect.AttributeModifier("Strength", 1)
            ]
        };

        // Assert - Verify effects define mechanical behavior
        Assert.That(traitWithEffects.Effects, Has.Count.EqualTo(2), "Should have two effects");

        TraitEffect skillEffect = traitWithEffects.Effects[0];
        Assert.That(skillEffect.EffectType, Is.EqualTo(TraitEffectType.SkillModifier));
        Assert.That(skillEffect.Target, Is.EqualTo("Athletics"));
        Assert.That(skillEffect.Magnitude, Is.EqualTo(2));

        TraitEffect attributeEffect = traitWithEffects.Effects[1];
        Assert.That(attributeEffect.EffectType, Is.EqualTo(TraitEffectType.AttributeModifier));
        Assert.That(attributeEffect.Target, Is.EqualTo("Strength"));
        Assert.That(attributeEffect.Magnitude, Is.EqualTo(1));
    }

    #endregion

    #region Trait Interactions and Special Cases

    [Test]
    public void MultipleNegativeTraits_CanStack_ToIncreasePointBudget()
    {
        // Arrange
        TraitBudget budget = TraitBudget.CreateDefault();

        // Act - Select two negative traits (drawbacks)
        TraitBudget afterFirst = budget.AfterSpending(-1);  // First negative trait
        TraitBudget afterSecond = afterFirst.AfterSpending(-1); // Second negative trait

        // Assert
        Assert.That(afterSecond.SpentPoints, Is.EqualTo(-2), "Two negative traits = -2 spent");
        Assert.That(afterSecond.AvailablePoints, Is.EqualTo(4), "2 base + 2 from negatives = 4 available");
    }

    [Test]
    public void ConflictingTraits_CannotBeBothSelected()
    {
        // Example: Can't be both "Brave" and "Cowardly"
        // Arrange
        Trait braveTrait = new Trait
        {
            Tag = "brave",
            Name = "Brave",
            Description = "Fearless",
            PointCost = 1,
            ConflictingTraits = ["cowardly"]
        };

        Trait cowardlyTrait = new Trait
        {
            Tag = "cowardly",
            Name = "Cowardly",
            Description = "Easily frightened",
            PointCost = -1, // Negative trait
            ConflictingTraits = ["brave"]
        };

        ICharacterInfo character = TestCharacterInfo.From("Human", "Fighter");
        TraitBudget budget = TraitBudget.CreateDefault();

        List<CharacterTrait> selectedTraits = [
            new CharacterTrait
            {
                Id = Guid.NewGuid(),
                CharacterId = CharacterId.From(Guid.NewGuid()),
                TraitTag = new TraitTag("brave"),
                DateAcquired = DateTime.UtcNow,
                IsConfirmed = false
            }
        ];

        // Act - Try to select conflicting trait
        bool canSelectCowardly = TraitSelectionValidator.CanSelect(cowardlyTrait, character, selectedTraits, budget);

        // Assert
        Assert.That(canSelectCowardly, Is.False, "Cannot select conflicting trait");
    }

    [Test]
    public void TraitUnlock_ViaDmEvent_ShouldPersist()
    {
        // Arrange
        ICharacterTraitRepository charTraitRepo = InMemoryCharacterTraitRepository.Create();
        Guid characterId = Guid.NewGuid();

        CharacterTrait unlockedTrait = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag("hero"),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = false,
            IsActive = false,
            IsUnlocked = true // Unlocked by DM event
        };

        // Act - DM unlocks the trait
        charTraitRepo.Add(unlockedTrait);

        // Simulate logout/login by retrieving persisted unlock
        List<CharacterTrait> traitsAfterLogin = charTraitRepo.GetByCharacterId(CharacterId.From(characterId));
        CharacterTrait? persistedUnlock = traitsAfterLogin.FirstOrDefault(t => t.TraitTag.Value == "hero");

        // Assert - Unlock should persist
        Assert.That(persistedUnlock, Is.Not.Null, "Unlock should be persisted");
        Assert.That(persistedUnlock!.IsUnlocked, Is.True, "Trait should remain unlocked");
        Assert.That(persistedUnlock.IsConfirmed, Is.False, "Unlock is not the same as selection");
        Assert.That(persistedUnlock.IsActive, Is.False, "Unlocked but not yet selected");
    }

    [Test]
    public void LegacyCharacter_ReceivesTwoFreePoints_OnFirstLogin()
    {
        // Arrange - Legacy character has no trait system records
        ICharacterTraitRepository charTraitRepo = InMemoryCharacterTraitRepository.Create();
        Guid legacyCharacterId = Guid.NewGuid();

        // Act - Check if character has any trait records
        List<CharacterTrait> existingTraits = charTraitRepo.GetByCharacterId(CharacterId.From(legacyCharacterId));
        bool isLegacyCharacter = existingTraits.Count == 0;

        // Legacy characters get the default budget on first login
        TraitBudget budget = TraitBudget.CreateDefault();

        // Assert - Legacy characters get the default 2 points
        Assert.That(isLegacyCharacter, Is.True, "Character should be identified as legacy");
        Assert.That(budget.BasePoints, Is.EqualTo(2), "Legacy characters receive 2 base points");
        Assert.That(budget.AvailablePoints, Is.EqualTo(2), "Legacy characters have 2 points available");
        Assert.That(budget.EarnedPoints, Is.EqualTo(0), "Legacy characters start with no earned points");
        Assert.That(budget.SpentPoints, Is.EqualTo(0), "Legacy characters start with no spent points");
    }

    #endregion

    #region Trait Effect Application

    [Test]
    public void ConfirmedTraitEffects_ApplyOnLogin()
    {
        // Arrange
        ITraitRepository traitRepo = InMemoryTraitRepository.Create();
        traitRepo.Add(new Trait
        {
            Tag = "skilled",
            Name = "Skilled",
            Description = "Bonus to Persuasion",
            PointCost = 1,
            Effects =
            [
                TraitEffect.SkillModifier("Persuasion", 2)
            ]
        });

        ICharacterTraitRepository charTraitRepo = InMemoryCharacterTraitRepository.Create();
        Guid characterId = Guid.NewGuid();

        CharacterTrait skilledTrait = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag("skilled"),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true,
            IsActive = true
        };
        charTraitRepo.Add(skilledTrait);

        TraitEffectApplicationService effectService = TraitEffectApplicationService.Create(charTraitRepo, traitRepo);

        // Act - Get effects to apply on login
        List<(string TraitTag, TraitEffect Effect)> effects = effectService.GetActiveEffects(CharacterId.From(characterId));

        // Assert
        Assert.That(effects, Has.Count.EqualTo(1), "Should have one effect");
        Assert.That(effects[0].TraitTag, Is.EqualTo("skilled"));
        Assert.That(effects[0].Effect.EffectType, Is.EqualTo(TraitEffectType.SkillModifier));
        Assert.That(effects[0].Effect.Target, Is.EqualTo("Persuasion"));
        Assert.That(effects[0].Effect.Magnitude, Is.EqualTo(2));
    }

    [Test]
    public void TraitEffects_CanBeReapplied_Safely()
    {
        // Remove old effects, reapply current traits
        // Arrange
        ITraitRepository traitRepo = InMemoryTraitRepository.Create();
        traitRepo.Add(new Trait
        {
            Tag = "strong",
            Name = "Strong",
            Description = "Bonus to Strength",
            PointCost = 1,
            Effects =
            [
                TraitEffect.AttributeModifier("Strength", 1)
            ]
        });

        ICharacterTraitRepository charTraitRepo = InMemoryCharacterTraitRepository.Create();
        Guid characterId = Guid.NewGuid();

        CharacterTrait strongTrait = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag("strong"),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true,
            IsActive = true
        };
        charTraitRepo.Add(strongTrait);

        TraitEffectApplicationService effectService = TraitEffectApplicationService.Create(charTraitRepo, traitRepo);

        // Act - Get effects multiple times (simulating reapplication)
        List<(string TraitTag, TraitEffect Effect)> effects1 = effectService.GetActiveEffects(CharacterId.From(characterId));
        List<(string TraitTag, TraitEffect Effect)> effects2 = effectService.GetActiveEffects(CharacterId.From(characterId));

        // Assert - Both calls return same effects (safe to reapply)
        Assert.That(effects1, Has.Count.EqualTo(1));
        Assert.That(effects2, Has.Count.EqualTo(1));
        Assert.That(effects1[0].Effect.EffectType, Is.EqualTo(effects2[0].Effect.EffectType));
        Assert.That(effects1[0].Effect.Magnitude, Is.EqualTo(effects2[0].Effect.Magnitude));
    }

    [Test]
    public void InactiveTraitEffects_DoNotApply()
    {
        // Hero trait inactive after death until bonuses rebuild
        // Arrange
        ITraitRepository traitRepo = InMemoryTraitRepository.Create();
        traitRepo.Add(new Trait
        {
            Tag = "hero",
            Name = "Hero",
            Description = "Heroic bonuses",
            PointCost = 2,
            DeathBehavior = TraitDeathBehavior.ResetOnDeath,
            Effects =
            [
                TraitEffect.AttributeModifier("Strength", 1),
                TraitEffect.AttributeModifier("Constitution", 1)
            ]
        });

        ICharacterTraitRepository charTraitRepo = InMemoryCharacterTraitRepository.Create();
        Guid characterId = Guid.NewGuid();

        CharacterTrait heroTrait = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag("hero"),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true,
            IsActive = false // Inactive after death
        };
        charTraitRepo.Add(heroTrait);

        TraitEffectApplicationService effectService = TraitEffectApplicationService.Create(charTraitRepo, traitRepo);

        // Act - Get effects when trait is inactive
        List<(string TraitTag, TraitEffect Effect)> effects = effectService.GetActiveEffects(CharacterId.From(characterId));

        // Assert - No effects should be applied when trait is inactive
        Assert.That(effects, Is.Empty, "Inactive traits should not apply effects");
    }

    #endregion
}

using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.IndustryAndCraft;

[TestFixture]
public class CharacterKnowledgeServiceTests
{
    private Mock<ICharacterKnowledgeRepository> _mockCharacterRepo = null!;
    private Mock<IKnowledgeDefinitionRepository> _mockKnowledgeRepo = null!;
    private CharacterKnowledgeService _service = null!;
    private Guid _characterId;

    [SetUp]
    public void SetUp()
    {
        _mockCharacterRepo = new Mock<ICharacterKnowledgeRepository>();
        _mockKnowledgeRepo = new Mock<IKnowledgeDefinitionRepository>();
        _service = new CharacterKnowledgeService(_mockCharacterRepo.Object, _mockKnowledgeRepo.Object);
        _characterId = Guid.NewGuid();
    }

    [Test]
    public async Task CanLearnKnowledgeAsync_ReturnsFalse_WhenKnowledgeDoesNotExist()
    {
        // Arrange
        KnowledgeKey knowledge = KnowledgeKey.From("NONEXISTENT");
        _mockKnowledgeRepo.Setup(r => r.FindByKeyAsync(knowledge, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeDefinition?)null);

        // Act
        bool result = await _service.CanLearnKnowledgeAsync(_characterId, knowledge);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CanLearnKnowledgeAsync_ReturnsFalse_WhenAlreadyKnown()
    {
        // Arrange
        KnowledgeKey knowledge = KnowledgeKey.From("BLACKSMITHING");
        KnowledgeDefinition definition = CreateKnowledgeDefinition(knowledge);

        _mockKnowledgeRepo.Setup(r => r.FindByKeyAsync(knowledge, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);
        _mockCharacterRepo.Setup(r => r.GetSetAsync(_characterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey> { knowledge });

        // Act
        bool result = await _service.CanLearnKnowledgeAsync(_characterId, knowledge);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CanLearnKnowledgeAsync_ReturnsFalse_WhenPrerequisitesNotMet()
    {
        // Arrange
        KnowledgeKey prereq = KnowledgeKey.From("BASIC_CRAFTING");
        KnowledgeKey knowledge = KnowledgeKey.From("ADVANCED_SMITHING");

        KnowledgeDefinition definition = CreateKnowledgeDefinition(knowledge);
        definition.AddPrerequisite(prereq);

        _mockKnowledgeRepo.Setup(r => r.FindByKeyAsync(knowledge, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);
        _mockCharacterRepo.Setup(r => r.GetSetAsync(_characterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey>()); // No knowledge

        // Act
        bool result = await _service.CanLearnKnowledgeAsync(_characterId, knowledge);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CanLearnKnowledgeAsync_ReturnsTrue_WhenCanLearn()
    {
        // Arrange
        KnowledgeKey prereq = KnowledgeKey.From("BASIC_CRAFTING");
        KnowledgeKey knowledge = KnowledgeKey.From("ADVANCED_SMITHING");

        KnowledgeDefinition definition = CreateKnowledgeDefinition(knowledge);
        definition.AddPrerequisite(prereq);

        _mockKnowledgeRepo.Setup(r => r.FindByKeyAsync(knowledge, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);
        _mockCharacterRepo.Setup(r => r.GetSetAsync(_characterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey> { prereq }); // Has prerequisite

        // Act
        bool result = await _service.CanLearnKnowledgeAsync(_characterId, knowledge);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task GetAvailableKnowledgeAsync_FiltersCorrectly()
    {
        // Arrange
        KnowledgeKey known = KnowledgeKey.From("KNOWN_SKILL");
        KnowledgeKey available = KnowledgeKey.From("AVAILABLE_SKILL");
        KnowledgeKey locked = KnowledgeKey.From("LOCKED_SKILL");
        KnowledgeKey prereq = KnowledgeKey.From("PREREQUISITE");

        List<KnowledgeDefinition> allKnowledge =
        [
            CreateKnowledgeDefinition(known),
            CreateKnowledgeDefinition(available),
            CreateKnowledgeDefinition(locked, prerequisite: prereq),
            CreateKnowledgeDefinition(prereq)
        ];

        _mockKnowledgeRepo.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allKnowledge);
        _mockCharacterRepo.Setup(r => r.GetSetAsync(_characterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey> { known }); // Only knows 'known'

        // Act
        IReadOnlyList<KnowledgeDefinition> result = await _service.GetAvailableKnowledgeAsync(_characterId);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2)); // available and prereq
        Assert.That(result.Any(k => k.Key == available), Is.True);
        Assert.That(result.Any(k => k.Key == prereq), Is.True);
        Assert.That(result.Any(k => k.Key == known), Is.False); // Already known
        Assert.That(result.Any(k => k.Key == locked), Is.False); // Prerequisites aren't met
    }


    [Test]
    public async Task GetAvailableKnowledgeAsync_FiltersByCategory()
    {
        // Arrange
        string category = "COMBAT";
        KnowledgeDefinition combatSkill = CreateKnowledgeDefinition(KnowledgeKey.From("SWORD_FIGHTING"), category);
        KnowledgeDefinition craftingSkill = CreateKnowledgeDefinition(KnowledgeKey.From("BLACKSMITHING"), "CRAFTING");

        _mockKnowledgeRepo.Setup(r => r.FindByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeDefinition> { combatSkill });
        _mockCharacterRepo.Setup(r => r.GetSetAsync(_characterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey>());

        // Act
        IReadOnlyList<KnowledgeDefinition> result = await _service.GetAvailableKnowledgeAsync(_characterId, category);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Key, Is.EqualTo(KnowledgeKey.From("SWORD_FIGHTING")));
    }

    [Test]
    public async Task TeachKnowledgeAsync_ReturnsTrue_WhenSuccessful()
    {
        // Arrange
        KnowledgeKey knowledge = KnowledgeKey.From("NEW_SKILL");
        KnowledgeDefinition definition = CreateKnowledgeDefinition(knowledge);

        _mockKnowledgeRepo.Setup(r => r.FindByKeyAsync(knowledge, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);
        _mockCharacterRepo.Setup(r => r.GetSetAsync(_characterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey>());

        // Act
        bool result = await _service.TeachKnowledgeAsync(_characterId, knowledge);

        // Assert
        Assert.That(result, Is.True);
        _mockCharacterRepo.Verify(r => r.GrantAsync(_characterId, knowledge, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task TeachKnowledgeAsync_ReturnsFalse_WhenCannotLearn()
    {
        // Arrange
        KnowledgeKey knowledge = KnowledgeKey.From("LOCKED_SKILL");
        _mockKnowledgeRepo.Setup(r => r.FindByKeyAsync(knowledge, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeDefinition?)null);

        // Act
        bool result = await _service.TeachKnowledgeAsync(_characterId, knowledge);

        // Assert
        Assert.That(result, Is.False);
        _mockCharacterRepo.Verify(r => r.GrantAsync(It.IsAny<Guid>(), It.IsAny<KnowledgeKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetKnowledgeByCategoryAsync_GroupsCorrectly()
    {
        // Arrange
        KnowledgeKey combat1 = KnowledgeKey.From("SWORD_FIGHTING");
        KnowledgeKey combat2 = KnowledgeKey.From("ARCHERY");
        KnowledgeKey crafting = KnowledgeKey.From("BLACKSMITHING");

        List<KnowledgeDefinition> knownDefinitions =
        [
            CreateKnowledgeDefinition(combat1, "COMBAT"),
            CreateKnowledgeDefinition(combat2, "COMBAT"),
            CreateKnowledgeDefinition(crafting, "CRAFTING")
        ];

        _mockCharacterRepo.Setup(r => r.GetSetAsync(_characterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey> { combat1, combat2, crafting });

        foreach (KnowledgeDefinition def in knownDefinitions)
        {
            _mockKnowledgeRepo.Setup(r => r.FindByKeyAsync(def.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(def);
        }

        // Act
        IReadOnlyDictionary<string, IReadOnlyList<KnowledgeDefinition>> result =
            await _service.GetKnowledgeByCategoryAsync(_characterId);

        // Assert
        Assert.That(result.Keys.Count, Is.EqualTo(2));
        Assert.That(result["COMBAT"].Count, Is.EqualTo(2));
        Assert.That(result["CRAFTING"].Count, Is.EqualTo(1));
    }

    private static KnowledgeDefinition CreateKnowledgeDefinition(KnowledgeKey key, string? category = null, KnowledgeKey? prerequisite = null)
    {
        KnowledgeDefinition def = KnowledgeDefinition.Create(
            key,
            key.Value.Replace("_", " "),
            $"Description for {key.Value}",
            category);

        if (prerequisite.HasValue)
            def.AddPrerequisite(prerequisite.Value);

        return def;
    }
}

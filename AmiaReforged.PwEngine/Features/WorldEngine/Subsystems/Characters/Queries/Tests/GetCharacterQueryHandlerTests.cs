using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Anvil.API;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Tests;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Queries.Tests;

/// <summary>
/// Focused tests for <see cref="GetCharacterQueryHandler"/>. Uses the in-memory
/// repository double; requires no live NWN objects or database.
/// </summary>
[TestFixture]
public class GetCharacterQueryHandlerTests
{
    private InMemoryCharacterRepository _repository = null!;
    private GetCharacterQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryCharacterRepository();
        _handler = new GetCharacterQueryHandler(_repository);
    }

    [Test]
    public async Task HandleAsync_KnownCharacter_ReturnsCharacter()
    {
        // Arrange
        ICharacter character = new InMemoryCharacter();
        _repository.Add(character);
        CharacterId id = character.GetId();

        // Act
        ICharacter? result = await _handler.HandleAsync(new GetCharacterQuery(id));

        // Assert
        Assert.That(result, Is.SameAs(character));
    }

    [Test]
    public async Task HandleAsync_MissingCharacter_ReturnsNull()
    {
        // Act
        ICharacter? result = await _handler.HandleAsync(new GetCharacterQuery(CharacterId.New()));

        // Assert
        Assert.That(result, Is.Null);
    }

    private sealed class InMemoryCharacter : ICharacter
    {
        private readonly CharacterId _id = CharacterId.New();

        public CharacterId GetId() => _id;
        public List<SkillData> GetSkills() => new();
        public int GetKnowledgePoints() => 0;
        public void AddKnowledgePoints(int points) { }
        public void SubtractKnowledgePoints(int points) { }
        public List<Knowledge> AllKnowledge() => new();
        public LearningResult Learn(string knowledgeTag) => LearningResult.DoesNotExist;
        public bool CanLearn(string knowledgeTag) => false;
        public List<KnowledgeHarvestEffect> KnowledgeEffectsForResource(string definitionTag, ResourceType resourceType) => new();
        public void InvalidateEffectCache() { }
        public List<CraftingModifier> CraftingModifiersForRecipe(string recipeId, string industryTag) => new();
        public bool HasUnlockedInteraction(string interactionTag) => false;
        public KnowledgeProgression GetProgression() => default;
        public void AddItem(ItemDto item) { }
        public List<ItemSnapshot> GetInventory() => new();
        public Dictionary<EquipmentSlots, ItemSnapshot?> GetEquipment() => new();
        public void JoinIndustry(string industryTag) { }
        public List<IndustryMembership> AllIndustryMemberships() => new();
        public RankUpResult RankUp(string industryTag) => default;
    }
}

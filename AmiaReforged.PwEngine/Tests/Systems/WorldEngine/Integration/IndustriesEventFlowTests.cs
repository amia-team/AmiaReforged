using AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Industries.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Helpers;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Integration;

/// <summary>
/// Integration tests for Industries event flows.
/// Verifies that events are published and handled correctly across the subsystem.
/// </summary>
[TestFixture]
public class IndustriesEventFlowTests
{
    private InMemoryEventBus _eventBus = null!;
    private IndustryMembershipService _membershipService = null!;
    private IIndustryRepository _industryRepository = null!;
    private ICharacterKnowledgeRepository _knowledgeRepository = null!;
    private Guid _characterId;
    private IndustryTag _industryTag;

    [SetUp]
    public void SetUp()
    {
        _characterId = Guid.NewGuid();
        _industryTag = new IndustryTag("test_industry");

        // Set up repositories
        _industryRepository = InMemoryIndustryRepository.Create();
        _knowledgeRepository = new InMemoryCharacterKnowledgeRepository();
        var membershipRepository = new InMemoryIndustryMembershipRepository();
        var characterRepository = RuntimeCharacterRepository.Create();

        // Set up event bus
        _eventBus = new InMemoryEventBus();

        // Create test industry
        var testIndustry = new Industry
        {
            Tag = _industryTag.Value,
            Name = "Test Industry",
            Knowledge = new List<Knowledge>
            {
                new Knowledge
                {
                    Tag = "test_knowledge",
                    Name = "Test Knowledge",
                    Description = "Test",
                    Level = ProficiencyLevel.Novice,
                    PointCost = 1
                }
            }
        };
        _industryRepository.Add(testIndustry);

        // Create test character with knowledge points
        var testCharacter = new TestCharacter(
            new Dictionary<Anvil.API.EquipmentSlots, AmiaReforged.PwEngine.Features.WorldEngine.Items.ItemData.ItemSnapshot>(),
            new List<SkillData>(),
            CharacterId.From(_characterId),
            _knowledgeRepository,
            null!,
            new List<AmiaReforged.PwEngine.Features.WorldEngine.Items.ItemData.ItemSnapshot>(),
            999 // Lots of knowledge points
        );
        characterRepository.Add(testCharacter);

        // Create service
        _membershipService = new IndustryMembershipService(
            membershipRepository,
            _industryRepository,
            characterRepository,
            _knowledgeRepository,
            _eventBus
        );
    }

    [Test]
    public void AddMembership_ShouldPublish_MemberJoinedIndustryEvent()
    {
        // Arrange
        var membership = new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(_characterId),
            IndustryTag = _industryTag,
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = new List<CharacterKnowledge>()
        };

        // Act
        _membershipService.AddMembership(membership);

        // Assert - Verify event was published
        var events = _eventBus.PublishedEvents;
        Assert.That(events, Has.Count.EqualTo(1), "Should publish exactly one event");

        var evt = events.OfType<MemberJoinedIndustryEvent>().FirstOrDefault();
        Assert.That(evt, Is.Not.Null, "Should publish MemberJoinedIndustryEvent");
        Assert.That(evt!.MemberId.Value, Is.EqualTo(_characterId), "Event should contain correct character ID");
        Assert.That(evt.IndustryTag, Is.EqualTo(_industryTag), "Event should contain correct industry tag");
        Assert.That(evt.InitialLevel, Is.EqualTo(ProficiencyLevel.Novice), "Event should contain correct initial level");
    }

    [Test]
    public void LearnKnowledge_ShouldPublish_RecipeLearnedEvent()
    {
        // Arrange - First add membership
        var membership = new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(_characterId),
            IndustryTag = _industryTag,
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = new List<CharacterKnowledge>()
        };
        _membershipService.AddMembership(membership);
        _eventBus.ClearPublishedEvents(); // Clear the join event

        // Act - Learn knowledge
        var result = _membershipService.LearnKnowledge(membership, "test_knowledge");

        // Assert - Verify success
        Assert.That(result, Is.EqualTo(LearningResult.Success), "Learning should succeed");

        // Assert - Verify event was published
        var events = _eventBus.PublishedEvents;
        Assert.That(events, Has.Count.EqualTo(1), "Should publish exactly one event");

        var evt = events.OfType<RecipeLearnedEvent>().FirstOrDefault();
        Assert.That(evt, Is.Not.Null, "Should publish RecipeLearnedEvent");
        Assert.That(evt!.LearnerId.Value, Is.EqualTo(_characterId), "Event should contain correct learner ID");
        Assert.That(evt.IndustryTag, Is.EqualTo(_industryTag), "Event should contain correct industry tag");
        Assert.That(evt.KnowledgeTag, Is.EqualTo("test_knowledge"), "Event should contain correct knowledge tag");
        Assert.That(evt.PointCost, Is.EqualTo(1), "Event should contain correct point cost");
    }

    [Test]
    public void RankUp_ShouldPublish_ProficiencyGainedEvent()
    {
        // Arrange - Add membership and learn required knowledge
        var membership = new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(_characterId),
            IndustryTag = _industryTag,
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = new List<CharacterKnowledge>()
        };
        _membershipService.AddMembership(membership);
        _membershipService.LearnKnowledge(membership, "test_knowledge");
        _eventBus.ClearPublishedEvents(); // Clear previous events

        // Act - Rank up
        var result = _membershipService.RankUp(membership);

        // Assert - Verify success
        Assert.That(result, Is.EqualTo(RankUpResult.Success), "Rank up should succeed");

        // Assert - Verify event was published
        var events = _eventBus.PublishedEvents;
        Assert.That(events, Has.Count.EqualTo(1), "Should publish exactly one event");

        var evt = events.OfType<ProficiencyGainedEvent>().FirstOrDefault();
        Assert.That(evt, Is.Not.Null, "Should publish ProficiencyGainedEvent");
        Assert.That(evt!.MemberId.Value, Is.EqualTo(_characterId), "Event should contain correct member ID");
        Assert.That(evt.IndustryTag, Is.EqualTo(_industryTag), "Event should contain correct industry tag");
        Assert.That(evt.NewLevel, Is.EqualTo(ProficiencyLevel.Apprentice), "Event should contain new level");
        Assert.That(evt.PreviousLevel, Is.EqualTo(ProficiencyLevel.Novice), "Event should contain previous level");
    }

    [Test]
    public void MultipleOperations_ShouldPublish_EventsInOrder()
    {
        // Arrange
        var membership = new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(_characterId),
            IndustryTag = _industryTag,
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = new List<CharacterKnowledge>()
        };

        // Act - Perform multiple operations
        _membershipService.AddMembership(membership);
        _membershipService.LearnKnowledge(membership, "test_knowledge");
        _membershipService.RankUp(membership);

        // Assert - Verify all events published in order
        var events = _eventBus.PublishedEvents;
        Assert.That(events, Has.Count.EqualTo(3), "Should publish three events");

        Assert.That(events[0], Is.TypeOf<MemberJoinedIndustryEvent>(), "First event should be MemberJoined");
        Assert.That(events[1], Is.TypeOf<RecipeLearnedEvent>(), "Second event should be RecipeLearned");
        Assert.That(events[2], Is.TypeOf<ProficiencyGainedEvent>(), "Third event should be ProficiencyGained");
    }
}


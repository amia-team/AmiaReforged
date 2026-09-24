using System;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Tests;

[TestFixture]
public class LearnKnowledgeCommandTests
{
    private InMemoryIndustryRepository _industryRepository = null!;
    private InMemoryIndustryMembershipRepository _membershipRepository = null!;
    private InMemoryCharacterKnowledgeRepository _knowledgeRepository = null!;
    private InMemoryEventBus _eventBus = null!;
    private ICharacterRepository _characterRepository = null!;
    private IndustryMembershipService _membershipService = null!;
    private LearnKnowledgeHandler _handler = null!;
    private CharacterId _testCharacterId;
    private TestCharacter _testCharacter = null!;

    [SetUp]
    public void SetUp()
    {
        _industryRepository = new InMemoryIndustryRepository();
        _membershipRepository = new InMemoryIndustryMembershipRepository();
        _knowledgeRepository = new InMemoryCharacterKnowledgeRepository();
        _eventBus = new InMemoryEventBus();
        _characterRepository = RuntimeCharacterRepository.Create();

        _industryRepository.Add(new Industry
        {
            Tag = "blacksmithing",
            Name = "Blacksmithing",
            Knowledge =
            [
                new Knowledge
                {
                    Tag = "basic_forging",
                    Name = "Basic Forging",
                    Description = "Basic forging techniques",
                    Level = ProficiencyLevel.Novice,
                    PointCost = 1
                }
            ]
        });

        _testCharacterId = CharacterId.From(Guid.NewGuid());
        _testCharacter = new TestCharacter(
            new Dictionary<EquipmentSlots, ItemSnapshot>(),
            [],
            _testCharacterId,
            _knowledgeRepository,
            null!,
            inventory: [],
            knowledgePoints: 999);
        _characterRepository.Add(_testCharacter);

        _membershipService = new IndustryMembershipService(
            _membershipRepository, _industryRepository, _characterRepository, _knowledgeRepository, _eventBus);
        _handler = new LearnKnowledgeHandler(_membershipService);
    }

    private void EnrollTestCharacter()
    {
        _membershipService.AddMembership(new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing"),
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = []
        });

        // Exclude the MemberJoinedIndustryEvent published by enrollment from event assertions.
        _eventBus.ClearPublishedEvents();
    }

    [Test]
    public async Task LearnKnowledge_Success_PersistsAndPublishesEventOnce()
    {
        EnrollTestCharacter();

        CommandResult result = await _handler.HandleAsync(new LearnKnowledgeCommand
        {
            CharacterId = _testCharacterId,
            KnowledgeTag = "basic_forging"
        });

        Assert.That(result.Success, Is.True);
        Assert.That(_knowledgeRepository.GetKnowledgeForIndustry("blacksmithing", _testCharacterId.Value),
            Has.Count.EqualTo(1));
        Assert.That(_eventBus.PublishedEvents.OfType<RecipeLearnedEvent>().ToList(), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task LearnKnowledge_AlreadyKnown_DoesNotCreateAnotherRecord()
    {
        EnrollTestCharacter();
        await _handler.HandleAsync(new LearnKnowledgeCommand
        {
            CharacterId = _testCharacterId,
            KnowledgeTag = "basic_forging"
        });

        CommandResult second = await _handler.HandleAsync(new LearnKnowledgeCommand
        {
            CharacterId = _testCharacterId,
            KnowledgeTag = "basic_forging"
        });

        Assert.That(second.Success, Is.False);
        Assert.That(_knowledgeRepository.GetKnowledgeForIndustry("blacksmithing", _testCharacterId.Value),
            Has.Count.EqualTo(1));
        Assert.That(_eventBus.PublishedEvents.OfType<RecipeLearnedEvent>().ToList(), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task LearnKnowledge_UnknownKnowledge_Fails()
    {
        EnrollTestCharacter();

        CommandResult result = await _handler.HandleAsync(new LearnKnowledgeCommand
        {
            CharacterId = _testCharacterId,
            KnowledgeTag = "nonexistent"
        });

        Assert.That(result.Success, Is.False);
        Assert.That(_knowledgeRepository.GetKnowledgeForIndustry("blacksmithing", _testCharacterId.Value),
            Has.Count.Zero);
        Assert.That(_eventBus.PublishedEvents, Has.Count.Zero);
    }

    [Test]
    public async Task LearnKnowledge_ReturnsOutcomeUsableByCallers()
    {
        EnrollTestCharacter();

        CommandResult result = await _handler.HandleAsync(new LearnKnowledgeCommand
        {
            CharacterId = _testCharacterId,
            KnowledgeTag = "basic_forging"
        });

        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.ContainsKey("result"), Is.True);
        Assert.That(result.Data!["result"], Is.EqualTo(LearningResult.Success));
    }
}

using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Services;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Helpers;
using Anvil.API;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine;

[TestFixture]
public class RuntimeCharacterTests
{
    private const string IndustryWithKnowledge = "industry_with_knowledge";
    private const string Noviceknowledge = "NoviceKnowledge";
    private const string Grandmasterknowledge = "GrandMasterKnowledge";
    private const string Masterknowledge = "MasterKnowledge";
    private const string Expertknowledge = "ExpertKnowledge";
    private const string Journeymanknowledge = "JourneymanKnowledge";
    private const string Journeymanknowledge2 = "JourneymanKnowledge2";
    private const string Apprenticeknowledge = "ApprenticeKnowledge";
    private const string TestIndustry = "test";
    private ICharacterRepository _characters = null!;
    private IndustryMembershipService _membershipService = null!;
    private ICharacterStatService _characterStatService = null!;

    private readonly Guid _characterId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _characters = RuntimeCharacterRepository.Create();

        IIndustryRepository industryRepository = InMemoryIndustryRepository.Create();

        Industry i = new()
        {
            Tag = IndustryWithKnowledge,
            Name = "industry with knowledge",
            Knowledge =
            [
                new Knowledge
                {
                    Tag = Noviceknowledge,
                    Name = "Novice",
                    Description = string.Empty,
                    Level = ProficiencyLevel.Novice
                },
                new Knowledge
                {
                    Tag = Apprenticeknowledge,
                    Name = "Apprentice",
                    Description = string.Empty,
                    Level = ProficiencyLevel.Apprentice
                },
                new Knowledge
                {
                    Tag = Journeymanknowledge,
                    Name = "Journeyman",
                    Description = string.Empty,
                    Level = ProficiencyLevel.Journeyman
                },
                new Knowledge
                {
                    Tag = Journeymanknowledge2,
                    Name = "Journeyman",
                    Description = string.Empty,
                    Level = ProficiencyLevel.Journeyman,
                    PointCost = 2
                },
                new Knowledge
                {
                    Tag = Expertknowledge,
                    Name = "Expert",
                    Description = string.Empty,
                    Level = ProficiencyLevel.Expert
                },
                new Knowledge
                {
                    Tag = Masterknowledge,
                    Name = "Master",
                    Description = string.Empty,
                    Level = ProficiencyLevel.Master
                },
                new Knowledge
                {
                    Tag = Grandmasterknowledge,
                    Name = "GrandMaster",
                    Description = string.Empty,
                    Level = ProficiencyLevel.Grandmaster
                },
            ]
        };

        industryRepository.Add(i);

        Industry noKnowledge = new()
        {
            Tag = TestIndustry,
            Name = "Test",
            Knowledge = []
        };

        industryRepository.Add(noKnowledge);

        _membershipService = new IndustryMembershipService(InMemoryIndustryMembershipRepository.Create(),
            industryRepository, _characters, InMemoryCharacterKnowledgeRepository.Create(), new InMemoryEventBus());

        _characterStatService = new CharacterStatService(InMemoryCharacterStatRepository.Create());
    }

    [Test]
    public void Should_Return_Id()
    {
        RuntimeCharacter character = new(CharacterId.From(_characterId), Mock.Of<IInventoryPort>(), Mock.Of<ICharacterSheetPort>(),
            _membershipService, _characterStatService);

        Assert.That(character.GetId(), Is.EqualTo(CharacterId.From(_characterId)), "ID should have been injected.");
    }

    /// <summary>
    /// Despite it appearing that we are just testing a Mock here, we are actually testing that a contract for fetching skills exists.
    /// </summary>
    [Test]
    public void Should_Return_Skills()
    {
        Mock<ICharacterSheetPort> mockCharacterSheet = new();
        List<SkillData> expectedSkills =
        [
            new(Skill.Taunt, 1),
            new(Skill.Listen, 1)
        ];

        mockCharacterSheet.Setup(x => x.GetSkills()).Returns(expectedSkills);

        RuntimeCharacter character = new(CharacterId.From(_characterId), Mock.Of<IInventoryPort>(), mockCharacterSheet.Object,
            _membershipService, _characterStatService);

        List<SkillData> actualSkills = character.GetSkills();

        Assert.That(actualSkills, Is.EqualTo(expectedSkills));
    }


    [Test]
    public void Should_Return_Knowledge()
    {
        RuntimeCharacter character = new(CharacterId.From(_characterId), Mock.Of<IInventoryPort>(), Mock.Of<ICharacterSheetPort>(),
            _membershipService, _characterStatService);

        _characters.Add(character);

        character.JoinIndustry(IndustryWithKnowledge);

        LearningResult result = character.Learn(Noviceknowledge);

        Assert.That(result, Is.EqualTo(LearningResult.Success));

        List<Knowledge> knowledge = character.AllKnowledge();

        Assert.That(knowledge, Is.Not.Empty);
        Assert.That(knowledge.Any(k => k.Tag == Noviceknowledge), Is.True);
    }

    [Test]
    public void Should_Rank_Up_In_Industry()
    {
        RuntimeCharacter character = new(CharacterId.From(_characterId), Mock.Of<IInventoryPort>(), Mock.Of<ICharacterSheetPort>(),
            _membershipService, _characterStatService);

        _characters.Add(character);

        character.JoinIndustry(IndustryWithKnowledge);

        LearningResult result = character.Learn(Noviceknowledge);

        Assert.That(result, Is.EqualTo(LearningResult.Success));

        RankUpResult rankUpResult = character.RankUp(IndustryWithKnowledge);

        Assert.That(rankUpResult, Is.EqualTo(RankUpResult.Success));
    }

    [Test]
    public void Should_Get_All_Memberships()
    {
        RuntimeCharacter character = new(CharacterId.From(_characterId), Mock.Of<IInventoryPort>(), Mock.Of<ICharacterSheetPort>(),
            _membershipService, _characterStatService);

        _characters.Add(character);

        character.JoinIndustry(IndustryWithKnowledge);
        character.JoinIndustry(TestIndustry);

        List<IndustryMembership> memberships = character.AllIndustryMemberships();

        Assert.That(memberships, Is.Not.Empty);
        Assert.That(memberships.Count, Is.EqualTo(2));
        Assert.That(memberships.Any(m => m.IndustryTag == new IndustryTag(IndustryWithKnowledge)), Is.True);
        Assert.That(memberships.Any(m => m.IndustryTag == new IndustryTag(TestIndustry)), Is.True);
    }

    /// <summary>
    /// Another case where we want to make sure that all that is provided is a contract to get the character's inventory contents in a NWN agnostic context
    /// </summary>
    [Test]
    public void Should_Get_Inventory()
    {
        Mock<IInventoryPort> mockInventory = new();
        List<ItemSnapshot> expectedInventory =
        [
            new("test_item", "Test Item", "Test Item", IPQuality.Average, [], JobSystemItemType.None, 1, null)
        ];
        mockInventory.Setup(x => x.GetInventory()).Returns(expectedInventory);

        RuntimeCharacter character = new(CharacterId.From(_characterId), mockInventory.Object, Mock.Of<ICharacterSheetPort>(),
            _membershipService, _characterStatService);

        _characters.Add(character);

        List<ItemSnapshot> inventory = character.GetInventory();

        Assert.That(inventory, Is.EqualTo(expectedInventory));
    }

    /// <summary>
    ///  Another case where we want to make sure that all that is provided is a contract to get the character's equipped items in a NWN agnostic context
    /// </summary>
    [Test]
    public void Should_Get_Equipment()
    {
        Mock<IInventoryPort> mockInventory = new();
        Dictionary<EquipmentSlots, ItemSnapshot> expectedEquipment = new()
        {
            { EquipmentSlots.Boots, new ItemSnapshot("test_boots", "Test Item", "Test Item", IPQuality.Average, [], JobSystemItemType.None, 1, null) }
        };
        mockInventory.Setup(x => x.GetEquipment()).Returns(expectedEquipment);

        RuntimeCharacter character = new(CharacterId.From(_characterId), mockInventory.Object, Mock.Of<ICharacterSheetPort>(),
            _membershipService, _characterStatService);

        _characters.Add(character);

        Dictionary<EquipmentSlots, ItemSnapshot> equipment = character.GetEquipment();

        Assert.That(equipment, Is.EqualTo(expectedEquipment));
    }

    [Test]
    public void Should_Get_Knowledge_Points()
    {
        RuntimeCharacter character = new(CharacterId.From(_characterId), Mock.Of<IInventoryPort>(), Mock.Of<ICharacterSheetPort>(),
            _membershipService, _characterStatService);

        _characters.Add(character);

        character.AddKnowledgePoints(2);

        int knowledge = character.GetKnowledgePoints();

        Assert.That(knowledge, Is.EqualTo(2));

        character.AddKnowledgePoints(2);

        knowledge = character.GetKnowledgePoints();
        Assert.That(knowledge, Is.EqualTo(4));
    }

    [Test]
    public void Should_Deduct_Knowledge_Points_After_Learning()
    {
        RuntimeCharacter character = new(CharacterId.From(_characterId), Mock.Of<IInventoryPort>(), Mock.Of<ICharacterSheetPort>(),
            _membershipService, _characterStatService);

        _characters.Add(character);

        character.JoinIndustry(IndustryWithKnowledge);

        character.Learn(Noviceknowledge);
        character.RankUp(IndustryWithKnowledge);

        character.Learn(Apprenticeknowledge);
        character.RankUp(IndustryWithKnowledge);

        character.AddKnowledgePoints(2);

        character.Learn(Journeymanknowledge2);

        int knowledgePoints = character.GetKnowledgePoints();

        Assert.That(knowledgePoints, Is.EqualTo(0));
    }

    [Test]
    public void Should_See_If_Character_Can_Not_Learn_Knowledge()
    {
        RuntimeCharacter character = new(CharacterId.From(_characterId), Mock.Of<IInventoryPort>(), Mock.Of<ICharacterSheetPort>(),
            _membershipService, _characterStatService);

        _characters.Add(character);

        bool result = character.CanLearn(Noviceknowledge);

        Assert.That(result, Is.False, "Should be false because the Character is NOT part of the proper Industry");
    }

    [Test]
    public void Should_See_If_Character_Can_Learn_Knowledge()
    {
        RuntimeCharacter character = new(CharacterId.From(_characterId), Mock.Of<IInventoryPort>(), Mock.Of<ICharacterSheetPort>(),
            _membershipService, _characterStatService);

        _characters.Add(character);
        character.JoinIndustry(IndustryWithKnowledge);
        bool result = character.CanLearn(Noviceknowledge);

        Assert.That(result, Is.True, "Should be true because the Character is part of the proper Industry");
    }
}

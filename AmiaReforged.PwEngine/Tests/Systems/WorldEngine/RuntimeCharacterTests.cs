using AmiaReforged.PwEngine.Systems.WorldEngine;
using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;
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
    private const string Apprenticeknowledge = "ApprenticeKnowledge";
    private ICharacterRepository _characters = null!;
    private IndustryMembershipService _membershipService = null!;

    private readonly Guid _characterId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _characters = InMemoryCharacterRepository.Create();

        IIndustryRepository industryRepository = InMemoryIndustryRepository.Create();

        Industry i = new Industry
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

        _membershipService = new IndustryMembershipService(InMemoryIndustryMembershipRepository.Create(),
            industryRepository, _characters, InMemoryCharacterKnowledgeRepository.Create());
    }

    [Test]
    public void Should_Return_Id()
    {
        RuntimeCharacter character = new(_characterId, Mock.Of<IInventoryPort>(), Mock.Of<ICharacterSheetPort>(),
            _membershipService);

        Assert.That(character.GetId(), Is.EqualTo(_characterId), "ID should have been injected.");
    }

    /// <summary>
    /// Despite it appearing that we are just testing a Mock here, we are actually testing that a contract for fetching skills exists.
    /// </summary>
    [Test]
    public void Should_Return_Skills()
    {
        Mock<ICharacterSheetPort> mockCharacterSheet = new Mock<ICharacterSheetPort>();
        List<SkillData> expectedSkills =
        [
            new(Skill.Taunt, 1),
            new(Skill.Listen, 1)
        ];

        mockCharacterSheet.Setup(x => x.GetSkills()).Returns(expectedSkills);

        RuntimeCharacter character = new(_characterId, Mock.Of<IInventoryPort>(), mockCharacterSheet.Object,
            _membershipService);

        List<SkillData> actualSkills = character.GetSkills();

        Assert.That(actualSkills, Is.EqualTo(expectedSkills));
    }


    [Test]
    public void Should_Return_Knowledge()
    {
        RuntimeCharacter character = new(_characterId, Mock.Of<IInventoryPort>(), Mock.Of<ICharacterSheetPort>(),
            _membershipService);

        _characters.Add(character);

        character.JoinIndustry(IndustryWithKnowledge);

        LearningResult result = character.Learn(Noviceknowledge);

        Assert.That(result, Is.EqualTo(LearningResult.Success));

        List<Knowledge> knowledge = character.AllKnowledge();

        Assert.That(knowledge, Is.Not.Empty);
        Assert.That(knowledge.Any(k => k.Tag == Noviceknowledge), Is.True);
    }
}

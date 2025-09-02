using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Helpers;
using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine;

[TestFixture]
public class IndustryMembershipTests
{
    private const string NoviceKnowledge = "novice";
    private const string NoviceKnowledge2 = "Novice2";
    private const string NoviceKnowledge3 = "Novice3";
    private const string NoviceKnowledge4 = "Novice4";
    private const string ApprenticeKnowledge = "Apprentice1";
    private IndustryMembershipService _sut = null!;
    private readonly Guid _characterGuid = Guid.NewGuid();
    private readonly ICharacterRepository _characterRepository = InMemoryCharacterRepository.Create();
    private ICharacterKnowledgeRepository _characterKnowledgeRepository = null!;

    [SetUp]
    public void SetUp()
    {
        Industry testIndustry = new()
        {
            Tag = "test_industry",
            Name = "test industry",
            Knowledge =
            [
                new Knowledge
                {
                    Tag = NoviceKnowledge,
                    Name = "Novice Knowledge",
                    Level = ProficiencyLevel.Novice,
                    PointCost = 1,
                    Description = string.Empty
                },
                new Knowledge
                {
                    Tag = NoviceKnowledge2,
                    Name = "Novice Knowledge 2",
                    Level = ProficiencyLevel.Novice,
                    PointCost = 1,
                    Description = string.Empty
                },
                new Knowledge
                {
                    Tag = NoviceKnowledge3,
                    Name = "Novice Knowledge 3",
                    Level = ProficiencyLevel.Novice,
                    PointCost = 1,
                    Description = string.Empty
                },
                new Knowledge
                {
                    Tag = NoviceKnowledge4,
                    Name = "Novice Knowledge 4",
                    Level = ProficiencyLevel.Novice,
                    PointCost = 1,
                    Description = string.Empty
                },
                new Knowledge
                {
                    Tag = ApprenticeKnowledge,
                    Name = ApprenticeKnowledge,
                    Level = ProficiencyLevel.Apprentice,
                    PointCost = 2,
                    Description = string.Empty
                }
            ]
        };

        IIndustryRepository testIndustryRepo = InMemoryIndustryRepository.Create();
        testIndustryRepo.Add(testIndustry);

        _characterKnowledgeRepository = InMemoryCharacterKnowledgeRepository.Create();
        _sut = new IndustryMembershipService(new InMemoryIndustryMembershipRepository(), testIndustryRepo, _characterRepository,
            _characterKnowledgeRepository);
        TestCharacter c = new(new Dictionary<EquipmentSlots, ItemSnapshot>(), [], _characterGuid, _characterKnowledgeRepository, _sut, inventory: [], 999);
        _characterRepository.Add(c);
    }

    [Test]
    public void Should_Add_Industry_Membership()
    {
        IndustryMembership membership = new()
        {
            IndustryTag = "test_industry",
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            Id = Guid.NewGuid(),
            CharacterId = _characterGuid
        };

        _sut.AddMembership(membership);

        Assert.That(_sut.GetMemberships(_characterGuid), Is.Not.Empty);
        Assert.That(_sut.GetMemberships(_characterGuid), Does.Contain(membership));
    }

    [Test]
    public void Should_Not_Add_Industry_Membership_With_Invalid_Industry_Tag()
    {
        IndustryMembership membership = new()
        {
            IndustryTag = "invalid_industry",
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            Id = Guid.NewGuid(),
            CharacterId = _characterGuid
        };

        _sut.AddMembership(membership);

        Assert.That(_sut.GetMemberships(_characterGuid), Does.Not.Contain(membership));
    }

    [Test]
    public void Should_Not_Add_Industry_Membership_With_Invalid_Character_Id()
    {
        IndustryMembership membership = new()
        {
            IndustryTag = "test_industry",
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            Id = Guid.NewGuid(),
            CharacterId = Guid.NewGuid()
        };

        _sut.AddMembership(membership);

        Assert.That(_sut.GetMemberships(_characterGuid), Does.Not.Contain(membership));
    }

    [Test]
    public void Should_Not_Update_Membership_Rank_With_Insufficient_Knowledge()
    {
        IndustryMembership membership = new()
        {
            IndustryTag = "test_industry",
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            Id = Guid.NewGuid(),
            CharacterId = _characterGuid
        };

        _sut.AddMembership(membership);
        RankUpResult success = _sut.RankUp(membership);

        IndustryMembership actual = _sut.GetMemberships(_characterGuid).First();

        Assert.That(success, Is.EqualTo(RankUpResult.InsufficientKnowledge),
            "The character's industry rank should not have been incremented");
        Assert.That(actual.Level, Is.EqualTo(ProficiencyLevel.Novice),
            "The character's industry rank should not have been incremented");
    }

    [Test]
    public void Should_Not_Learn_Knowledge_Without_Enough_Points()
    {
        TestCharacter character = new(new Dictionary<EquipmentSlots, ItemSnapshot>(), [], Guid.NewGuid(), _characterKnowledgeRepository, _sut, []);

        _characterRepository.Add(character);

        IndustryMembership membership = new()
        {
            IndustryTag = "test_industry",
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            Id = Guid.NewGuid(),
            CharacterId = character.GetId()
        };

        _sut.AddMembership(membership);

        LearningResult result = _sut.LearnKnowledge(character.GetId(), NoviceKnowledge);

        _characterRepository.Delete(character);

        Assert.That(result, Is.EqualTo(LearningResult.NotEnoughPoints));
    }

    [Test]
    public void Should_Not_Be_Able_To_Learn_Higher_Tier_Knowledge()
    {
        IndustryMembership membership = new()
        {
            IndustryTag = "test_industry",
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            Id = Guid.NewGuid(),
            CharacterId = _characterGuid
        };

        _sut.AddMembership(membership);

        LearningResult result = _sut.LearnKnowledge(_characterGuid, ApprenticeKnowledge);

        Assert.That(result, Is.EqualTo(LearningResult.InsufficientRank));
    }

    [Test]
    public void Should_Learn_Knowledge_With_Points_And_Rank()
    {
        IndustryMembership membership = new()
        {
            IndustryTag = "test_industry",
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            Id = Guid.NewGuid(),
            CharacterId = _characterGuid
        };

        _sut.AddMembership(membership);

        LearningResult noviceResult1 = _sut.LearnKnowledge(membership, NoviceKnowledge);

        LearningResult noviceResult2 = _sut.LearnKnowledge(membership, NoviceKnowledge2);

        _sut.RankUp(membership);

        LearningResult apprenticeResult = _sut.LearnKnowledge(membership, ApprenticeKnowledge);
        Assert.That(noviceResult1, Is.EqualTo(LearningResult.Success));
        Assert.That(noviceResult2, Is.EqualTo(LearningResult.Success));
        Assert.That(apprenticeResult, Is.EqualTo(LearningResult.Success));

    }

    [Test]
    public void Should_Update_Membership_Rank_With_Sufficient_Knowledge()
    {
        IndustryMembership membership = new()
        {
            IndustryTag = "test_industry",
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            Id = Guid.NewGuid(),
            CharacterId = _characterGuid
        };

        _sut.AddMembership(membership);

        LearningResult result = _sut.LearnKnowledge(membership, NoviceKnowledge2);
        Assert.That(result, Is.EqualTo(LearningResult.Success), "The character should have learned the knowledge");


        LearningResult result2 = _sut.LearnKnowledge(membership, NoviceKnowledge);
        Assert.That(result2, Is.EqualTo(LearningResult.Success), "The character should have learned the knowledge");


        RankUpResult success = _sut.RankUp(membership);
        IndustryMembership actual = _sut.GetMemberships(_characterGuid).First();

        Assert.That(success, Is.EqualTo(RankUpResult.Success),
            "The character's industry rank should have been incremented");
        Assert.That(actual.Level, Is.EqualTo(ProficiencyLevel.Apprentice),
            "The character's industry rank should have been incremented");
    }
}

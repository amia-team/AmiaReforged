using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Tests.Systems.WorldEngine.Helpers;
using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Tests;

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
    private readonly ICharacterRepository _characterRepository = RuntimeCharacterRepository.Create();
    private ICharacterKnowledgeRepository _characterKnowledgeRepository = null!;
    private IEventBus _eventBus = null!;

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
        _eventBus = new InMemoryEventBus();
        _sut = new IndustryMembershipService(new InMemoryIndustryMembershipRepository(), testIndustryRepo, _characterRepository,
            _characterKnowledgeRepository, _eventBus);
        TestCharacter c = new(new Dictionary<EquipmentSlots, ItemSnapshot>(), [], CharacterId.From(_characterGuid), _characterKnowledgeRepository, _sut, inventory: [], 999);
        _characterRepository.Add(c);
    }

    [Test]
    public void Should_Add_Industry_Membership()
    {
        IndustryMembership membership = new()
        {
            IndustryTag = new IndustryTag("test_industry"),
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(_characterGuid)
        };

        _sut.AddMembership(membership);

        Assert.That(_sut.GetMemberships(CharacterId.From(_characterGuid)), Is.Not.Empty);
        Assert.That(_sut.GetMemberships(CharacterId.From(_characterGuid)), Does.Contain(membership));
    }

    [Test]
    public void Should_Not_Add_Industry_Membership_With_Invalid_Industry_Tag()
    {
        IndustryMembership membership = new()
        {
            IndustryTag = new IndustryTag("invalid_industry"),
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(_characterGuid)
        };

        _sut.AddMembership(membership);

        Assert.That(_sut.GetMemberships(CharacterId.From(_characterGuid)), Does.Not.Contain(membership));
    }

    [Test]
    public void Should_Not_Add_Industry_Membership_With_Invalid_Character_Id()
    {
        IndustryMembership membership = new()
        {
            IndustryTag = new IndustryTag("test_industry"),
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(Guid.NewGuid())
        };

        _sut.AddMembership(membership);

        Assert.That(_sut.GetMemberships(CharacterId.From(_characterGuid)), Does.Not.Contain(membership));
    }

    [Test]
    public void Should_Not_Update_Membership_Rank_With_Insufficient_Knowledge()
    {
        IndustryMembership membership = new()
        {
            IndustryTag = new IndustryTag("test_industry"),
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(_characterGuid)
        };

        _sut.AddMembership(membership);
        RankUpResult success = _sut.RankUp(membership);

        IndustryMembership actual = _sut.GetMemberships(CharacterId.From(_characterGuid)).First();

        Assert.That(success, Is.EqualTo(RankUpResult.InsufficientKnowledge),
            "The character's industry rank should not have been incremented");
        Assert.That(actual.Level, Is.EqualTo(ProficiencyLevel.Novice),
            "The character's industry rank should not have been incremented");
    }

    [Test]
    public void Should_Not_Learn_Knowledge_Without_Enough_Points()
    {
        TestCharacter character = new(new Dictionary<EquipmentSlots, ItemSnapshot>(), [], CharacterId.From(Guid.NewGuid()), _characterKnowledgeRepository, _sut, []);

        _characterRepository.Add(character);

        IndustryMembership membership = new()
        {
            IndustryTag = new IndustryTag("test_industry"),
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
            IndustryTag = new IndustryTag("test_industry"),
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(_characterGuid)
        };

        _sut.AddMembership(membership);

        LearningResult result = _sut.LearnKnowledge(CharacterId.From(_characterGuid), ApprenticeKnowledge);

        Assert.That(result, Is.EqualTo(LearningResult.InsufficientRank));
    }

    [Test]
    public void Should_Learn_Knowledge_With_Points_And_Rank()
    {
        IndustryMembership membership = new()
        {
            IndustryTag = new IndustryTag("test_industry"),
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(_characterGuid)
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
            IndustryTag = new IndustryTag("test_industry"),
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(_characterGuid)
        };

        _sut.AddMembership(membership);

        LearningResult result = _sut.LearnKnowledge(membership, NoviceKnowledge2);
        Assert.That(result, Is.EqualTo(LearningResult.Success), "Since the character has the required rank, the character should have learned the knowledge");


        LearningResult result2 = _sut.LearnKnowledge(membership, NoviceKnowledge);
        Assert.That(result2, Is.EqualTo(LearningResult.Success), "Since the character has the required rank, the character should have learned the knowledge");


        RankUpResult success = _sut.RankUp(membership);
        IndustryMembership actual = _sut.GetMemberships(CharacterId.From(_characterGuid)).First();

        Assert.That(success, Is.EqualTo(RankUpResult.Success),
            "The character's industry rank should have been incremented");
        Assert.That(actual.Level, Is.EqualTo(ProficiencyLevel.Apprentice),
            "The character's industry rank should have been incremented");
    }
}

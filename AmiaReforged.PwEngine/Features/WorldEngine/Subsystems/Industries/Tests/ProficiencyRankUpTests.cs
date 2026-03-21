using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Tests;

/// <summary>
/// Integration tests for the proficiency XP rank-up flow:
/// proficiency XP → tier ceiling → learn knowledge → rank up → unlock next tier.
/// </summary>
[TestFixture]
public class ProficiencyRankUpTests
{
    private IndustryMembershipService _membershipService = null!;
    private ProficiencyProgressionService _proficiencyService = null!;
    private ICharacterKnowledgeRepository _knowledgeRepo = null!;
    private IIndustryMembershipRepository _membershipRepo = null!;
    private readonly Guid _characterGuid = Guid.NewGuid();
    private readonly ICharacterRepository _characterRepository = RuntimeCharacterRepository.Create();

    [SetUp]
    public void SetUp()
    {
        // Create an industry with enough knowledge items per tier
        List<Knowledge> knowledge = [];
        for (int i = 1; i <= 6; i++)
        {
            knowledge.Add(new Knowledge
            {
                Tag = $"novice_{i}",
                Name = $"Novice Knowledge {i}",
                Level = ProficiencyLevel.Novice,
                PointCost = 1,
                Description = string.Empty
            });
        }

        for (int i = 1; i <= 11; i++)
        {
            knowledge.Add(new Knowledge
            {
                Tag = $"apprentice_{i}",
                Name = $"Apprentice Knowledge {i}",
                Level = ProficiencyLevel.Apprentice,
                PointCost = 1,
                Description = string.Empty
            });
        }

        Industry testIndustry = new()
        {
            Tag = "test_smithing",
            Name = "Test Smithing",
            Knowledge = knowledge
        };

        IIndustryRepository industryRepo = InMemoryIndustryRepository.Create();
        industryRepo.Add(testIndustry);

        _knowledgeRepo = InMemoryCharacterKnowledgeRepository.Create();
        _membershipRepo = new InMemoryIndustryMembershipRepository();
        IEventBus eventBus = new InMemoryEventBus();

        _membershipService = new IndustryMembershipService(
            _membershipRepo, industryRepo, _characterRepository, _knowledgeRepo, eventBus);
        _proficiencyService = new ProficiencyProgressionService();

        TestCharacter c = new(
            new Dictionary<EquipmentSlots, ItemSnapshot>(), [],
            CharacterId.From(_characterGuid), _knowledgeRepo, _membershipService, inventory: [], 999);
        _characterRepository.Add(c);
    }

    private IndustryMembership CreateAndAddMembership(
        ProficiencyLevel level = ProficiencyLevel.Novice,
        int xpLevel = 1)
    {
        IndustryMembership membership = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(_characterGuid),
            IndustryTag = new IndustryTag("test_smithing"),
            Level = level,
            ProficiencyXpLevel = xpLevel,
            ProficiencyXp = 0,
            CharacterKnowledge = []
        };
        _membershipService.AddMembership(membership);
        return membership;
    }

    // ==================== RankUp requires proficiency level at ceiling ====================

    [Test]
    public void RankUp_WithoutReachingTierCeiling_ReturnsInsufficientProficiencyLevel()
    {
        IndustryMembership membership = CreateAndAddMembership(
            level: ProficiencyLevel.Novice, xpLevel: 20);

        RankUpResult result = _membershipService.RankUp(membership);

        Assert.That(result, Is.EqualTo(RankUpResult.InsufficientProficiencyLevel));
        Assert.That(membership.Level, Is.EqualTo(ProficiencyLevel.Novice));
    }

    [Test]
    public void RankUp_AtCeiling_WithInsufficientKnowledge_ReturnsInsufficientKnowledge()
    {
        IndustryMembership membership = CreateAndAddMembership(
            level: ProficiencyLevel.Novice, xpLevel: 25);

        // Learn only 2 knowledge items (need 5 for Novice -> Apprentice)
        _membershipService.LearnKnowledge(membership, "novice_1");
        _membershipService.LearnKnowledge(membership, "novice_2");

        RankUpResult result = _membershipService.RankUp(membership);

        Assert.That(result, Is.EqualTo(RankUpResult.InsufficientKnowledge));
        Assert.That(membership.Level, Is.EqualTo(ProficiencyLevel.Novice));
    }

    [Test]
    public void RankUp_AtCeiling_WithSufficientKnowledge_Succeeds()
    {
        IndustryMembership membership = CreateAndAddMembership(
            level: ProficiencyLevel.Novice, xpLevel: 25);

        // Learn 5 Novice knowledge items (meets the 5 KP requirement)
        for (int i = 1; i <= 5; i++)
        {
            _membershipService.LearnKnowledge(membership, $"novice_{i}");
        }

        RankUpResult result = _membershipService.RankUp(membership);

        Assert.That(result, Is.EqualTo(RankUpResult.Success));
        Assert.That(membership.Level, Is.EqualTo(ProficiencyLevel.Apprentice));
    }

    // ==================== Full flow: XP → ceiling → rank up → continue ====================

    [Test]
    public void FullFlow_XpToCeiling_RankUp_ContinueGaining()
    {
        IndustryMembership membership = CreateAndAddMembership(
            level: ProficiencyLevel.Novice, xpLevel: 1);

        // Award enough XP to reach the Novice ceiling (level 25)
        int massiveXp = 500_000;
        ProficiencyXpResult xpResult = _proficiencyService.AwardProficiencyXp(membership, massiveXp);

        Assert.That(xpResult.Success, Is.True);
        Assert.That(membership.ProficiencyXpLevel, Is.EqualTo(25), "Should cap at Novice ceiling");
        Assert.That(xpResult.IsAtTierCeiling, Is.True);

        // Can't gain more XP at ceiling
        Assert.That(_proficiencyService.CanGainXp(membership), Is.False);

        // Learn required knowledge and rank up
        for (int i = 1; i <= 5; i++)
        {
            _membershipService.LearnKnowledge(membership, $"novice_{i}");
        }

        RankUpResult rankResult = _membershipService.RankUp(membership);
        Assert.That(rankResult, Is.EqualTo(RankUpResult.Success));
        Assert.That(membership.Level, Is.EqualTo(ProficiencyLevel.Apprentice));

        // Now can gain XP again (but still at level 25 — need to move to 26)
        // The rank-up doesn't change ProficiencyXpLevel, just the enum Level.
        // CanGainXp should now return true since the Apprentice ceiling is 50.
        Assert.That(_proficiencyService.CanGainXp(membership), Is.True);

        // Award XP and see it advance past 25
        int costFor25 = ProficiencyXpCurve.XpForLevel(25);
        ProficiencyXpResult postRankResult = _proficiencyService.AwardProficiencyXp(membership, costFor25);
        Assert.That(postRankResult.Success, Is.True);
        Assert.That(membership.ProficiencyXpLevel, Is.EqualTo(26));
    }

    // ==================== RankUpRequirements ====================

    [Test]
    public void RankUpRequirements_NoviceRequires5KP()
    {
        Assert.That(RankUpRequirements.KnowledgePointsRequired(ProficiencyLevel.Novice), Is.EqualTo(5));
    }

    [Test]
    public void RankUpRequirements_ApprenticeRequires10KP()
    {
        Assert.That(RankUpRequirements.KnowledgePointsRequired(ProficiencyLevel.Apprentice), Is.EqualTo(10));
    }

    [Test]
    public void RankUpRequirements_JourneymanRequires15KP()
    {
        Assert.That(RankUpRequirements.KnowledgePointsRequired(ProficiencyLevel.Journeyman), Is.EqualTo(15));
    }

    [Test]
    public void RankUpRequirements_ExpertRequires20KP()
    {
        Assert.That(RankUpRequirements.KnowledgePointsRequired(ProficiencyLevel.Expert), Is.EqualTo(20));
    }

    [Test]
    public void RankUpRequirements_MasterRequires25KP()
    {
        Assert.That(RankUpRequirements.KnowledgePointsRequired(ProficiencyLevel.Master), Is.EqualTo(25));
    }

    [Test]
    public void RankUpRequirements_LaymanReturns0()
    {
        Assert.That(RankUpRequirements.KnowledgePointsRequired(ProficiencyLevel.Layman), Is.EqualTo(0));
    }

    [Test]
    public void RankUpRequirements_GrandmasterReturns0()
    {
        Assert.That(RankUpRequirements.KnowledgePointsRequired(ProficiencyLevel.Grandmaster), Is.EqualTo(0));
    }

    [Test]
    public void RankUpRequirements_RequiredLevelForRankUp_MatchesTierCeiling()
    {
        Assert.That(RankUpRequirements.RequiredLevelForRankUp(ProficiencyLevel.Novice), Is.EqualTo(25));
        Assert.That(RankUpRequirements.RequiredLevelForRankUp(ProficiencyLevel.Apprentice), Is.EqualTo(50));
        Assert.That(RankUpRequirements.RequiredLevelForRankUp(ProficiencyLevel.Journeyman), Is.EqualTo(75));
        Assert.That(RankUpRequirements.RequiredLevelForRankUp(ProficiencyLevel.Expert), Is.EqualTo(100));
        Assert.That(RankUpRequirements.RequiredLevelForRankUp(ProficiencyLevel.Master), Is.EqualTo(124));
    }
}

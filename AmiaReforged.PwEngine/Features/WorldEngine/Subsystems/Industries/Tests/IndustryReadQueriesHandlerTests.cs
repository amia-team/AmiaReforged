using System;
using System.Collections.Generic;
using System.Linq;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Services;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Tests;

/// <summary>
/// Behavioural coverage for the independent membership and knowledge read projections added to the
/// industry CQRS layer. Each case mirrors the behaviour of the corresponding
/// <see cref="IIndustryMembershipService"/> read that RuntimeCharacter now dispatches through a
/// query, so regressions in missing-membership, knowledge-filtering and learning-eligibility
/// results are caught at the query boundary itself.
/// </summary>
[TestFixture]
public class IndustryReadQueriesHandlerTests
{
    private const string TestIndustry = "test_industry";
    private const string OtherIndustry = "other_industry";
    private const string NoviceA = "novice_a";
    private const string NoviceB = "novice_b";
    private const string NoviceC = "novice_c";

    private CharacterId _characterId;
    private ICharacterKnowledgeRepository _knowledgeRepository = null!;
    private InMemoryIndustryMembershipRepository _membershipRepository = null!;
    private IndustryMembershipService _membershipService = null!;
    private ICharacterRepository _characterRepository = null!;

    [SetUp]
    public void SetUp()
    {
        _characterId = new CharacterId(Guid.NewGuid());
        _knowledgeRepository = InMemoryCharacterKnowledgeRepository.Create();
        _membershipRepository = new InMemoryIndustryMembershipRepository();
        _characterRepository = RuntimeCharacterRepository.Create();

        IIndustryRepository industryRepository = InMemoryIndustryRepository.Create();
        industryRepository.Add(BuildIndustry("test_industry", new[] { NoviceA, NoviceB }));
        industryRepository.Add(BuildIndustry("other_industry", new[] { NoviceC }));

        ICharacterStatService statService = new CharacterStatService(InMemoryCharacterStatRepository.Create());

        _membershipService = new IndustryMembershipService(
            _membershipRepository, industryRepository, _characterRepository, _knowledgeRepository, new InMemoryEventBus());

        // A character with plenty of knowledge points so eligibility is governed by membership/rank
        // alone, not by an unrelated point balance.
        var equipment = new Dictionary<EquipmentSlots, ItemSnapshot>();
        TestCharacter character = new(equipment, [], _characterId, _knowledgeRepository, _membershipService, inventory: [], knowledgePoints: 999);
        _characterRepository.Add(character);
    }

    private static Industry BuildIndustry(string tag, string[] knowledgeTags)
    {
        return new Industry
        {
            Tag = tag,
            Name = tag,
            Knowledge = knowledgeTags.Select(tagValue => new Knowledge
            {
                Tag = tagValue,
                Name = tagValue,
                Description = string.Empty,
                Level = ProficiencyLevel.Novice,
                PointCost = 1
            }).ToList()
        };
    }

    private void AddKnowledge(string industryTag, string knowledgeTag)
    {
        _knowledgeRepository.Add(new CharacterKnowledge
        {
            Id = Guid.NewGuid(),
            IndustryTag = industryTag,
            Definition = new Knowledge
            {
                Tag = knowledgeTag,
                Name = knowledgeTag,
                Description = string.Empty,
                Level = ProficiencyLevel.Novice,
                PointCost = 1
            },
            CharacterId = _characterId.Value
        });
    }

    private void Enroll(string industryTag)
    {
        _membershipService.AddMembership(new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = _characterId,
            IndustryTag = new IndustryTag(industryTag),
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = []
        });
    }

    [Test]
    public void GetKnowledgeDefinitions_ReturnsLearnedDefinitionsForCharacter()
    {
        AddKnowledge(TestIndustry, NoviceA);
        AddKnowledge(OtherIndustry, NoviceC);

        List<Knowledge> result = new GetKnowledgeDefinitionsHandler(_knowledgeRepository)
            .HandleAsync(new GetKnowledgeDefinitionsQuery { CharacterId = _characterId }).GetAwaiter().GetResult();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(k => k.Tag), Contains.Item(NoviceA));
        Assert.That(result.Select(k => k.Tag), Contains.Item(NoviceC));
    }

    [Test]
    public void GetKnowledgeDefinitions_ReturnsEmptyForMissingCharacter()
    {
        List<Knowledge> result = new GetKnowledgeDefinitionsHandler(_knowledgeRepository)
            .HandleAsync(new GetKnowledgeDefinitionsQuery { CharacterId = _characterId }).GetAwaiter().GetResult();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void CanLearnKnowledge_ReturnsTrueWhenEligible()
    {
        Enroll(TestIndustry);

        bool result = new CanLearnKnowledgeHandler(_membershipService)
            .HandleAsync(new CanLearnKnowledgeQuery
            {
                CharacterId = _characterId,
                KnowledgeTag = NoviceA
            }).GetAwaiter().GetResult();

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanLearnKnowledge_ReturnsFalseWhenMissingMembership()
    {
        // Knowledge exists in an industry, but the character is not a member: eligibility is false.
        bool result = new CanLearnKnowledgeHandler(_membershipService)
            .HandleAsync(new CanLearnKnowledgeQuery
            {
                CharacterId = _characterId,
                KnowledgeTag = NoviceA
            }).GetAwaiter().GetResult();

        Assert.That(result, Is.False);
    }

    [Test]
    public void CanLearnKnowledge_ReturnsFalseWhenKnowledgeUnknown()
    {
        Enroll(TestIndustry);

        bool result = new CanLearnKnowledgeHandler(_membershipService)
            .HandleAsync(new CanLearnKnowledgeQuery
            {
                CharacterId = _characterId,
                KnowledgeTag = "does_not_exist"
            }).GetAwaiter().GetResult();

        Assert.That(result, Is.False);
    }
}

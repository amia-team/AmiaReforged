using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AmiaReforged.Core.Models;
using AmiaReforged.System.Services;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AmiaReforged.Core.Test.Integration;

public class FactionRelationServiceTest : IDisposable
{
    private ITestOutputHelper _outputHelper;
    private readonly FactionRelationService _factionRelationService;
    private readonly FactionService _factionService;

    public FactionRelationServiceTest(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _factionService = new FactionService(new CharacterService());
        _factionRelationService = new FactionRelationService(_factionService);
    }

    [Fact]
    public async void ShouldAddFactionRelation()
    {
        Faction factionOne = new()
        {
            Name = "Faction One",
            Description = "Faction One Description",
            Members = new List<Guid>()
        };
        Faction factionTwo = new()
        {
            Name = "Faction Two",
            Description = "Faction Two Description"
        };

        await _factionService.AddFaction(factionOne);
        await _factionService.AddFaction(factionTwo);

        FactionRelation relation = new()
        {
            Id = Guid.NewGuid(),
            FactionName = factionOne.Name,
            TargetFactionName = factionTwo.Name,
            Relation = -50
        };

        await _factionRelationService.AddFactionRelation(relation);

        IEnumerable<FactionRelation> actual = await _factionRelationService.GetRelationsForFaction(factionOne);

        actual.ToList().ToList().Should().NotBeEmpty("because we added a relation").And
            .Contain(relation, "because we added a relation");
    }

    [Fact]
    public async void ShouldNotAddRelationForNonExistentFaction()
    {
        Faction nonExistentFaction = new()
        {
            Name = "Non Existent Faction",
            Description = "Non Existent Faction Description"
        };

        FactionRelation relation = new()
        {
            Id = Guid.NewGuid(),
            FactionName = nonExistentFaction.Name,
            TargetFactionName = "Non Existent Faction",
            Relation = -50
        };

        await _factionRelationService.AddFactionRelation(relation);


        IEnumerable<FactionRelation> actual = await _factionRelationService.GetRelationsForFaction(nonExistentFaction);

        actual.Should().BeEmpty("because we added a relation for a non existent faction");
    }

    [Fact]
    public async void ShouldNotAddRelationForNonExistentTargetFaction()
    {
        Faction factionOne = new()
        {
            Name = "Faction Exists",
            Description = "Faction does exist",
            Members = new List<Guid>()
        };

        await _factionService.AddFaction(factionOne);

        FactionRelation relation = new()
        {
            Id = Guid.NewGuid(),
            FactionName = factionOne.Name,
            TargetFactionName = "Non Existent Faction",
            Relation = -50
        };

        await _factionRelationService.AddFactionRelation(relation);

        IEnumerable<FactionRelation> actual = await _factionRelationService.GetRelationsForFaction(factionOne);

        actual.Should().BeEmpty("because we added a relation for a non existent target faction");
    }

    [Fact]
    public async Task ShouldUpdateFactionRelationship()
    {
        Faction factionOne = new()
        {
            Name = "A Faction To Update",
            Description = "A Faction To Update",
            Members = new List<Guid>()
        };

        Faction factionTwo = new()
        {
            Name = "Faction Doesn't Have A Relationship With Faction One",
            Description = "Faction Two Description",
            Members = new List<Guid>()
        };

        await _factionService.AddFaction(factionOne);
        await _factionService.AddFaction(factionTwo);

        FactionRelation relation = new()
        {
            Id = Guid.NewGuid(),
            FactionName = factionOne.Name,
            TargetFactionName = factionTwo.Name,
            Relation = -50
        };

        await _factionRelationService.AddFactionRelation(relation);

        relation.Relation = 50;

        await _factionRelationService.UpdateFactionRelation(relation);

        IEnumerable<FactionRelation> actual = await _factionRelationService.GetRelationsForFaction(factionOne);

        IEnumerable<FactionRelation> factionRelations = actual as FactionRelation[] ?? actual.ToArray();
        factionRelations.Should().Contain(relation, "because we updated the relation");
        factionRelations.First().Relation.Should().Be(relation.Relation);
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
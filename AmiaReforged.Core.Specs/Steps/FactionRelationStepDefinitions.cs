﻿using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using BoDi;
using FluentAssertions;

namespace SpecFlowProject1.Steps;

[Binding]
public class FactionRelationStepDefinitions
{
    private readonly IObjectContainer _objectContainer;

    public FactionRelationStepDefinitions(IObjectContainer objectContainer)
    {
        _objectContainer = objectContainer;
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        _objectContainer.RegisterTypeAs<FactionRelationService, FactionRelationService>();
    }

    [When(@"I check the relation between the pair of Factions")]
    public async Task WhenICheckTheRelationBetweenThePairOfFactions()
    {
        FactionRelationService factionRelationService = _objectContainer.Resolve<FactionRelationService>();
        Tuple<Faction, Faction> factions = _objectContainer.Resolve<Tuple<Faction, Faction>>("FactionPair");

        FactionRelation? relation =
            await factionRelationService.GetFactionRelationAsync(factions.Item1, factions.Item2);

        _objectContainer.RegisterInstanceAs(relation, "FactionPairRelation");
    }

    [Then(@"the relation should be (.*)")]
    public void ThenTheRelationIs(int expectedValue)
    {
        FactionRelation? relation = _objectContainer.Resolve<FactionRelation?>("FactionPairRelation");
        relation.Should().NotBeNull();
        relation?.Relation.Should().Be(expectedValue);
    }

    [Then(@"the Faction should have a neutral relationship with every other Faction")]
    public async Task ThenTheFactionShouldHaveANeutralRelationshipWithEveryOtherFaction()
    {
        FactionRelationService factionRelationService = _objectContainer.Resolve<FactionRelationService>();
        FactionService factionService = _objectContainer.Resolve<FactionService>();
        Faction faction = _objectContainer.Resolve<Faction>("Faction");

        IEnumerable<Faction> factions = await factionService.GetAllFactions();
        foreach (Faction otherFaction in factions)
        {
            if (otherFaction.Name == faction.Name)
                continue;

            FactionRelation? relation = await factionRelationService.GetFactionRelationAsync(otherFaction, faction);
            relation.Should().NotBeNull();
            relation?.Relation.Should().Be(0);
        }
    }

    [When(@"I set the relation of Faction A with Faction B to (.*)")]
    public async Task WhenISetTheRelationOfFactionAForFactionBTo(int value)
    {
        FactionRelationService factionRelationService = _objectContainer.Resolve<FactionRelationService>();
        Tuple<Faction, Faction> factions = _objectContainer.Resolve<Tuple<Faction, Faction>>("FactionPair");

        FactionRelation? relation =
            await factionRelationService.GetFactionRelationAsync(factions.Item1, factions.Item2);
        relation.Should().NotBeNull();
        relation!.Relation = value;

        await factionRelationService.UpdateFactionRelation(relation);
    }

    [Then(@"the relation of Faction B for Faction A should be (.*)")]
    public async Task ThenTheRelationOfFactionBForFactionAShouldBe(int expected)
    {
        FactionRelationService factionRelationService = _objectContainer.Resolve<FactionRelationService>();
        Tuple<Faction, Faction> factions = _objectContainer.Resolve<Tuple<Faction, Faction>>("FactionPair");

        FactionRelation? relation =
            await factionRelationService.GetFactionRelationAsync(factions.Item2, factions.Item1);

        relation.Should().NotBeNull();
        relation!.Relation.Should().Be(expected);
    }

    [Then(@"the relation of Faction A for Faction B should be (.*)")]
    public async Task ThenTheRelationOfFactionAForFactionBShouldBe(int expected)
    {
        FactionRelationService factionRelationService = _objectContainer.Resolve<FactionRelationService>();
        Tuple<Faction, Faction> factions = _objectContainer.Resolve<Tuple<Faction, Faction>>("FactionPair");
        
        FactionRelation? relation =
            await factionRelationService.GetFactionRelationAsync(factions.Item1, factions.Item2);
        
        relation.Should().NotBeNull();
        relation!.Relation.Should().Be(expected);
    }
}
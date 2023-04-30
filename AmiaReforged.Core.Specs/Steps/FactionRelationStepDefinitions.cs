using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Specs;
using AmiaReforged.Core.Specs.Steps;
using BoDi;
using FluentAssertions;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowProject1.Steps;

[Binding]
public class FactionRelationStepDefinitions
{
    private readonly IObjectContainer _objectContainer;
    private readonly ISpecFlowOutputHelper _outputHelper;

    public FactionRelationStepDefinitions(IObjectContainer objectContainer, ISpecFlowOutputHelper outputHelper)
    {
        _objectContainer = objectContainer;
        _outputHelper = outputHelper;
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        _objectContainer.RegisterTypeAs<FactionRelationService, FactionRelationService>(ObjectContainerKeys.FactionRelationService);
    }

    [When(@"I check the relation between the pair of Factions")]
    public async Task WhenICheckTheRelationBetweenThePairOfFactions()
    {
        FactionRelationService factionRelationService = _objectContainer.Resolve<FactionRelationService>();
        Tuple<Faction, Faction> factions = _objectContainer.Resolve<Tuple<Faction, Faction>>(ObjectContainerKeys.FactionPair);

        FactionRelation? relation =
            await factionRelationService.GetFactionRelationAsync(factions.Item1, factions.Item2);

        _objectContainer.RegisterInstanceAs(relation, ObjectContainerKeys.FactionPairRelation);
    }

    [Then(@"the relation should be (.*)")]
    public void ThenTheRelationIs(int expectedValue)
    {
        FactionRelation? relation = _objectContainer.Resolve<FactionRelation?>(ObjectContainerKeys.FactionPairRelation);
        relation.Should().NotBeNull();
        relation?.Relation.Should().Be(expectedValue);
    }

    [Then(@"the Faction should have a neutral relationship with every other Faction")]
    public async Task ThenTheFactionShouldHaveANeutralRelationshipWithEveryOtherFaction()
    {
        FactionRelationService factionRelationService = _objectContainer.Resolve<FactionRelationService>();
        FactionService factionService = _objectContainer.Resolve<FactionService>();
        Faction faction = _objectContainer.Resolve<Faction>(ObjectContainerKeys.Faction);

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
        Tuple<Faction, Faction> factions = _objectContainer.Resolve<Tuple<Faction, Faction>>(ObjectContainerKeys.FactionPair);

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
        Tuple<Faction, Faction> factions = _objectContainer.Resolve<Tuple<Faction, Faction>>(ObjectContainerKeys.FactionPair);

        FactionRelation? relation =
            await factionRelationService.GetFactionRelationAsync(factions.Item2, factions.Item1);

        relation.Should().NotBeNull();
        relation!.Relation.Should().Be(expected);
    }

    [Then(@"the relation of Faction A for Faction B should be (.*)")]
    public async Task ThenTheRelationOfFactionAForFactionBShouldBe(int expected)
    {
        FactionRelationService factionRelationService = _objectContainer.Resolve<FactionRelationService>();
        Tuple<Faction, Faction> factions = _objectContainer.Resolve<Tuple<Faction, Faction>>(ObjectContainerKeys.FactionPair);
        
        FactionRelation? relation =
            await factionRelationService.GetFactionRelationAsync(factions.Item1, factions.Item2);
        
        relation.Should().NotBeNull();
        relation!.Relation.Should().Be(expected);
    }

    [When(@"I set the relation of the Faction with the Character to (.*)")]
    public async Task WhenISetTheRelationOfTheFactionWithTheCharacterTo(int value)
    {
        FactionRelationService factionRelationService = _objectContainer.Resolve<FactionRelationService>(ObjectContainerKeys.FactionRelationService);
        Faction faction = _objectContainer.Resolve<Faction>(ObjectContainerKeys.Faction);
        Character character = _objectContainer.Resolve<Character>(ObjectContainerKeys.Character);
        FactionCharacterRelation relation = new()
        {
            CharacterId = character.Id,
            FactionName = faction.Name,
            Relation = value
        };


        await factionRelationService.AddFactionCharacterRelation(relation);
    }

    [When(@"I update the relation of the Faction with the Character to (.*)")]
    public async Task WhenIUpdateTheRelationOfTheFactionWithTheCharacterTo(int value)
    {
        FactionRelationService factionRelationService = _objectContainer.Resolve<FactionRelationService>(ObjectContainerKeys.FactionRelationService);
        Faction faction = _objectContainer.Resolve<Faction>(ObjectContainerKeys.Faction);
        Character character = _objectContainer.Resolve<Character>(ObjectContainerKeys.Character);
        FactionCharacterRelation relation = new()
        {
            CharacterId = character.Id,
            FactionName = faction.Name,
            Relation = value
        };

        _outputHelper.WriteLine($"Updating relation of {faction.Name} with {character.FirstName} to {value}");
        await factionRelationService.UpdateFactionCharacterRelation(relation);
    }

    [Then(@"the relation of the Faction for the Character should be (.*)")]
    public async Task ThenTheRelationOfTheFactionForTheCharacterShouldBe(int expectedValue)
    {
        FactionRelationService factionRelationService = _objectContainer.Resolve<FactionRelationService>(ObjectContainerKeys.FactionRelationService);
        Faction faction = _objectContainer.Resolve<Faction>(ObjectContainerKeys.Faction);
        Character character = _objectContainer.Resolve<Character>(ObjectContainerKeys.Character);
        
        FactionCharacterRelation? relation = await factionRelationService.GetFactionCharacterRelation(faction.Name, character.Id);
        
        relation.Should().NotBeNull("the relation should exist");
        relation!.Relation.Should().Be(expectedValue, "the relation should be the expected value");
    }
}
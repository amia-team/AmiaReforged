using AmiaReforged.Core;
using AmiaReforged.Core.Entities;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using AmiaReforged.System.Helpers;
using BoDi;
using FluentAssertions;
using NUnit.Framework;

namespace SpecFlowProject1.Steps;

[Binding]
public class FactionStepDefinitions
{
    private readonly IObjectContainer _objectContainer;

    public FactionStepDefinitions(IObjectContainer objectContainer)
    {
        _objectContainer = objectContainer;
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        _objectContainer.RegisterInstanceAs(new Faction() { Members = new List<Guid>() }, "Faction");
        _objectContainer.RegisterTypeAs<NwTaskHelper, NwTaskHelper>();
        _objectContainer.RegisterTypeAs<AmiaContext, AmiaContext>();
        _objectContainer.RegisterTypeAs<CharacterService, CharacterService>();
        _objectContainer.RegisterTypeAs<FactionService, FactionService>();
    }

    [Given(@"a Faction named ""(.*)""")]
    public void GivenAFactionNamed(string name)
    {
        Faction? faction = _objectContainer.Resolve<Faction>("Faction");
        faction.Name = name;
    }

    [Given(@"with the description ""(.*)""")]
    public void GivenWithTheDescription(string description)
    {
        Faction? faction = _objectContainer.Resolve<Faction>("Faction");
        faction.Description = description;
    }

    [When(@"a request is made to persist the Faction")]
    public async Task WhenARequestIsMadeToPersistTheFaction()
    {
        FactionService? factionService = _objectContainer.Resolve<FactionService>();
        Faction? faction = _objectContainer.Resolve<Faction>("Faction");
        await factionService.AddFaction(faction);
    }

    [Then(@"the Faction should be persisted")]
    public async Task ThenTheFactionShouldBePersisted()
    {
        FactionService? factionService = _objectContainer.Resolve<FactionService>();
        Faction? faction = _objectContainer.Resolve<Faction>("Faction");
        Faction? persistedFaction = await factionService.GetFactionByName(faction.Name);

        persistedFaction.Should().NotBeNull();
        persistedFaction!.Name.Should().Be(faction.Name);
        persistedFaction.Description.Should().Be(faction.Description);
    }

    [When(@"a request is made to update the Faction with the name ""(.*)"" and the description ""(.*)""")]
    public async Task WhenARequestIsMadeToUpdateTheFactionWithTheNameAndTheDescription(string name, string description)
    {
        Faction? faction = _objectContainer.Resolve<Faction>("Faction");
        faction.Name = name;
        faction.Description = description;

        FactionService? factionService = _objectContainer.Resolve<FactionService>();
        await factionService.UpdateFaction(faction);
    }

    [Then(@"the Faction should be updated")]
    public void ThenTheFactionShouldBeUpdated()
    {
        FactionService? factionService = _objectContainer.Resolve<FactionService>();
        Faction? faction = _objectContainer.Resolve<Faction>("Faction");
        Faction? persistedFaction = factionService.GetFactionByName(faction.Name).Result;

        persistedFaction.Should().NotBeNull("The faction should have been persisted");
        persistedFaction!.Name.Should().Be(faction.Name, "because the name should be updated");
        persistedFaction.Description.Should().Be(faction.Description, "because the description should be updated");
    }

    [When(@"a request is made to delete the Faction")]
    public async Task WhenARequestIsMadeToDeleteTheFaction()
    {
        FactionService? factionService = _objectContainer.Resolve<FactionService>();
        Faction? faction = _objectContainer.Resolve<Faction>("Faction");
        await factionService.DeleteFaction(faction);
    }

    [Then(@"the Faction should be deleted")]
    public async Task ThenTheFactionShouldBeDeleted()
    {
        FactionService? factionService = _objectContainer.Resolve<FactionService>();
        Faction? faction = _objectContainer.Resolve<Faction>("Faction");
        Faction? persistedFaction = await factionService.GetFactionByName(faction.Name);

        persistedFaction.Should().BeNull("because the faction should have been deleted");
    }

    [When(@"a request is made to add the characters to the faction")]
    public async Task WhenARequestIsMadeToAddTheCharactersToTheFaction()
    {
        List<Character> characters = _objectContainer.Resolve<List<Character>>("testCharacters");

        CharacterService characterService = _objectContainer.Resolve<CharacterService>();
        await characterService.AddCharacters(characters);

        Faction? faction = _objectContainer.Resolve<Faction>("Faction");

        characters.ForEach(character => faction.Members.Add(character.Id));

        FactionService? factionService = _objectContainer.Resolve<FactionService>();

        await factionService.UpdateFaction(faction);
    }

    [Then(@"the characters should be added to the faction roster")]
    public async Task ThenTheCharactersShouldBeAddedToTheFactionRoster()
    {
        FactionService? factionService = _objectContainer.Resolve<FactionService>();
        Faction? faction = _objectContainer.Resolve<Faction>("Faction");
        Faction? persistedFaction = await factionService.GetFactionByName(faction.Name);

        persistedFaction.Should().NotBeNull("because the faction should have been persisted");
        persistedFaction!.Members.Should().HaveCount(2, "because two characters should have been added to the faction");
    }

    [Given(@"the faction already has a list of members")]
    public async Task GivenTheFactionAlreadyHasAListOfMembers()
    {
        Character memberOne = new()
        {
            FirstName = "Test1",
            LastName = "Test1",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = true
        };

        Character memberTwo = new()
        {
            FirstName = "Test2",
            LastName = "Test2",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = true
        };

        Character memberThree = new()
        {
            FirstName = "Test3",
            LastName = "Test3",
            Id = Guid.NewGuid(),
        };

        CharacterService characterService = _objectContainer.Resolve<CharacterService>();
        await characterService.AddCharacters(new List<Character>() { memberOne, memberTwo, memberThree });

        Faction? faction = _objectContainer.Resolve<Faction>("Faction");
        faction.Members.AddRange(new List<Guid> { memberOne.Id, memberTwo.Id, memberThree.Id });
        
        FactionService? factionService = _objectContainer.Resolve<FactionService>();
        
        await factionService.UpdateFaction(faction);
    }

    [Then(@"the Faction should be persisted with the list of members")]
    public async Task ThenTheFactionShouldBePersistedWithTheListOfMembers()
    {
        FactionService? factionService = _objectContainer.Resolve<FactionService>();
        Faction? faction = _objectContainer.Resolve<Faction>("Faction");
        Faction? persistedFaction = await factionService.GetFactionByName(faction.Name);

        persistedFaction.Should().NotBeNull("because the faction should have been persisted");
        persistedFaction!.Members.Should().HaveCount(3, "because three members should have been added to the faction");
    }

    [Given(@"the roster contains a character that does not exist")]
    public void GivenTheRosterContainsACharacterThatDoesNotExist()
    {
        Faction? faction = _objectContainer.Resolve<Faction>("Faction");
        faction.Members.Add(Guid.NewGuid());
    }

    [Given(@"multiple factions with random names")]
    public void GivenMultipleFactionsWithRandomNames()
    {
        List<Faction> factions = new();

        for (int i = 0; i < 10; i++)
        {
            factions.Add(new Faction()
            {
                Name = Guid.NewGuid().ToString(),
                Description = Guid.NewGuid().ToString(),
                Members = new List<Guid>()
            });
        }

        _objectContainer.RegisterInstanceAs(factions);
    }
}
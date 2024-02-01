using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using BoDi;
using FluentAssertions;
using TechTalk.SpecFlow.Infrastructure;
using Testcontainers.PostgreSql;

namespace AmiaReforged.Core.Specs.Steps;

[Binding]
public class CharacterStepDefinitions
{
    private readonly IObjectContainer _objectContainer;

    public CharacterStepDefinitions(IObjectContainer objectContainer)
    {
        _objectContainer = objectContainer;
    }

    [BeforeScenario]
    public async void BeforeScenario()
    {
        PlayerCharacter playerCharacter = new()
        {
            Id = Guid.NewGuid()
        };
        
        _objectContainer.RegisterInstanceAs(playerCharacter, ObjectContainerKeys.Character);
    }


    [Given(@"a player with the CDKey '(.*)'")]
    public void GivenAPlayerWithTheCdKey(string cdKey)
    {
        PlayerCharacter playerCharacter = _objectContainer.Resolve<PlayerCharacter>(ObjectContainerKeys.Character);
        playerCharacter.CdKey = cdKey;
        playerCharacter.IsPlayerCharacter = true;
    }

    [Given(@"a Character with the first name '(.*)' and last name '(.*)'")]
    public void GivenACharacterWithTheFirstNameAndLastName(string firstName, string lastName)
    {
        PlayerCharacter playerCharacter = _objectContainer.Resolve<PlayerCharacter>(ObjectContainerKeys.Character);
        playerCharacter.FirstName = firstName;
        playerCharacter.LastName = lastName;
    }


    [When(@"a request is made to persist the Character")]
    public async Task WhenARequestIsMadeToPersistTheCharacter()
    {
        CharacterService characterService =
            _objectContainer.Resolve<CharacterService>(ObjectContainerKeys.CharacterService);
        PlayerCharacter playerCharacter = _objectContainer.Resolve<PlayerCharacter>(ObjectContainerKeys.Character);
        await characterService.AddCharacter(playerCharacter);
    }

    [Then(@"the Character should be persisted")]
    public async Task ThenTheCharacterShouldBePersisted()
    {
        CharacterService characterService =
            _objectContainer.Resolve<CharacterService>(ObjectContainerKeys.CharacterService);
        PlayerCharacter playerCharacter = _objectContainer.Resolve<PlayerCharacter>(ObjectContainerKeys.Character);
        PlayerCharacter? persistedCharacter = await characterService.GetCharacterByGuid(playerCharacter.Id);

        persistedCharacter.Should().NotBeNull("the character should be persisted");
        persistedCharacter!.FirstName.Should().Be(playerCharacter.FirstName, "the first name should be persisted");
        persistedCharacter.LastName.Should().Be(playerCharacter.LastName, "the last name should be persisted");
        persistedCharacter.CdKey.Should().Be(playerCharacter.CdKey, "the cd key should be persisted");
        persistedCharacter.IsPlayerCharacter.Should()
            .Be(playerCharacter.IsPlayerCharacter, "the player character flag should be set");
    }

    [When(@"a request is made to update the Character with the first name '(.*)' and last name '(.*)'")]
    public async Task WhenARequestIsMadeToUpdateTheCharacterWithTheFirstNameAndLastName(string updated, string test)
    {
        PlayerCharacter playerCharacter = _objectContainer.Resolve<PlayerCharacter>(ObjectContainerKeys.Character);
        playerCharacter.FirstName = updated;
        playerCharacter.LastName = test;

        CharacterService characterService =
            _objectContainer.Resolve<CharacterService>(ObjectContainerKeys.CharacterService);

        await characterService.UpdateCharacter(playerCharacter);
    }


    [Then(@"the Character's name should be updated")]
    public async Task ThenTheCharactersNameShouldBeUpdated()
    {
        CharacterService characterService =
            _objectContainer.Resolve<CharacterService>(ObjectContainerKeys.CharacterService);
        PlayerCharacter playerCharacter = _objectContainer.Resolve<PlayerCharacter>(ObjectContainerKeys.Character);
        PlayerCharacter? persistedCharacter = await characterService.GetCharacterByGuid(playerCharacter.Id);

        persistedCharacter.Should().NotBeNull("the character should be persisted");
        persistedCharacter!.FirstName.Should().Be(playerCharacter.FirstName, "the first name should be persisted");
        persistedCharacter.LastName.Should().Be(playerCharacter.LastName, "the last name should be persisted");
    }

    [When(@"a request is made to delete the Character")]
    public async Task WhenARequestIsMadeToDeleteTheCharacter()
    {
        CharacterService characterService =
            _objectContainer.Resolve<CharacterService>(ObjectContainerKeys.CharacterService);
        PlayerCharacter playerCharacter = _objectContainer.Resolve<PlayerCharacter>(ObjectContainerKeys.Character);
        await characterService.DeleteCharacter(playerCharacter);
    }

    [Then(@"the Character should be deleted")]
    public async Task ThenTheCharacterShouldBeDeleted()
    {
        CharacterService characterService =
            _objectContainer.Resolve<CharacterService>(ObjectContainerKeys.CharacterService);
        PlayerCharacter playerCharacter = _objectContainer.Resolve<PlayerCharacter>(ObjectContainerKeys.Character);
        PlayerCharacter? persistedCharacter = await characterService.GetCharacterByGuid(playerCharacter.Id);

        persistedCharacter.Should().BeNull("the character should be deleted");
    }

    [Given(@"a list of Characters")]
    public void GivenAListOfCharacters()
    {
        List<PlayerCharacter> characters = new();
        _objectContainer.RegisterInstanceAs(characters, ObjectContainerKeys.TestCharacters);
    }

    [Given(@"a Character named '(.*)' and last name '(.*)' is added to the list")]
    public void GivenACharacterNamedAndLastNameIsAddedToTheList(string first, string last)
    {
        List<PlayerCharacter> characters = _objectContainer.Resolve<List<PlayerCharacter>>(ObjectContainerKeys.TestCharacters);
        characters.Add(new PlayerCharacter
        {
            FirstName = first,
            LastName = last,
            Id = Guid.NewGuid()
        });
    }

    [When(@"all of the Characters are added to the database")]
    public async Task WhenAllOfTheCharactersAreAddedToTheDatabase()
    {
        List<PlayerCharacter> characters = _objectContainer.Resolve<List<PlayerCharacter>>(ObjectContainerKeys.TestCharacters);
        CharacterService characterService =
            _objectContainer.Resolve<CharacterService>(ObjectContainerKeys.CharacterService);
        await characterService.AddCharacters(characters);
        
    }

    [Then(@"the list of all Characters should be retrievable")]
    public async Task ThenTheListOfCharactersShouldBeReturned()
    {
        List<PlayerCharacter> characters = _objectContainer.Resolve<List<PlayerCharacter>>(ObjectContainerKeys.TestCharacters);
        CharacterService characterService =
            _objectContainer.Resolve<CharacterService>(ObjectContainerKeys.CharacterService);

        List<PlayerCharacter> persistedCharacters = await characterService.GetAllCharacters();

        persistedCharacters.Should().NotBeEmpty("the list of characters should have been persisted");

        foreach (PlayerCharacter character in characters)
        {
            persistedCharacters.Should()
                .ContainEquivalentOf(character, "the list of characters should contain the character");
        }
    }

    [When(@"a request is made to delete all Characters in the list")]
    public async Task WhenARequestIsMadeToDeleteAllCharactersInTheList()
    {
        List<PlayerCharacter> characters = _objectContainer.Resolve<List<PlayerCharacter>>(ObjectContainerKeys.TestCharacters);
        CharacterService characterService =
            _objectContainer.Resolve<CharacterService>(ObjectContainerKeys.CharacterService);

        await characterService.DeleteCharacters(characters);
    }

    [Then(@"the list of Characters should be deleted")]
    public async Task ThenTheListOfCharactersShouldBeDeleted()
    {
        List<PlayerCharacter> characters = _objectContainer.Resolve<List<PlayerCharacter>>(ObjectContainerKeys.TestCharacters);
        CharacterService characterService =
            _objectContainer.Resolve<CharacterService>(ObjectContainerKeys.CharacterService);

        List<PlayerCharacter> persistedCharacters = await characterService.GetAllCharacters();

        characters.ForEach(character =>
            persistedCharacters.Should()
                .NotContainEquivalentOf(character, "the list of characters should not contain the character"));
    }

    [Given(@"all Characters in the list are Player Characters")]
    public void GivenAllCharactersInTheListArePlayerCharacters()
    {
        List<PlayerCharacter> characters = _objectContainer.Resolve<List<PlayerCharacter>>(ObjectContainerKeys.TestCharacters);
        characters.ForEach(c => c.IsPlayerCharacter = true);
    }

    [Then(@"the list of all player Characters should be retrievable")]
    public async Task ThenTheListOfAllPlayerCharactersShouldBeRetrievable()
    {
        List<PlayerCharacter> characters = _objectContainer.Resolve<List<PlayerCharacter>>(ObjectContainerKeys.TestCharacters);
        CharacterService characterService =
            _objectContainer.Resolve<CharacterService>(ObjectContainerKeys.CharacterService);

        List<PlayerCharacter> persistedCharacters = await characterService.GetAllPlayerCharacters();

        persistedCharacters.Should().NotBeEmpty("the list of characters should not be empty");

        characters.ForEach(character => persistedCharacters.Should()
            .ContainEquivalentOf(character, "the list of characters should contain the character"));
    }

    [Given(@"all Characters in the list are Non-Player Characters")]
    public void GivenAllCharactersInTheListAreNonPlayerCharacters()
    {
        List<PlayerCharacter> characters = _objectContainer.Resolve<List<PlayerCharacter>>(ObjectContainerKeys.TestCharacters);
        characters.ForEach(c => c.IsPlayerCharacter = false);
    }

    [Then(@"the list of all non-player Characters should be retrievable")]
    public async Task ThenTheListOfAllNonPlayerCharactersShouldBeRetrievable()
    {
        List<PlayerCharacter> characters = _objectContainer.Resolve<List<PlayerCharacter>>(ObjectContainerKeys.TestCharacters);
        CharacterService characterService =
            _objectContainer.Resolve<CharacterService>(ObjectContainerKeys.CharacterService);

        List<PlayerCharacter> persistedCharacters = await characterService.GetAllNonPlayerCharacters();

        persistedCharacters.Should().NotBeEmpty("the list of characters should not be empty");

        characters.ForEach(character => persistedCharacters.Should()
            .ContainEquivalentOf(character, "the list of characters should contain the character"));
    }

    [Then(@"a request to determine if the Character exists should be '(.*)'")]
    public async Task ThenARequestToDetermineIfTheCharacterExistsShouldBe(bool exists)
    {
        CharacterService characterService =
            _objectContainer.Resolve<CharacterService>(ObjectContainerKeys.CharacterService);
        PlayerCharacter playerCharacter = _objectContainer.Resolve<PlayerCharacter>(ObjectContainerKeys.Character);

        bool doesCharacterExist = await characterService.CharacterExists(playerCharacter.Id);
        doesCharacterExist.Should().Be(exists);
    }


    [Given(@"the most recently added Character is a player character")]
    public void GivenTheMostRecentlyAddedCharacterIsAPlayerCharacter()
    {
        List<PlayerCharacter> characters = _objectContainer.Resolve<List<PlayerCharacter>>(ObjectContainerKeys.TestCharacters);
        characters.Last().IsPlayerCharacter = true;
    }
}
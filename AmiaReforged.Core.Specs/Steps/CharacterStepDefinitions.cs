using AmiaReforged.Core;
using AmiaReforged.Core.Entities;
using AmiaReforged.Core.Services;
using AmiaReforged.System.Helpers;
using BoDi;
using FluentAssertions;
using TechTalk.SpecFlow.Infrastructure;

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
    public void BeforeScenario()
    {
        Character character = new()
        {
            Id = Guid.NewGuid()
        };

        _objectContainer.RegisterInstanceAs(character, "testCharacter");
        _objectContainer.RegisterTypeAs<NwTaskHelper, NwTaskHelper>("nwTaskHelper");
        _objectContainer.RegisterTypeAs<AmiaContext, AmiaContext>("amiaContext");
        _objectContainer.RegisterTypeAs<CharacterService, CharacterService>("testCharacterService");
    }


    [Given(@"a player with the CDKey '(.*)'")]
    public void GivenAPlayerWithTheCdKey(string cdKey)
    {
        Character character = _objectContainer.Resolve<Character>("testCharacter");
        character.CdKey = cdKey;
        character.IsPlayerCharacter = true;
    }

    [Given(@"a Character with the first name '(.*)' and last name '(.*)'")]
    public void GivenACharacterWithTheFirstNameAndLastName(string firstName, string lastName)
    {
        Character character = _objectContainer.Resolve<Character>("testCharacter");
        character.FirstName = firstName;
        character.LastName = lastName;
    }


    [When(@"a request is made to persist the Character")]
    public async Task WhenARequestIsMadeToPersistTheCharacter()
    {
        CharacterService characterService = _objectContainer.Resolve<CharacterService>("testCharacterService");
        Character character = _objectContainer.Resolve<Character>("testCharacter");
        await characterService.AddCharacter(character);
    }

    [Then(@"the Character should be persisted")]
    public async Task ThenTheCharacterShouldBePersisted()
    {
        CharacterService characterService = _objectContainer.Resolve<CharacterService>("testCharacterService");
        Character character = _objectContainer.Resolve<Character>("testCharacter");
        Character? persistedCharacter = await characterService.GetCharacterById(character.Id);

        persistedCharacter.Should().NotBeNull("the character should be persisted");
        persistedCharacter!.FirstName.Should().Be(character.FirstName, "the first name should be persisted");
        persistedCharacter.LastName.Should().Be(character.LastName, "the last name should be persisted");
        persistedCharacter.CdKey.Should().Be(character.CdKey, "the cd key should be persisted");
        persistedCharacter.IsPlayerCharacter.Should()
            .Be(character.IsPlayerCharacter, "the player character flag should be set");
    }

    [When(@"a request is made to update the Character with the first name '(.*)' and last name '(.*)'")]
    public async Task WhenARequestIsMadeToUpdateTheCharacterWithTheFirstNameAndLastName(string updated, string test)
    {
        Character character = _objectContainer.Resolve<Character>("testCharacter");
        character.FirstName = updated;
        character.LastName = test;

        CharacterService characterService = _objectContainer.Resolve<CharacterService>("testCharacterService");

        await characterService.UpdateCharacter(character);
    }


    [Then(@"the Character's name should be updated")]
    public async Task ThenTheCharactersNameShouldBeUpdated()
    {
        CharacterService characterService = _objectContainer.Resolve<CharacterService>("testCharacterService");
        Character character = _objectContainer.Resolve<Character>("testCharacter");
        Character? persistedCharacter = await characterService.GetCharacterById(character.Id);

        persistedCharacter.Should().NotBeNull("the character should be persisted");
        persistedCharacter!.FirstName.Should().Be(character.FirstName, "the first name should be persisted");
        persistedCharacter.LastName.Should().Be(character.LastName, "the last name should be persisted");
    }

    [When(@"a request is made to delete the Character")]
    public async Task WhenARequestIsMadeToDeleteTheCharacter()
    {
        CharacterService characterService = _objectContainer.Resolve<CharacterService>("testCharacterService");
        Character character = _objectContainer.Resolve<Character>("testCharacter");
        await characterService.DeleteCharacter(character);
    }

    [Then(@"the Character should be deleted")]
    public async Task ThenTheCharacterShouldBeDeleted()
    {
        CharacterService characterService = _objectContainer.Resolve<CharacterService>("testCharacterService");
        Character character = _objectContainer.Resolve<Character>("testCharacter");
        Character? persistedCharacter = await characterService.GetCharacterById(character.Id);

        persistedCharacter.Should().BeNull("the character should be deleted");
    }

    [Given(@"a list of Characters")]
    public void GivenAListOfCharacters()
    {
        List<Character> characters = new();
        _objectContainer.RegisterInstanceAs(characters, "testCharacters");
    }

    [Given(@"a Character named '(.*)' and last name '(.*)' is added to the list")]
    public void GivenACharacterNamedAndLastNameIsAddedToTheList(string first, string last)
    {
        List<Character> characters = _objectContainer.Resolve<List<Character>>("testCharacters");
        characters.Add(new Character
        {
            FirstName = first,
            LastName = last,
            Id = Guid.NewGuid()
        });
    }

    [When(@"all of the Characters are added to the database")]
    public async Task WhenAllOfTheCharactersAreAddedToTheDatabase()
    {
        List<Character> characters = _objectContainer.Resolve<List<Character>>("testCharacters");
        CharacterService characterService = _objectContainer.Resolve<CharacterService>("testCharacterService");
        await characterService.AddCharacters(characters);
    }

    [Then(@"the list of all Characters should be retrievable")]
    public async Task ThenTheListOfCharactersShouldBeReturned()
    {
        List<Character> characters = _objectContainer.Resolve<List<Character>>("testCharacters");
        CharacterService characterService = _objectContainer.Resolve<CharacterService>("testCharacterService");

        List<Character> persistedCharacters = await characterService.GetAllCharacters();

        persistedCharacters.Should().NotBeEmpty("the list of characters should not be empty");

        foreach (Character character in characters)
        {
            persistedCharacters.Should()
                .ContainEquivalentOf(character, "the list of characters should contain the character");
        }
    }

    [When(@"a request is made to delete all Characters in the list")]
    public async Task WhenARequestIsMadeToDeleteAllCharactersInTheList()
    {
        List<Character> characters = _objectContainer.Resolve<List<Character>>("testCharacters");
        CharacterService characterService = _objectContainer.Resolve<CharacterService>("testCharacterService");

        await characterService.DeleteCharacters(characters);
    }

    [Then(@"the list of Characters should be deleted")]
    public async Task ThenTheListOfCharactersShouldBeDeleted()
    {
        List<Character> characters = _objectContainer.Resolve<List<Character>>("testCharacters");
        CharacterService characterService = _objectContainer.Resolve<CharacterService>("testCharacterService");

        List<Character> persistedCharacters = await characterService.GetAllCharacters();

        characters.ForEach(character =>
            persistedCharacters.Should()
                .NotContainEquivalentOf(character, "the list of characters should not contain the character"));
    }

    [Given(@"all Characters in the list are Player Characters")]
    public void GivenAllCharactersInTheListArePlayerCharacters()
    {
        List<Character> characters = _objectContainer.Resolve<List<Character>>("testCharacters");
        characters.ForEach(c => c.IsPlayerCharacter = true);
    }

    [Then(@"the list of all player Characters should be retrievable")]
    public async Task ThenTheListOfAllPlayerCharactersShouldBeRetrievable()
    {
        List<Character> characters = _objectContainer.Resolve<List<Character>>("testCharacters");
        CharacterService characterService = _objectContainer.Resolve<CharacterService>("testCharacterService");

        List<Character> persistedCharacters = await characterService.GetAllPlayerCharacters();

        persistedCharacters.Should().NotBeEmpty("the list of characters should not be empty");

        characters.ForEach(character => persistedCharacters.Should()
            .ContainEquivalentOf(character, "the list of characters should contain the character"));
    }

    [Given(@"all Characters in the list are Non-Player Characters")]
    public void GivenAllCharactersInTheListAreNonPlayerCharacters()
    {
        List<Character> characters = _objectContainer.Resolve<List<Character>>("testCharacters");
        characters.ForEach(c => c.IsPlayerCharacter = false);
    }

    [Then(@"the list of all non-player Characters should be retrievable")]
    public async Task ThenTheListOfAllNonPlayerCharactersShouldBeRetrievable()
    {
        List<Character> characters = _objectContainer.Resolve<List<Character>>("testCharacters");
        CharacterService characterService = _objectContainer.Resolve<CharacterService>("testCharacterService");

        List<Character> persistedCharacters = await characterService.GetAllNonPlayerCharacters();

        persistedCharacters.Should().NotBeEmpty("the list of characters should not be empty");

        characters.ForEach(character => persistedCharacters.Should()
            .ContainEquivalentOf(character, "the list of characters should contain the character"));
    }

    [Then(@"a request to determine if the Character exists should be '(.*)'")]
    public async Task ThenARequestToDetermineIfTheCharacterExistsShouldBe(bool exists)
    {
        CharacterService characterService = _objectContainer.Resolve<CharacterService>("testCharacterService");
        Character character = _objectContainer.Resolve<Character>("testCharacter");

        bool doesCharacterExist = await characterService.CharacterExists(character.Id);
        doesCharacterExist.Should().Be(exists);
    }
}
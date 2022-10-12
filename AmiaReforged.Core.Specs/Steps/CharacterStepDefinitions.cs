using AmiaReforged.Core.Entities;
using BoDi;
using Moq;
using NUnit.Framework;

namespace Amia.Core.Specs.Steps;

[Binding]
public class CharacterStepDefinitions
{
    // IObjectContainer is a BoDi container that is used to resolve dependencies.
    private IObjectContainer _container;
    private AmiaPlayer _player;
    private string _characterName;
    private Mock<ICharacterRepository> _characterRepositoryMock;

    // Dependency injection container is passed in the constructor
    public CharacterStepDefinitions(IObjectContainer container)
    {
        _container = container;
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        _characterRepositoryMock = new Mock<ICharacterRepository>();
        _container.RegisterInstanceAs(_characterRepositoryMock.Object);
        // ICharacterRepository characterRepository = new FakeCharacterRepository();
    }

    [Given(@"a player with the public CD key '(.*)'")]
    public void GivenAPlayerWithThePublicCdKey(string cdKey)
    {
        _player = new AmiaPlayer(cdKey, _container.Resolve<ICharacterRepository>());
    }

    [When(@"the player makes a character named ""(.*)""")]
    public void WhenThePlayerMakesACharacterNamed(string name)
    {
        // create amia character with pcKey of cdKey + uuid
        AmiaCharacter character = new($"{_player.PublicCdKey}_{Guid.NewGuid()}");
        _characterName = name;
        _player.AddCharacter(character);
    }

    [Then(@"an entry is made for the character")]
    public void ThenAnEntryIsMadeForTheCharacter()
    {
        Assert.That(_player.Characters.Count, Is.EqualTo(1));
    }
}
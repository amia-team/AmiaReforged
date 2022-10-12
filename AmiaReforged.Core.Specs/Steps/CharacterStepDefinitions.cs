using AmiaReforged.Core.Entities;
using AmiaReforged.Core.Types;
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
    
    // Dependency injection container is passed in the constructor
    public CharacterStepDefinitions(IObjectContainer container)
    {
        _container = container;
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        ICharacterAccessor accessor = new FakeCharacterAccessor();
        
        _container.RegisterInstanceAs(accessor);
    }

    [Given(@"a player with the public CD key '(.*)'")]
    public void GivenAPlayerWithThePublicCdKey(string cdKey)
    {
        _player = new AmiaPlayer(cdKey, _container.Resolve<ICharacterAccessor>());
    }

    [When(@"the player makes a character named ""(.*)""")]
    public void WhenThePlayerMakesACharacterNamed(string name)
    {
     
        AmiaCharacter character = new($"{_player.PublicCdKey}_{Guid.NewGuid()}")
        {
            Name = name
        };
        _characterName = name;
        
        _player.AddCharacter(character);
    }

    [Then(@"an entry is made for the character")]
    public void ThenAnEntryIsMadeForTheCharacter()
    {
        Assert.That(_player.Characters.Count, Is.EqualTo(1));
        Assert.That(_player.Characters.ToList()[0].Name, Is.EqualTo(_characterName));
    }
}

public class FakeCharacterAccessor : ICharacterAccessor
{
    private readonly List<AmiaCharacter> _characters;

    public FakeCharacterAccessor()
    {
        _characters = new List<AmiaCharacter>();
    }
    
    public IReadOnlyList<AmiaCharacter> GetCharacters(string publicCdKey)
    {
        return _characters;
    }

    public void AddCharacter(string publicCdKey, AmiaCharacter character)
    {
        _characters.Add(character);
    }
}
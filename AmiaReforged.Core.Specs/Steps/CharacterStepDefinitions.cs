using AmiaReforged.Core.Entities;
using BoDi;

namespace Amia.Core.Specs.Steps;

[Binding]
public class CharacterStepDefinitions
{
    // IObjectContainer is a BoDi container that is used to resolve dependencies.
    private IObjectContainer _container;
    private AmiaPlayer _player;
    
    // Dependency injection container is passed in the constructor
    public CharacterStepDefinitions(IObjectContainer container)
    {
        _container = container;
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
    }

    [Given(@"a player with the public CD key '(.*)'")]
    public void GivenAPlayerWithThePublicCdKey(string p0)
    {
        _player = new AmiaPlayer(p0);
    }

    [When(@"the player makes a character named ""(.*)""")]
    public void WhenThePlayerMakesACharacterNamed(string bob)
    {
        ScenarioContext.StepIsPending();
    }

    [Then(@"an entry is made for the character")]
    public void ThenAnEntryIsMadeForTheCharacter()
    {
        ScenarioContext.StepIsPending();
    }
}
namespace Amia.Core.Specs.Steps;

[Binding]
public class CharacterStepDefinitions
{
    [Given(@"a player with the public CD key '(.*)'")]
    public void GivenAPlayerWithThePublicCdKey(string p0)
    {
        ScenarioContext.StepIsPending();
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
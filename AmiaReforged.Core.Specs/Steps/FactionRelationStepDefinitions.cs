using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using BoDi;

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

        FactionRelation? relation = await factionRelationService.GetFactionRelationAsync(factions.Item1, factions.Item2);
    }

    [Then(@"the relation is (.*)")]
    public void ThenTheRelationIs(int p0)
    {
        ScenarioContext.StepIsPending();
    }
}
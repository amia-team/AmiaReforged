using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Common;

namespace AmiaReforged.EngineSpecs;

[Binding]
public class ItemCalculatorSteps
{
    private WorldItem Item { get; set; } = null!;

    [Given("an Sale Transaction with an Item named {string}")]
    public void GivenAnSaleTransactionWithAnItemNamedOakLog(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Given("the Base Cost is {int}")]
    public void GivenTheBaseCostIs(int p0)
    {

        ScenarioContext.StepIsPending();
    }

    [Given("the Item is made of {enum:MaterialEnum}")]
    public void GivenTheItemIsMadeOfOak(MaterialEnum p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Given("the Item quality is {enum:QualityEnum}")]
    public void GivenTheItemQualityIsAverage(QualityEnum p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Given("a locality demand of {decimal}")]
    public void GivenALocalityDemandOf(decimal p0)
    {
        ScenarioContext.StepIsPending();
    }
}

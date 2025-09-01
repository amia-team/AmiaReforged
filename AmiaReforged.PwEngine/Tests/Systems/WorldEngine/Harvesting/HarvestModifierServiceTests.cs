using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Harvesting;

[TestFixture]
public class HarvestModifierServiceTests
{
    private IIndustryRepository _industryRepository = null!;
    private IHarvestModifierService _sut = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _industryRepository = InMemoryIndustryRepository.Create();

        Knowledge k1 = new()
        {
            Tag = "k1",
            Name = "knowledge 1",
            Description = "eee",
            HarvestEffects =
            [
                new KnowledgeHarvestEffect(["test_node_1", "test_node_2"], [HarvestStep.ItemYield], 1.0f,
                    EffectOperation.Additive),
            ],
            Level = ProficiencyLevel.Novice
        };
        Knowledge k2 = new()
        {
            Tag = "k2",
            Name = "knowledge 2",
            Description = "eeeeeeee",
            HarvestEffects =
            [
                new KnowledgeHarvestEffect(["test_node_1", "test_node_2"], [HarvestStep.HarvestTime], 1.0f,
                    EffectOperation.Additive)
            ],
            Level = ProficiencyLevel.Novice
        };
        Knowledge k3 = new()
        {
            Tag = "k3",
            Name = "knowledge 3",
            Description = "eeee123",
            HarvestEffects =
            [
                new KnowledgeHarvestEffect(["test_node_1", "test_node_2"], [HarvestStep.Quality], 1.0f,
                    EffectOperation.Additive),
            ],
            Level = ProficiencyLevel.Novice
        };

        Industry i = new Industry
        {
            Tag = "industry",
            Name = "Industry with Knowledge",
            Knowledge =
            [
                k1, k2, k3
            ]
        };

        _industryRepository.Add(i);
    }

    [SetUp]
    public void SetUp()
    {
        _sut = new HarvestModifierService(_industryRepository);
    }


    [Test]
    public void Should_Fetch_Modifiers_For_Nodes()
    {

    }
}

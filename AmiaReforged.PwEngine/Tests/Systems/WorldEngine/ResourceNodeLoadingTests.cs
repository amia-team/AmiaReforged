using AmiaReforged.PwEngine.Systems.WorldEngine;
using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine;

[TestFixture]
public class ResourceNodeLoadingTests
{
    private ResourceNodeDefinitionLoadingService _sut = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        _sut = new ResourceNodeDefinitionLoadingService(CreateTestRepository());
    }

    [Test]
    public void Should_Load_Valid_ResourceNode()
    {
        string reactionJson = """
                              {
                                "tag": "test",
                                "requirements" :
                                [
                                    {
                                        "requiredItemType": 0
                                    }
                                ],
                                "yield_modifiers":
                                [
                                    {
                                        "modifierType": "has_knowledge",
                                        "modifierOperation": "addition",
                                        "modifierValue": 1
                                    }
                                ]
                              }
                              """;

        Assert.Fail("Pending implementation");
    }

    private IResourceNodeDefinitionRepository CreateTestRepository()
    {
        return new TestResourceNodeDefinitionRepository();
    }
}

internal class TestResourceNodeDefinitionRepository : IResourceNodeDefinitionRepository
{
    private readonly Dictionary<string, ResourceNodeDefinition> _resourceNodeDefinitions = new();

    public void Create(ResourceNodeDefinition definition)
    {
        _resourceNodeDefinitions.TryAdd(definition.Tag, definition);
    }

    public ResourceNodeDefinition? Get(string tag)
    {
        return _resourceNodeDefinitions.GetValueOrDefault(tag);
    }

    public void Update(ResourceNodeDefinition definition)
    {
        if (!Exists(definition.Tag)) Create(definition);

        _resourceNodeDefinitions[definition.Tag] = definition;
    }

    public bool Delete(string tag)
    {
        return _resourceNodeDefinitions.Remove(tag);
    }

    public bool Exists(string tag)
    {
        return _resourceNodeDefinitions.ContainsKey(tag);
    }
}

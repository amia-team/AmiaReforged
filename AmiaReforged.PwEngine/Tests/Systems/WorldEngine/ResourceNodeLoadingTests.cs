using AmiaReforged.PwEngine.Systems.WorldEngine;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine;

[TestFixture]
public class ResourceNodeLoadingTests
{
    private ResourceNodeLoadingService _sut = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        _sut = new ResourceNodeLoadingService(CreateTestRepository());
    }

    private IResourceNodeDefinitionRepository CreateTestRepository()
    {
        return new TestResourceNodeDefinitionRepository();
    }
}

internal class TestResourceNodeDefinitionRepository : IResourceNodeDefinitionRepository
{
    public void Create(ResourceNodeDefinition definition)
    {
        throw new NotImplementedException();
    }

    public ResourceNodeDefinition Get(string tag)
    {
        throw new NotImplementedException();
    }

    public ResourceNodeDefinition Update(ResourceNodeDefinition definition)
    {
        throw new NotImplementedException();
    }

    public bool Delete(string tag)
    {
        throw new NotImplementedException();
    }

    public bool Exists(string tag)
    {
        throw new NotImplementedException();
    }
}

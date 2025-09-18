using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

[ServiceBinding(typeof(ResourceNodeMappingHelper))]
public class ResourceNodeMappingHelper(IResourceNodeDefinitionRepository definitionRepository)
{
    public PersistentResourceNodeInstance MapFrom(ResourceNodeInstance node)
    {
        return new PersistentResourceNodeInstance
        {
            Id = node.Id,
            Area = node.Area,
            DefinitionTag = node.Definition.Tag,
            Uses = node.Uses,
            Quality = (int)node.Quality,
            X = node.X,
            Y = node.Y,
            Z = node.Z,
            Rotation = node.Rotation,
        };
    }

    public ResourceNodeInstance? MapTo(PersistentResourceNodeInstance instance)
    {
        ResourceNodeDefinition? definition = definitionRepository.Get(instance.DefinitionTag);

        if (definition == null)
        {
            return null;
        }

        return new ResourceNodeInstance
        {
            Id = instance.Id,
            Area = instance.Area,
            Definition = definition,
            Uses = instance.Uses,
            X = instance.X,
            Y = instance.Y,
            Z = instance.Z,
            Quality = (IPQuality)instance.Quality,
            Rotation = instance.Rotation,
        };
    }
}
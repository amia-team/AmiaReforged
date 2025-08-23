using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

[ServiceBinding(typeof(ResourceNodeLoadingService))]
public class ResourceNodeLoadingService(IResourceNodeDefinitionRepository repository)
{
}


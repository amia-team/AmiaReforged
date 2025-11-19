using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;

public sealed record GetItemDefinitionByTagQuery(string Tag) : IQuery<ItemDefinition?>;


using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;

public sealed record GetItemDefinitionByTagQuery(string Tag) : IQuery<ItemBlueprint?>;

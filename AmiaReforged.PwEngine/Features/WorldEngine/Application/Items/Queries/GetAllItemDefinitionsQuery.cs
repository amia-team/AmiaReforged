using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;

public sealed record GetAllItemDefinitionsQuery() : IQuery<List<ItemDefinition>>;


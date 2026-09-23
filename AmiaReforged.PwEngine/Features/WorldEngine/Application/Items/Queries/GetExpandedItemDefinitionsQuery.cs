using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;

/// <summary>
/// Resolves the concrete items expanded from a template blueprint by tag.
/// </summary>
public sealed record GetExpandedItemDefinitionsQuery(string TemplateTag) : IQuery<List<ItemBlueprint>>;

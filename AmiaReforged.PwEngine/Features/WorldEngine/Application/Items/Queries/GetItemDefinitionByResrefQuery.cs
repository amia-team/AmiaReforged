using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

public sealed record GetItemDefinitionByResrefQuery(string Resref) : IQuery<ItemDefinition?>;



using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Commands;

/// <summary>
/// Command to deselect a previously selected trait.
/// Only works for unconfirmed traits.
/// </summary>
public sealed record DeselectTraitCommand(
    CharacterId CharacterId,
    TraitTag TraitTag) : ICommand;


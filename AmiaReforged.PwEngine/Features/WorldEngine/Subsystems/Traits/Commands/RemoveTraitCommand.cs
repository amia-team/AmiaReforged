using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Commands;

/// <summary>
/// Command to revoke a trait from a character.
/// Validates the character holds the trait, then removes the selection from persistence.
/// </summary>
public sealed record RemoveTraitCommand(
    CharacterId CharacterId,
    TraitTag TraitTag) : ICommand;

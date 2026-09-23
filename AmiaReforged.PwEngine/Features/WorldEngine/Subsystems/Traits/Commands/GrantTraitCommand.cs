using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Commands;

/// <summary>
/// Command to grant a trait to a character.
/// Validates the trait definition exists and that the character does not
/// already hold the trait, then persists a confirmed character trait.
/// </summary>
public sealed record GrantTraitCommand(
    CharacterId CharacterId,
    TraitTag TraitTag) : ICommand;

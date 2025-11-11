using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Commands;

/// <summary>
/// Command to toggle a trait's active state (for temporary disabling).
/// </summary>
public sealed record SetTraitActiveCommand(
    CharacterId CharacterId,
    TraitTag TraitTag,
    bool IsActive) : ICommand;


/// <summary>
/// Command to select a trait for a character.
/// Creates an unconfirmed trait selection that can be modified before confirmation.
/// </summary>
public sealed record SelectTraitCommand(
    CharacterId CharacterId,
    TraitTag TraitTag,
    Dictionary<string, bool> UnlockedTraits) : ICommand;


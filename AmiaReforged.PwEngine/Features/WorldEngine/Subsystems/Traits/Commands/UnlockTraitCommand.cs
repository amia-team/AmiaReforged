using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Commands;

/// <summary>
/// Command to unlock a trait for a character (DM/achievement unlock).
/// </summary>
public sealed record UnlockTraitCommand(
    CharacterId CharacterId,
    TraitTag TraitTag) : ICommand;


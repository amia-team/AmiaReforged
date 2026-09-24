using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Commands;

/// <summary>
/// Updates the only writable character statistic: total play time.
/// </summary>
/// <remarks>
/// The command carries only <see cref="PlayTime"/> because the statistics
/// contract (see <see cref="CharacterStats"/>) exposes no other supported,
/// backing field. Dispatching through the command system means a successful
/// update publishes the generic <c>CommandExecutedEvent</c>. See task 022.
/// </remarks>
public sealed record UpdateCharacterStatsCommand(
    CharacterId CharacterId,
    int PlayTime) : ICommand;

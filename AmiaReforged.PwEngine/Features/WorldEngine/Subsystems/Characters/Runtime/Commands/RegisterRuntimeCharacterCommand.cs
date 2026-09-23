using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Commands;

/// <summary>
/// Requests that a freshly constructed <see cref="ICharacter"/> be registered
/// with the runtime character repository.
/// </summary>
/// <remarks>
/// The caller does not pass a <c>NwPlayer</c> or <c>NwCreature</c>. Constructing
/// the <see cref="ICharacter"/> (and resolving its Anvil dependencies) remains the
/// responsibility of <see cref="RuntimeCharacterService"/>.
/// </remarks>
public sealed record RegisterRuntimeCharacterCommand(ICharacter Character) : ICommand;

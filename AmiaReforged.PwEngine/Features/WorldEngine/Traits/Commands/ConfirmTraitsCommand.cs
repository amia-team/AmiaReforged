using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits.Commands;

/// <summary>
/// Command to confirm all selected traits for a character.
/// Finalizes the initial selection and validates the budget.
/// </summary>
public sealed record ConfirmTraitsCommand(CharacterId CharacterId) : ICommand;


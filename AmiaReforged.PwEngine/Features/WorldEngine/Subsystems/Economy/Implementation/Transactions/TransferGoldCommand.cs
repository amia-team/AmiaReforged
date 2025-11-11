using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Transactions;

/// <summary>
/// Command to transfer gold between two personas.
/// Supports transfers between any persona types (Character, Organization, Coinhouse, etc.)
/// </summary>
public sealed record TransferGoldCommand(
    PersonaId From,
    PersonaId To,
    Quantity Amount,
    string? Memo = null
) : ICommand
{
    /// <summary>
    /// Validates the command parameters.
    /// </summary>
    public (bool IsValid, string? ErrorMessage) Validate()
    {
        if (Amount.Value <= 0)
            return (false, "Amount must be greater than zero");

        if (From.Equals(To))
            return (false, "Cannot transfer to self");

        if (Memo != null && Memo.Length > 500)
            return (false, "Memo cannot exceed 500 characters");

        return (true, null);
    }
}


using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Access;

public interface IBankAccessEvaluator
{
    BankAccessProfile Evaluate(PersonaId viewerPersona, CoinhouseAccountSummary accountSummary, IReadOnlyList<CoinhouseAccountHolderDto> holders);
}

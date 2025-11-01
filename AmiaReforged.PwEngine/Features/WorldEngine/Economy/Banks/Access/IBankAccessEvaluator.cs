using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Access;

public interface IBankAccessEvaluator
{
    BankAccessProfile Evaluate(PersonaId viewerPersona, CoinhouseAccountSummary accountSummary, IReadOnlyList<CoinhouseAccountHolderDto> holders);
}

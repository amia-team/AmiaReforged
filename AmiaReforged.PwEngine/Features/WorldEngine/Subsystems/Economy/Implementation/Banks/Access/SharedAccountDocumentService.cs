using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Access;

[ServiceBinding(typeof(SharedAccountDocumentService))]
public class SharedAccountDocumentService
{
    private readonly ICommandHandler<JoinCoinhouseAccountCommand> _joinAccountHandler;
    private readonly IPersonaDescriptorService _personaDescriptors;
    private readonly RuntimeCharacterService _runtimeCharacterService;
    private const string ShareDocumentResRef = "bank_sharedoc";


    public SharedAccountDocumentService(ICommandHandler<JoinCoinhouseAccountCommand> joinAccountHandler,
        IPersonaDescriptorService personaDescriptors, RuntimeCharacterService runtimeCharacterService)
    {
        _joinAccountHandler = joinAccountHandler;
        _personaDescriptors = personaDescriptors;
        _runtimeCharacterService = runtimeCharacterService;

        NwModule.Instance.OnActivateItem += HandleDocumentActivation;
    }

    private async void HandleDocumentActivation(ModuleEvents.OnActivateItem obj)
    {
        if (obj.ActivatedItem.ResRef != ShareDocumentResRef)
            return;

        if (!obj.ItemActivator.IsLoginPlayerCharacter(out NwPlayer? player))
            return;
        if (player.LoginCreature is null)
            return;

        if (!_runtimeCharacterService.TryGetPlayerKey(player, out Guid characterGuid) || characterGuid == Guid.Empty)
            return;

        // If the issuing persona is not set, we cannot proceed, because this document is invalid.
        string? issuingPersona = obj.ActivatedItem
            .GetObjectVariable<LocalVariableString>(BankVariableConstants.ShareDocIssuingPersonaLocalString).Value;
        if (issuingPersona == null)
            return;

        PersonaId issuer = PersonaId.Parse(issuingPersona);

        CharacterId characterId = CharacterId.From(characterGuid);
        PersonaId userPersona = PersonaId.FromCharacter(characterId);

        IReadOnlyList<PersonaDescriptor> descriptors =
            _personaDescriptors.DescribeMany(new[] { issuer, userPersona });

        Dictionary<PersonaId, PersonaDescriptor> descriptorLookup = descriptors.ToDictionary(d => d.Id);

        if (!descriptorLookup.TryGetValue(issuer, out PersonaDescriptor issuerDescription) ||
            !descriptorLookup.TryGetValue(userPersona, out PersonaDescriptor userDescription))
        {
            player.SendServerMessage("The shared account document references an unknown persona.",
                ColorConstants.Red);
            obj.ActivatedItem.Destroy();
            return;
        }

        bool areSamePeople = issuerDescription.PrimaryOwnerCdKey != null &&
                             userDescription.PrimaryOwnerCdKey != null &&
                             issuerDescription.PrimaryOwnerCdKey == userDescription.PrimaryOwnerCdKey;
        if (!areSamePeople && issuer == userPersona)
        {
            areSamePeople = true;
        }

        if (areSamePeople)
        {
            obj.ActivatedItem.Destroy();
            player.SendServerMessage("You cannot join an account you yourself have issued a document for.",
                ColorConstants.Red);
            NwModule.Instance.SendMessageToAllDMs("Player " + player.LoginCreature +
                                                  " attempted to join an account with a document they issued themselves.");
            return;
        }

        // Extract document data (in a real implementation, this would involve reading item variables or similar)
        JoinCoinhouseAccountCommand? command =
            ExtractCommandFromDocument(obj.ActivatedItem, player, userPersona);

        if (command is null)
        {
            player.SendServerMessage("The shared account document is invalid or corrupted.", ColorConstants.Red);
            return;
        }

        CommandResult result = await _joinAccountHandler.HandleAsync(command);

        if (!result.Success)
        {
            player.SendServerMessage(result.ErrorMessage ??
                                     "Unable to add you to the coinhouse account at this time.",
                ColorConstants.Red);
            return;
        }

        obj.ActivatedItem.Destroy();
        player.SendServerMessage("You have been added to the shared account.", ColorConstants.Green);
    }

    private JoinCoinhouseAccountCommand? ExtractCommandFromDocument(NwItem item, NwPlayer player,
        PersonaId userPersona)
    {
        LocalVariableString accountIdVar =
            item.GetObjectVariable<LocalVariableString>(BankVariableConstants.ShareDocAccountIdLocalString);
        if (string.IsNullOrEmpty(accountIdVar.Value))
            return null;
        if (!Guid.TryParse(accountIdVar.Value, out Guid accountId))
            return null;

        LocalVariableInt holderTypeVar =
            item.GetObjectVariable<LocalVariableInt>(BankVariableConstants.ShareDocHolderTypeLocalInt);
        LocalVariableInt holderRoleVar =
            item.GetObjectVariable<LocalVariableInt>(BankVariableConstants.ShareDocHolderRoleLocalInt);
        LocalVariableString coinhouseTagVar =
            item.GetObjectVariable<LocalVariableString>(BankVariableConstants.ShareDocCoinhouseTagLocalString);
        LocalVariableInt bankShareTypeVar =
            item.GetObjectVariable<LocalVariableInt>(BankVariableConstants.ShareDocBankShareTypeLocalInt);

        string? value = coinhouseTagVar.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        CoinhouseTag tag = CoinhouseTag.Parse(value);
        HolderRole role = (HolderRole)holderRoleVar.Value;
        HolderType holderType = (HolderType)holderTypeVar.Value;
        BankShareType shareType = (BankShareType)bankShareTypeVar.Value;

        return new JoinCoinhouseAccountCommand(
            userPersona,
            accountId,
            tag,
            shareType,
            holderType,
            role,
            player.LoginCreature!.OriginalFirstName,
            player.LoginCreature!.OriginalLastName
        );
    }
}

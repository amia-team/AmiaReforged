using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Access;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Tests.Helpers.WorldEngine;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Economy;

/// <summary>
/// Behavior tests for adding additional holders to existing coinhouse accounts.
/// </summary>
[TestFixture]
public class JoinCoinhouseAccountCommandHandlerTests
{
    private Mock<IPersonaRepository> _mockPersonaRepository = null!;
    private Mock<ICoinhouseRepository> _mockCoinhouseRepository = null!;
    private JoinCoinhouseAccountCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPersonaRepository = new Mock<IPersonaRepository>();
        _mockCoinhouseRepository = new Mock<ICoinhouseRepository>();
        _handler = new JoinCoinhouseAccountCommandHandler(
            _mockPersonaRepository.Object,
            _mockCoinhouseRepository.Object);
    }

    [Test]
    public async Task JoinAccount_AccountNotFound_Fails()
    {
        // Given a non-existent account
        CharacterPersona requestor = PersonaTestHelpers.CreateCharacterPersona("NewHolder");
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("TEST_BANK");
        Guid accountId = Guid.NewGuid();

        _mockCoinhouseRepository
            .Setup(r => r.GetAccountForAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseAccountDto?)null);

        _mockPersonaRepository
            .Setup(r => r.Exists(requestor.Id))
            .Returns(true);

        var command = new JoinCoinhouseAccountCommand(
            Requestor: requestor.Id,
            AccountId: accountId,
            Coinhouse: coinhouse,
            ShareType: BankShareType.JointOwner,
            HolderType: HolderType.Individual,
            Role: HolderRole.JointOwner,
            HolderFirstName: "New",
            HolderLastName: "Holder"
        );

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when account doesn't exist");
        Assert.That(result.ErrorMessage, Does.Contain("could not be found").IgnoreCase);
    }

    [Test]
    public async Task JoinAccount_InvalidPersonaType_Fails()
    {
        // Given a non-character persona trying to join
        PersonaId invalidPersona = PersonaId.FromOrganization(OrganizationId.New());
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("TEST_BANK");
        Guid accountId = Guid.NewGuid();

        var command = new JoinCoinhouseAccountCommand(
            Requestor: invalidPersona,
            AccountId: accountId,
            Coinhouse: coinhouse,
            ShareType: BankShareType.JointOwner,
            HolderType: HolderType.Individual,
            Role: HolderRole.JointOwner,
            HolderFirstName: "Test",
            HolderLastName: "Person"
        );

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Only characters can join accounts");
        Assert.That(result.ErrorMessage, Does.Contain("character").IgnoreCase);
    }

    [Test]
    public async Task JoinAccount_Success_AddsNewHolder()
    {
        // Given an existing coinhouse account and a new holder
        CharacterPersona requestor = PersonaTestHelpers.CreateCharacterPersona("NewHolder");
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("TEST_BANK");
        Guid accountId = Guid.NewGuid();

        var existingAccount = new CoinhouseAccountDto
        {
            Id = accountId,
            Debit = 0,
            Credit = 100,
            CoinHouseId = 1L,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Holders = new List<CoinhouseAccountHolderDto>
            {
                new()
                {
                    HolderId = Guid.NewGuid(),
                    Type = HolderType.Individual,
                    Role = HolderRole.Owner,
                    FirstName = "Original",
                    LastName = "Owner"
                }
            }
        };

        _mockCoinhouseRepository
            .Setup(r => r.GetAccountForAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccount);

        _mockPersonaRepository
            .Setup(r => r.Exists(requestor.Id))
            .Returns(true);

        CoinhouseAccountDto? capturedAccount = null;
        _mockCoinhouseRepository
            .Setup(r => r.SaveAccountAsync(It.IsAny<CoinhouseAccountDto>(), It.IsAny<CancellationToken>()))
            .Callback<CoinhouseAccountDto, CancellationToken>((acc, _) => capturedAccount = acc)
            .Returns(Task.CompletedTask);

        var command = new JoinCoinhouseAccountCommand(
            Requestor: requestor.Id,
            AccountId: accountId,
            Coinhouse: coinhouse,
            ShareType: BankShareType.JointOwner,
            HolderType: HolderType.Individual,
            Role: HolderRole.JointOwner,
            HolderFirstName: "New",
            HolderLastName: "Holder"
        );

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then the new holder should be added
        Assert.That(result.Success, Is.True, "Adding holder should succeed");
        Assert.That(capturedAccount, Is.Not.Null, "Account should be saved");
        Assert.That(capturedAccount!.Holders, Has.Count.EqualTo(2), "Should have two holders now");
        Assert.That(capturedAccount.Holders.Any(h => h.FirstName == "New"), Is.True);
    }

    [Test]
    public async Task JoinAccount_DuplicateHolder_Fails()
    {
        // Given an account where the requestor is already a holder
        CharacterPersona requestor = PersonaTestHelpers.CreateCharacterPersona("ExistingHolder");
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("TEST_BANK");
        Guid accountId = Guid.NewGuid();
        Guid requestorGuid = Guid.Parse(requestor.Id.Value);

        var existingAccount = new CoinhouseAccountDto
        {
            Id = accountId,
            Debit = 0,
            Credit = 100,
            CoinHouseId = 1L,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Holders = new List<CoinhouseAccountHolderDto>
            {
                new()
                {
                    HolderId = requestorGuid, // Already a holder
                    Type = HolderType.Individual,
                    Role = HolderRole.Owner,
                    FirstName = "Existing",
                    LastName = "Holder"
                }
            }
        };

        _mockCoinhouseRepository
            .Setup(r => r.GetAccountForAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccount);

        _mockPersonaRepository
            .Setup(r => r.Exists(requestor.Id))
            .Returns(true);

        var command = new JoinCoinhouseAccountCommand(
            Requestor: requestor.Id,
            AccountId: accountId,
            Coinhouse: coinhouse,
            ShareType: BankShareType.JointOwner,
            HolderType: HolderType.Individual,
            Role: HolderRole.JointOwner,
            HolderFirstName: "Existing",
            HolderLastName: "Holder"
        );

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail for duplicate holder");
        Assert.That(result.ErrorMessage, Does.Contain("already").IgnoreCase);
    }

    [Test]
    public async Task JoinAccount_PersonaDoesNotExist_Fails()
    {
        // Given a requestor persona that doesn't exist in the repository
        CharacterPersona requestor = PersonaTestHelpers.CreateCharacterPersona("NonExistent");
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("TEST_BANK");
        Guid accountId = Guid.NewGuid();

        var existingAccount = new CoinhouseAccountDto
        {
            Id = accountId,
            Debit = 0,
            Credit = 100,
            CoinHouseId = 1L,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Holders = new List<CoinhouseAccountHolderDto>()
        };

        _mockCoinhouseRepository
            .Setup(r => r.GetAccountForAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccount);

        _mockPersonaRepository
            .Setup(r => r.Exists(requestor.Id))
            .Returns(false); // Persona doesn't exist

        var command = new JoinCoinhouseAccountCommand(
            Requestor: requestor.Id,
            AccountId: accountId,
            Coinhouse: coinhouse,
            ShareType: BankShareType.JointOwner,
            HolderType: HolderType.Individual,
            Role: HolderRole.JointOwner,
            HolderFirstName: "Test",
            HolderLastName: "Person"
        );

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail for non-existent persona");
        Assert.That(result.ErrorMessage, Does.Contain("character").IgnoreCase);
    }
}

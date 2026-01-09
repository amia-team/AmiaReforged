using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Events;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests;

/// <summary>
/// Behavior tests for updating holder roles on coinhouse accounts.
/// Verifies that ownership transfer is not permitted.
/// </summary>
[TestFixture]
public class UpdateCoinhouseAccountHolderRoleCommandHandlerTests
{
    private Mock<IPersonaRepository> _mockPersonaRepository = null!;
    private Mock<ICoinhouseRepository> _mockCoinhouseRepository = null!;
    private Mock<IEventBus> _mockEventBus = null!;
    private UpdateCoinhouseAccountHolderRoleCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPersonaRepository = new Mock<IPersonaRepository>();
        _mockCoinhouseRepository = new Mock<ICoinhouseRepository>();
        _mockEventBus = new Mock<IEventBus>();
        _handler = new UpdateCoinhouseAccountHolderRoleCommandHandler(
            _mockPersonaRepository.Object,
            _mockCoinhouseRepository.Object,
            _mockEventBus.Object);
    }

    [Test]
    public async Task UpdateRole_Success_ChangesHolderRole()
    {
        // Given an account with an owner and an authorized user
        CharacterPersona owner = PersonaTestHelpers.CreateCharacterPersona("Owner");
        CharacterPersona authorizedUser = PersonaTestHelpers.CreateCharacterPersona("AuthUser");
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("TEST_BANK");
        Guid accountId = Guid.NewGuid();

        CoinhouseAccountDto existingAccount = new()
        {
            Id = accountId,
            Debit = 1000,
            Credit = 0,
            CoinHouseId = 1L,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Holders =
            [
                new()
                {
                    HolderId = Guid.Parse(owner.Id.Value),
                    Type = HolderType.Individual,
                    Role = HolderRole.Owner,
                    FirstName = "Owner",
                    LastName = "Person"
                },
                new()
                {
                    HolderId = Guid.Parse(authorizedUser.Id.Value),
                    Type = HolderType.Individual,
                    Role = HolderRole.AuthorizedUser,
                    FirstName = "Auth",
                    LastName = "User"
                }
            ]
        };

        _mockCoinhouseRepository
            .Setup(r => r.GetAccountForAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccount);

        _mockPersonaRepository
            .Setup(r => r.Exists(owner.Id))
            .Returns(true);

        CoinhouseAccountDto? savedAccount = null;
        _mockCoinhouseRepository
            .Setup(r => r.SaveAccountAsync(It.IsAny<CoinhouseAccountDto>(), It.IsAny<CancellationToken>()))
            .Callback<CoinhouseAccountDto, CancellationToken>((a, _) => savedAccount = a)
            .Returns(Task.CompletedTask);

        // Promote AuthorizedUser to JointOwner
        UpdateCoinhouseAccountHolderRoleCommand command = new(
            Requestor: owner.Id,
            AccountId: accountId,
            CoinhouseTag: coinhouse,
            HolderIdToUpdate: Guid.Parse(authorizedUser.Id.Value),
            NewRole: HolderRole.JointOwner);

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should succeed
        Assert.That(result.Success, Is.True, "Should succeed when changing role");
        Assert.That(savedAccount, Is.Not.Null, "Account should be saved");

        CoinhouseAccountHolderDto? updatedHolder = savedAccount!.Holders
            .FirstOrDefault(h => h.HolderId == Guid.Parse(authorizedUser.Id.Value));
        Assert.That(updatedHolder, Is.Not.Null, "Updated holder should exist");
        Assert.That(updatedHolder!.Role, Is.EqualTo(HolderRole.JointOwner), "Role should be updated to JointOwner");

        // Verify audit event was published
        _mockEventBus.Verify(
            e => e.PublishAsync(It.IsAny<AccountHolderRoleChangedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task UpdateRole_ToOwner_Fails()
    {
        // Given an account with an owner and a joint owner
        CharacterPersona owner = PersonaTestHelpers.CreateCharacterPersona("Owner");
        CharacterPersona jointOwner = PersonaTestHelpers.CreateCharacterPersona("JointOwner");
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("TEST_BANK");
        Guid accountId = Guid.NewGuid();

        CoinhouseAccountDto existingAccount = new()
        {
            Id = accountId,
            Debit = 1000,
            Credit = 0,
            CoinHouseId = 1L,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Holders =
            [
                new()
                {
                    HolderId = Guid.Parse(owner.Id.Value),
                    Type = HolderType.Individual,
                    Role = HolderRole.Owner,
                    FirstName = "Owner",
                    LastName = "Person"
                },
                new()
                {
                    HolderId = Guid.Parse(jointOwner.Id.Value),
                    Type = HolderType.Individual,
                    Role = HolderRole.JointOwner,
                    FirstName = "Joint",
                    LastName = "Owner"
                }
            ]
        };

        _mockCoinhouseRepository
            .Setup(r => r.GetAccountForAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccount);

        _mockPersonaRepository
            .Setup(r => r.Exists(owner.Id))
            .Returns(true);

        // Attempt to promote JointOwner to Owner (ownership transfer)
        UpdateCoinhouseAccountHolderRoleCommand command = new(
            Requestor: owner.Id,
            AccountId: accountId,
            CoinhouseTag: coinhouse,
            HolderIdToUpdate: Guid.Parse(jointOwner.Id.Value),
            NewRole: HolderRole.Owner);

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when trying to transfer ownership");
        Assert.That(result.ErrorMessage, Does.Contain("ownership transfer").IgnoreCase);

        // Verify no save occurred
        _mockCoinhouseRepository.Verify(
            r => r.SaveAccountAsync(It.IsAny<CoinhouseAccountDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task UpdateRole_ChangeOwnerRole_Fails()
    {
        // Given an account with an owner
        CharacterPersona owner = PersonaTestHelpers.CreateCharacterPersona("Owner");
        CharacterPersona jointOwner = PersonaTestHelpers.CreateCharacterPersona("JointOwner");
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("TEST_BANK");
        Guid accountId = Guid.NewGuid();

        CoinhouseAccountDto existingAccount = new()
        {
            Id = accountId,
            Debit = 1000,
            Credit = 0,
            CoinHouseId = 1L,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Holders =
            [
                new()
                {
                    HolderId = Guid.Parse(owner.Id.Value),
                    Type = HolderType.Individual,
                    Role = HolderRole.Owner,
                    FirstName = "Owner",
                    LastName = "Person"
                },
                new()
                {
                    HolderId = Guid.Parse(jointOwner.Id.Value),
                    Type = HolderType.Individual,
                    Role = HolderRole.JointOwner,
                    FirstName = "Joint",
                    LastName = "Owner"
                }
            ]
        };

        _mockCoinhouseRepository
            .Setup(r => r.GetAccountForAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccount);

        _mockPersonaRepository
            .Setup(r => r.Exists(jointOwner.Id))
            .Returns(true);

        // JointOwner attempts to demote the Owner to Viewer
        UpdateCoinhouseAccountHolderRoleCommand command = new(
            Requestor: jointOwner.Id,
            AccountId: accountId,
            CoinhouseTag: coinhouse,
            HolderIdToUpdate: Guid.Parse(owner.Id.Value),
            NewRole: HolderRole.Viewer);

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when trying to change Owner's role");
        Assert.That(result.ErrorMessage, Does.Contain("Owner's role cannot be changed").IgnoreCase);
    }

    [Test]
    public async Task UpdateRole_SameRole_Fails()
    {
        // Given an account with an owner and an authorized user
        CharacterPersona owner = PersonaTestHelpers.CreateCharacterPersona("Owner");
        CharacterPersona authorizedUser = PersonaTestHelpers.CreateCharacterPersona("AuthUser");
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("TEST_BANK");
        Guid accountId = Guid.NewGuid();

        CoinhouseAccountDto existingAccount = new()
        {
            Id = accountId,
            Debit = 1000,
            Credit = 0,
            CoinHouseId = 1L,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Holders =
            [
                new()
                {
                    HolderId = Guid.Parse(owner.Id.Value),
                    Type = HolderType.Individual,
                    Role = HolderRole.Owner,
                    FirstName = "Owner",
                    LastName = "Person"
                },
                new()
                {
                    HolderId = Guid.Parse(authorizedUser.Id.Value),
                    Type = HolderType.Individual,
                    Role = HolderRole.AuthorizedUser,
                    FirstName = "Auth",
                    LastName = "User"
                }
            ]
        };

        _mockCoinhouseRepository
            .Setup(r => r.GetAccountForAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccount);

        _mockPersonaRepository
            .Setup(r => r.Exists(owner.Id))
            .Returns(true);

        // Try to set the same role
        UpdateCoinhouseAccountHolderRoleCommand command = new(
            Requestor: owner.Id,
            AccountId: accountId,
            CoinhouseTag: coinhouse,
            HolderIdToUpdate: Guid.Parse(authorizedUser.Id.Value),
            NewRole: HolderRole.AuthorizedUser);

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when role is unchanged");
        Assert.That(result.ErrorMessage, Does.Contain("already has this role").IgnoreCase);
    }

    [Test]
    public async Task UpdateRole_NoPermission_Fails()
    {
        // Given an account where the requestor is only an AuthorizedUser
        CharacterPersona owner = PersonaTestHelpers.CreateCharacterPersona("Owner");
        CharacterPersona authorizedUser = PersonaTestHelpers.CreateCharacterPersona("AuthUser");
        CharacterPersona viewer = PersonaTestHelpers.CreateCharacterPersona("Viewer");
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("TEST_BANK");
        Guid accountId = Guid.NewGuid();

        CoinhouseAccountDto existingAccount = new()
        {
            Id = accountId,
            Debit = 1000,
            Credit = 0,
            CoinHouseId = 1L,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Holders =
            [
                new()
                {
                    HolderId = Guid.Parse(owner.Id.Value),
                    Type = HolderType.Individual,
                    Role = HolderRole.Owner,
                    FirstName = "Owner",
                    LastName = "Person"
                },
                new()
                {
                    HolderId = Guid.Parse(authorizedUser.Id.Value),
                    Type = HolderType.Individual,
                    Role = HolderRole.AuthorizedUser,
                    FirstName = "Auth",
                    LastName = "User"
                },
                new()
                {
                    HolderId = Guid.Parse(viewer.Id.Value),
                    Type = HolderType.Individual,
                    Role = HolderRole.Viewer,
                    FirstName = "View",
                    LastName = "Person"
                }
            ]
        };

        _mockCoinhouseRepository
            .Setup(r => r.GetAccountForAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccount);

        _mockPersonaRepository
            .Setup(r => r.Exists(authorizedUser.Id))
            .Returns(true);

        // AuthorizedUser tries to change Viewer's role
        UpdateCoinhouseAccountHolderRoleCommand command = new(
            Requestor: authorizedUser.Id,
            AccountId: accountId,
            CoinhouseTag: coinhouse,
            HolderIdToUpdate: Guid.Parse(viewer.Id.Value),
            NewRole: HolderRole.JointOwner);

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "AuthorizedUser should not be able to change roles");
        Assert.That(result.ErrorMessage, Does.Contain("permission").IgnoreCase);
    }

    [Test]
    public async Task UpdateRole_AccountNotFound_Fails()
    {
        // Given a non-existent account
        CharacterPersona requestor = PersonaTestHelpers.CreateCharacterPersona("Requestor");
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("TEST_BANK");
        Guid accountId = Guid.NewGuid();

        _mockCoinhouseRepository
            .Setup(r => r.GetAccountForAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseAccountDto?)null);

        _mockPersonaRepository
            .Setup(r => r.Exists(requestor.Id))
            .Returns(true);

        UpdateCoinhouseAccountHolderRoleCommand command = new(
            Requestor: requestor.Id,
            AccountId: accountId,
            CoinhouseTag: coinhouse,
            HolderIdToUpdate: Guid.NewGuid(),
            NewRole: HolderRole.JointOwner);

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when account doesn't exist");
        Assert.That(result.ErrorMessage, Does.Contain("could not be found").IgnoreCase);
    }

    [Test]
    public async Task UpdateRole_HolderNotFound_Fails()
    {
        // Given an account
        CharacterPersona owner = PersonaTestHelpers.CreateCharacterPersona("Owner");
        CharacterPersona notAHolder = PersonaTestHelpers.CreateCharacterPersona("NotAHolder");
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("TEST_BANK");
        Guid accountId = Guid.NewGuid();

        CoinhouseAccountDto existingAccount = new()
        {
            Id = accountId,
            Debit = 1000,
            Credit = 0,
            CoinHouseId = 1L,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Holders =
            [
                new()
                {
                    HolderId = Guid.Parse(owner.Id.Value),
                    Type = HolderType.Individual,
                    Role = HolderRole.Owner,
                    FirstName = "Owner",
                    LastName = "Person"
                }
            ]
        };

        _mockCoinhouseRepository
            .Setup(r => r.GetAccountForAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccount);

        _mockPersonaRepository
            .Setup(r => r.Exists(owner.Id))
            .Returns(true);

        // Attempt to update role for someone who isn't a holder
        UpdateCoinhouseAccountHolderRoleCommand command = new(
            Requestor: owner.Id,
            AccountId: accountId,
            CoinhouseTag: coinhouse,
            HolderIdToUpdate: Guid.Parse(notAHolder.Id.Value),
            NewRole: HolderRole.JointOwner);

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when holder doesn't exist");
        Assert.That(result.ErrorMessage, Does.Contain("not a member").IgnoreCase);
    }
}

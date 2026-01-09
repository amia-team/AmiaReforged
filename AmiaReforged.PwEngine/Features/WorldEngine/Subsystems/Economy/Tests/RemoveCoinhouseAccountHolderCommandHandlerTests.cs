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
/// Behavior tests for removing holders from coinhouse accounts.
/// Includes sole-owner protection tests.
/// </summary>
[TestFixture]
public class RemoveCoinhouseAccountHolderCommandHandlerTests
{
    private Mock<IPersonaRepository> _mockPersonaRepository = null!;
    private Mock<ICoinhouseRepository> _mockCoinhouseRepository = null!;
    private Mock<IEventBus> _mockEventBus = null!;
    private RemoveCoinhouseAccountHolderCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPersonaRepository = new Mock<IPersonaRepository>();
        _mockCoinhouseRepository = new Mock<ICoinhouseRepository>();
        _mockEventBus = new Mock<IEventBus>();
        _handler = new RemoveCoinhouseAccountHolderCommandHandler(
            _mockPersonaRepository.Object,
            _mockCoinhouseRepository.Object,
            _mockEventBus.Object);
    }

    [Test]
    public async Task RemoveHolder_Success_RemovesHolderFromAccount()
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

        CoinhouseAccountDto? savedAccount = null;
        _mockCoinhouseRepository
            .Setup(r => r.SaveAccountAsync(It.IsAny<CoinhouseAccountDto>(), It.IsAny<CancellationToken>()))
            .Callback<CoinhouseAccountDto, CancellationToken>((a, _) => savedAccount = a)
            .Returns(Task.CompletedTask);

        RemoveCoinhouseAccountHolderCommand command = new(
            Requestor: owner.Id,
            AccountId: accountId,
            CoinhouseTag: coinhouse,
            HolderIdToRemove: Guid.Parse(jointOwner.Id.Value));

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should succeed
        Assert.That(result.Success, Is.True, "Should succeed when removing a non-owner holder");
        Assert.That(savedAccount, Is.Not.Null, "Account should be saved");
        Assert.That(savedAccount!.Holders, Has.Count.EqualTo(1), "Should have one holder remaining");
        Assert.That(savedAccount.Holders.First().Role, Is.EqualTo(HolderRole.Owner), "Remaining holder should be the owner");

        // Verify audit event was published
        _mockEventBus.Verify(
            e => e.PublishAsync(It.IsAny<AccountHolderRemovedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RemoveHolder_SoleOwner_Fails()
    {
        // Given an account with only one owner
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

        // Attempt to remove the sole owner
        RemoveCoinhouseAccountHolderCommand command = new(
            Requestor: owner.Id,
            AccountId: accountId,
            CoinhouseTag: coinhouse,
            HolderIdToRemove: Guid.Parse(owner.Id.Value));

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when removing sole owner");
        Assert.That(result.ErrorMessage, Does.Contain("sole owner").IgnoreCase);

        // Verify no save occurred
        _mockCoinhouseRepository.Verify(
            r => r.SaveAccountAsync(It.IsAny<CoinhouseAccountDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task RemoveHolder_NotAHolder_Fails()
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

        // Attempt to remove someone who isn't a holder
        RemoveCoinhouseAccountHolderCommand command = new(
            Requestor: owner.Id,
            AccountId: accountId,
            CoinhouseTag: coinhouse,
            HolderIdToRemove: Guid.Parse(notAHolder.Id.Value));

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when removing non-existent holder");
        Assert.That(result.ErrorMessage, Does.Contain("not a member").IgnoreCase);
    }

    [Test]
    public async Task RemoveHolder_NoPermission_Fails()
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

        // AuthorizedUser tries to remove Viewer
        RemoveCoinhouseAccountHolderCommand command = new(
            Requestor: authorizedUser.Id,
            AccountId: accountId,
            CoinhouseTag: coinhouse,
            HolderIdToRemove: Guid.Parse(viewer.Id.Value));

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "AuthorizedUser should not be able to remove holders");
        Assert.That(result.ErrorMessage, Does.Contain("permission").IgnoreCase);
    }

    [Test]
    public async Task RemoveHolder_AccountNotFound_Fails()
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

        RemoveCoinhouseAccountHolderCommand command = new(
            Requestor: requestor.Id,
            AccountId: accountId,
            CoinhouseTag: coinhouse,
            HolderIdToRemove: Guid.NewGuid());

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when account doesn't exist");
        Assert.That(result.ErrorMessage, Does.Contain("could not be found").IgnoreCase);
    }
}

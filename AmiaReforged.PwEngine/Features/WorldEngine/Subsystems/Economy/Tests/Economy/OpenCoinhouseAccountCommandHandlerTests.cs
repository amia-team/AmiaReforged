using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.Tests.Helpers.WorldEngine;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Economy;

/// <summary>
/// Behavior tests for opening coinhouse accounts (personal and organizational).
/// </summary>
[TestFixture]
public class OpenCoinhouseAccountCommandHandlerTests
{
    private Mock<ICoinhouseRepository> _mockCoinhouseRepository = null!;
    private Mock<IOrganizationMemberRepository> _mockMemberRepository = null!;
    private Mock<IOrganizationRepository> _mockOrgRepository = null!;
    private OpenCoinhouseAccountCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockCoinhouseRepository = new Mock<ICoinhouseRepository>();
        _mockMemberRepository = new Mock<IOrganizationMemberRepository>();
        _mockOrgRepository = new Mock<IOrganizationRepository>();
        _handler = new OpenCoinhouseAccountCommandHandler(
            _mockCoinhouseRepository.Object,
            _mockMemberRepository.Object,
            _mockOrgRepository.Object);
    }

    [Test]
    public async Task OpenAccount_CoinhouseNotFound_Fails()
    {
        // Given a non-existent coinhouse
        CharacterPersona character = PersonaTestHelpers.CreateCharacterPersona("TestCharacter");
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("NONEXISTENT");

        _mockCoinhouseRepository
            .Setup(r => r.GetByTagAsync(coinhouse, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseDto?)null);

        var command = new OpenCoinhouseAccountCommand(
            Requestor: character.Id,
            AccountPersona: character.Id,
            Coinhouse: coinhouse
        );

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when coinhouse doesn't exist");
        Assert.That(result.ErrorMessage, Does.Contain("could not be found").IgnoreCase);
    }

    [Test]
    public async Task OpenPersonalAccount_RequestorNotOwner_Fails()
    {
        // Given a character trying to open an account for someone else
        CharacterPersona requestor = PersonaTestHelpers.CreateCharacterPersona("Requestor");
        CharacterPersona other = PersonaTestHelpers.CreateCharacterPersona("Other");
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("TEST_BANK");

        var coinhouseDto = new CoinhouseDto
        {
            Id = 1L,
            Tag = coinhouse,
            Settlement = 1,
            EngineId = Guid.NewGuid(),
            Persona = PersonaId.FromCoinhouse(coinhouse)
        };

        _mockCoinhouseRepository
            .Setup(r => r.GetByTagAsync(coinhouse, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coinhouseDto);

        _mockCoinhouseRepository
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseAccountDto?)null);

        var command = new OpenCoinhouseAccountCommand(
            Requestor: requestor.Id,
            AccountPersona: other.Id,
            Coinhouse: coinhouse
        );

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Cannot open account for another character");
        Assert.That(result.ErrorMessage, Does.Contain("Only the character themselves").IgnoreCase);
    }

    [Test]
    public async Task OpenPersonalAccount_Success_CreatesAccountWithOwner()
    {
        // Given a character persona requesting to open their own account
        CharacterPersona character = PersonaTestHelpers.CreateCharacterPersona("TestCharacter");
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("TEST_BANK");

        var coinhouseDto = new CoinhouseDto
        {
            Id = 1L,
            Tag = coinhouse,
            Settlement = 1,
            EngineId = Guid.NewGuid(),
            Persona = PersonaId.FromCoinhouse(coinhouse)
        };

        _mockCoinhouseRepository
            .Setup(r => r.GetByTagAsync(coinhouse, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coinhouseDto);

        _mockCoinhouseRepository
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseAccountDto?)null);

        CoinhouseAccountDto? capturedAccount = null;
        _mockCoinhouseRepository
            .Setup(r => r.SaveAccountAsync(It.IsAny<CoinhouseAccountDto>(), It.IsAny<CancellationToken>()))
            .Callback<CoinhouseAccountDto, CancellationToken>((acc, _) => capturedAccount = acc)
            .Returns(Task.CompletedTask);

        var command = new OpenCoinhouseAccountCommand(
            Requestor: character.Id,
            AccountPersona: character.Id,
            Coinhouse: coinhouse,
            AccountDisplayName: "My Account"
        );

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then the account should be created successfully
        Assert.That(result.Success, Is.True, "Personal account creation should succeed");
        Assert.That(capturedAccount, Is.Not.Null, "Account should be saved");
        Assert.That(capturedAccount!.Holders, Has.Count.EqualTo(1), "Should have one holder");
        Assert.That(capturedAccount.Holders.First().Role, Is.EqualTo(HolderRole.Owner));
        Assert.That(capturedAccount.Holders.First().Type, Is.EqualTo(HolderType.Individual));
    }

    [Test]
    public async Task OpenAccount_AccountAlreadyExists_Fails()
    {
        // Given an existing account for the persona
        CharacterPersona character = PersonaTestHelpers.CreateCharacterPersona("TestCharacter");
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag("TEST_BANK");

        var coinhouseDto = new CoinhouseDto
        {
            Id = 1L,
            Tag = coinhouse,
            Settlement = 1,
            EngineId = Guid.NewGuid(),
            Persona = PersonaId.FromCoinhouse(coinhouse)
        };

        var existingAccount = new CoinhouseAccountDto
        {
            Id = Guid.NewGuid(),
            Debit = 0,
            Credit = 100,
            CoinHouseId = coinhouseDto.Id,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Holders = new List<CoinhouseAccountHolderDto>()
        };

        _mockCoinhouseRepository
            .Setup(r => r.GetByTagAsync(coinhouse, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coinhouseDto);

        _mockCoinhouseRepository
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccount);

        var command = new OpenCoinhouseAccountCommand(
            Requestor: character.Id,
            AccountPersona: character.Id,
            Coinhouse: coinhouse
        );

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when account already exists");
        Assert.That(result.ErrorMessage, Does.Contain("already exists").IgnoreCase);
    }
}

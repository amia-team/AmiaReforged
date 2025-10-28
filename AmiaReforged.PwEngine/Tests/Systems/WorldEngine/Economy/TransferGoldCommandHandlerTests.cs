using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Transactions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy;

/// <summary>
/// Tests for TransferGoldCommandHandler.
/// Uses mocked repository to verify handler logic.
/// </summary>
[TestFixture]
public class TransferGoldCommandHandlerTests
{
    private Mock<ITransactionRepository> _mockRepository = null!;
    private TransferGoldCommandHandler _handler = null!;
    private PersonaId _fromPersona;
    private PersonaId _toPersona;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _handler = new TransferGoldCommandHandler(_mockRepository.Object);
        _fromPersona = PersonaId.FromCharacter(CharacterId.New());
        _toPersona = PersonaId.FromOrganization(OrganizationId.New());
    }

    #region Success Cases

    [Test]
    public async Task HandleAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, Quantity.Parse(100));

        _mockRepository
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken ct) =>
            {
                t.Id = 123; // Simulate database assigning ID
                return t;
            });

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!["transactionId"], Is.EqualTo(123L));
    }

    [Test]
    public async Task HandleAsync_CallsRepository_WithCorrectData()
    {
        // Arrange
        TransferGoldCommand command = new TransferGoldCommand(
            _fromPersona,
            _toPersona,
            Quantity.Parse(250),
            "Test memo");

        Transaction? recordedTransaction = null;
        _mockRepository
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, ct) => recordedTransaction = t)
            .ReturnsAsync((Transaction t, CancellationToken ct) =>
            {
                t.Id = 1;
                return t;
            });

        // Act
        await _handler.HandleAsync(command);

        // Assert
        Assert.That(recordedTransaction, Is.Not.Null);
        Assert.That(recordedTransaction!.FromPersonaId, Is.EqualTo(_fromPersona.ToString()));
        Assert.That(recordedTransaction.ToPersonaId, Is.EqualTo(_toPersona.ToString()));
        Assert.That(recordedTransaction.Amount, Is.EqualTo(250));
        Assert.That(recordedTransaction.Memo, Is.EqualTo("Test memo"));
        Assert.That(recordedTransaction.Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public async Task HandleAsync_WithMemo_StoresMemo()
    {
        // Arrange
        TransferGoldCommand command = new TransferGoldCommand(
            _fromPersona,
            _toPersona,
            Quantity.Parse(100),
            "Guild dues for October");

        Transaction? recordedTransaction = null;
        _mockRepository
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, ct) => recordedTransaction = t)
            .ReturnsAsync((Transaction t, CancellationToken ct) =>
            {
                t.Id = 1;
                return t;
            });

        // Act
        await _handler.HandleAsync(command);

        // Assert
        Assert.That(recordedTransaction!.Memo, Is.EqualTo("Guild dues for October"));
    }

    [Test]
    public async Task HandleAsync_WithoutMemo_StoresNull()
    {
        // Arrange
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, Quantity.Parse(100));

        Transaction? recordedTransaction = null;
        _mockRepository
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, ct) => recordedTransaction = t)
            .ReturnsAsync((Transaction t, CancellationToken ct) =>
            {
                t.Id = 1;
                return t;
            });

        // Act
        await _handler.HandleAsync(command);

        // Assert
        Assert.That(recordedTransaction!.Memo, Is.Null);
    }

    #endregion

    #region Validation Failure Cases

    [Test]
    public async Task HandleAsync_WithZeroAmount_ReturnsFailure()
    {
        // Arrange
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, Quantity.Parse(0));

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Amount must be greater than zero"));
        _mockRepository.Verify(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WithSelfTransfer_ReturnsFailure()
    {
        // Arrange
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _fromPersona, Quantity.Parse(100));

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Cannot transfer to self"));
        _mockRepository.Verify(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WithLongMemo_ReturnsFailure()
    {
        // Arrange
        string longMemo = new string('x', 501);
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, Quantity.Parse(100), longMemo);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Memo cannot exceed 500 characters"));
        _mockRepository.Verify(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Exception Handling

    [Test]
    public async Task HandleAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        // Arrange
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, Quantity.Parse(100));

        _mockRepository
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Failed to record transaction"));
        Assert.That(result.ErrorMessage, Does.Contain("Database connection failed"));
    }

    #endregion

    #region Cross-Persona Transfer Tests

    [Test]
    public async Task HandleAsync_CharacterToOrganization_Succeeds()
    {
        // Arrange
        PersonaId character = PersonaId.FromCharacter(CharacterId.New());
        PersonaId organization = PersonaId.FromOrganization(OrganizationId.New());
        TransferGoldCommand command = new TransferGoldCommand(character, organization, Quantity.Parse(500));

        _mockRepository
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken ct) =>
            {
                t.Id = 1;
                return t;
            });

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task HandleAsync_SystemToCharacter_Succeeds()
    {
        // Arrange
        PersonaId system = PersonaId.FromSystem("QuestRewards");
        PersonaId character = PersonaId.FromCharacter(CharacterId.New());
        TransferGoldCommand command = new TransferGoldCommand(system, character, Quantity.Parse(150), "Dragon quest reward");

        _mockRepository
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken ct) =>
            {
                t.Id = 1;
                return t;
            });

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task HandleAsync_CharacterToCoinhouse_Succeeds()
    {
        // Arrange
        PersonaId character = PersonaId.FromCharacter(CharacterId.New());
        PersonaId coinhouse = PersonaId.FromCoinhouse(new CoinhouseTag("CordorBank"));
        TransferGoldCommand command = new TransferGoldCommand(character, coinhouse, Quantity.Parse(1000), "Bank deposit");

        _mockRepository
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken ct) =>
            {
                t.Id = 1;
                return t;
            });

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    #endregion

    #region Cancellation Token Support

    [Test]
    public async Task HandleAsync_PassesCancellationToken_ToRepository()
    {
        // Arrange
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, Quantity.Parse(100));
        CancellationTokenSource cts = new CancellationTokenSource();
        CancellationToken receivedToken = default;

        _mockRepository
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, ct) => receivedToken = ct)
            .ReturnsAsync((Transaction t, CancellationToken ct) =>
            {
                t.Id = 1;
                return t;
            });

        // Act
        await _handler.HandleAsync(command, cts.Token);

        // Assert
        Assert.That(receivedToken, Is.EqualTo(cts.Token));
    }

    #endregion
}


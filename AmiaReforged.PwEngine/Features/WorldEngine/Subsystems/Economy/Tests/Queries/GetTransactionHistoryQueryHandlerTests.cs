using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Transactions;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Queries;

/// <summary>
/// Tests for GetTransactionHistoryQueryHandler.
/// Verifies transaction history retrieval with pagination and filtering.
/// </summary>
[TestFixture]
public class GetTransactionHistoryQueryHandlerTests
{
    private Mock<ITransactionRepository> _mockRepository = null!;
    private GetTransactionHistoryQueryHandler _handler = null!;
    private PersonaId _testPersonaId;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _handler = new GetTransactionHistoryQueryHandler(_mockRepository.Object);
        _testPersonaId = PersonaId.FromCharacter(CharacterId.From(Guid.NewGuid()));
    }

    [Test]
    public async Task GetHistory_ValidPersona_ReturnsTransactions()
    {
        // Arrange
        var fromPersona = PersonaId.FromCharacter(CharacterId.From(Guid.NewGuid()));
        var toPersona = PersonaId.FromCharacter(CharacterId.From(Guid.NewGuid()));
        
        var transactions = new List<Transaction>
        {
            new() { 
                Id = 1, 
                FromPersonaId = fromPersona.ToString(),
                ToPersonaId = _testPersonaId.ToString(),
                Amount = 100, 
                Memo = "Test 1", 
                Timestamp = DateTime.UtcNow 
            },
            new() { 
                Id = 2, 
                FromPersonaId = _testPersonaId.ToString(),
                ToPersonaId = toPersona.ToString(),
                Amount = 200, 
                Memo = "Test 2", 
                Timestamp = DateTime.UtcNow 
            }
        };

        _mockRepository
            .Setup(r => r.GetHistoryAsync(_testPersonaId, 50, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var query = new GetTransactionHistoryQuery(_testPersonaId);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        _mockRepository.Verify(r => r.GetHistoryAsync(_testPersonaId, 50, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetHistory_EmptyHistory_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetHistoryAsync(_testPersonaId, 50, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Transaction>());

        var query = new GetTransactionHistoryQuery(_testPersonaId);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetHistory_WithCustomPageSize_UsesSpecifiedPageSize()
    {
        // Arrange
        var fromPersona = PersonaId.FromCharacter(CharacterId.From(Guid.NewGuid()));
        
        var transactions = new List<Transaction>
        {
            new() { 
                Id = 1, 
                FromPersonaId = fromPersona.ToString(),
                ToPersonaId = _testPersonaId.ToString(),
                Amount = 100, 
                Memo = "Test", 
                Timestamp = DateTime.UtcNow 
            }
        };

        _mockRepository
            .Setup(r => r.GetHistoryAsync(_testPersonaId, 25, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var query = new GetTransactionHistoryQuery(_testPersonaId, PageSize: 25);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        _mockRepository.Verify(r => r.GetHistoryAsync(_testPersonaId, 25, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetHistory_WithPagination_RequestsCorrectPage()
    {
        // Arrange
        var fromPersona = PersonaId.FromCharacter(CharacterId.From(Guid.NewGuid()));
        
        var transactions = new List<Transaction>
        {
            new() { 
                Id = 3, 
                FromPersonaId = fromPersona.ToString(),
                ToPersonaId = _testPersonaId.ToString(),
                Amount = 300, 
                Memo = "Page 2", 
                Timestamp = DateTime.UtcNow 
            }
        };

        _mockRepository
            .Setup(r => r.GetHistoryAsync(_testPersonaId, 50, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var query = new GetTransactionHistoryQuery(_testPersonaId, Page: 2);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        _mockRepository.Verify(r => r.GetHistoryAsync(_testPersonaId, 50, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void GetHistory_InvalidPageSize_ThrowsArgumentException()
    {
        // Arrange
        var query = new GetTransactionHistoryQuery(_testPersonaId, PageSize: 0);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await _handler.HandleAsync(query, CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("PageSize"));
    }

    [Test]
    public void GetHistory_PageSizeTooLarge_ThrowsArgumentException()
    {
        // Arrange
        var query = new GetTransactionHistoryQuery(_testPersonaId, PageSize: 2000);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await _handler.HandleAsync(query, CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("1000"));
    }

    [Test]
    public void GetHistory_NegativePage_ThrowsArgumentException()
    {
        // Arrange
        var query = new GetTransactionHistoryQuery(_testPersonaId, Page: -1);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await _handler.HandleAsync(query, CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("Page"));
    }
}

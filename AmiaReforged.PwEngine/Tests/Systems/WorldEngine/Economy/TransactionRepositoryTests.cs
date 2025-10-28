using Microsoft.EntityFrameworkCore;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Transactions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy;

/// <summary>
/// Tests for TransactionRepository using EF Core InMemory database.
/// </summary>
[TestFixture]
public class TransactionRepositoryTests
{
    private PwEngineContext _context = null!;
    private TransactionRepository _repository = null!;

    [SetUp]
    public void Setup()
    {
        // Create in-memory database with unique name for each test
        DbContextOptions<PwEngineContext> options = new DbContextOptionsBuilder<PwEngineContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PwEngineContext(options);
        _repository = new TransactionRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region RecordTransactionAsync Tests

    [Test]
    public async Task RecordTransactionAsync_CreatesTransaction()
    {
        // Arrange
        PersonaId from = PersonaId.FromCharacter(CharacterId.New());
        PersonaId to = PersonaId.FromOrganization(OrganizationId.New());
        Transaction transaction = new Transaction
        {
            FromPersonaId = from.ToString(),
            ToPersonaId = to.ToString(),
            Amount = 100,
            Memo = "Test transaction"
        };

        // Act
        Transaction result = await _repository.RecordTransactionAsync(transaction);

        // Assert
        Assert.That(result.Id, Is.GreaterThan(0)); // ID assigned by InMemory DB
        Assert.That(result.FromPersonaId, Is.EqualTo(from.ToString()));
        Assert.That(result.ToPersonaId, Is.EqualTo(to.ToString()));
        Assert.That(result.Amount, Is.EqualTo(100));
    }

    [Test]
    public async Task RecordTransactionAsync_PersistsToDatabase()
    {
        // Arrange
        PersonaId from = PersonaId.FromCharacter(CharacterId.New());
        PersonaId to = PersonaId.FromOrganization(OrganizationId.New());
        Transaction transaction = new Transaction
        {
            FromPersonaId = from.ToString(),
            ToPersonaId = to.ToString(),
            Amount = 200
        };

        // Act
        Transaction recorded = await _repository.RecordTransactionAsync(transaction);

        // Assert - verify it's in the database
        Transaction? retrieved = await _context.Transactions.FindAsync(recorded.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Amount, Is.EqualTo(200));
    }

    #endregion

    #region GetByIdAsync Tests

    [Test]
    public async Task GetByIdAsync_ReturnsTransaction_WhenExists()
    {
        // Arrange
        Transaction transaction = new Transaction
        {
            FromPersonaId = "Character:00000000-0000-0000-0000-000000000001",
            ToPersonaId = "Organization:00000000-0000-0000-0000-000000000002",
            Amount = 100
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        Transaction? result = await _repository.GetByIdAsync(transaction.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(transaction.Id));
        Assert.That(result.Amount, Is.EqualTo(100));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        Transaction? result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region GetHistoryAsync Tests

    [Test]
    public async Task GetHistoryAsync_ReturnsIncomingAndOutgoing()
    {
        // Arrange
        PersonaId persona = PersonaId.FromCharacter(CharacterId.New());
        PersonaId other1 = PersonaId.FromOrganization(OrganizationId.New());
        PersonaId other2 = PersonaId.FromCharacter(CharacterId.New());

        // Outgoing transaction
        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = persona.ToString(),
            ToPersonaId = other1.ToString(),
            Amount = 100,
            Timestamp = DateTime.UtcNow.AddHours(-2)
        });

        // Incoming transaction
        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = other2.ToString(),
            ToPersonaId = persona.ToString(),
            Amount = 50,
            Timestamp = DateTime.UtcNow.AddHours(-1)
        });

        await _context.SaveChangesAsync();

        // Act
        IEnumerable<Transaction> results = await _repository.GetHistoryAsync(persona);

        // Assert
        Assert.That(results.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetHistoryAsync_OrdersByTimestampDescending()
    {
        // Arrange
        PersonaId persona = PersonaId.FromCharacter(CharacterId.New());
        PersonaId other = PersonaId.FromOrganization(OrganizationId.New());

        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = persona.ToString(),
            ToPersonaId = other.ToString(),
            Amount = 100,
            Timestamp = DateTime.UtcNow.AddHours(-3)
        });

        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = other.ToString(),
            ToPersonaId = persona.ToString(),
            Amount = 50,
            Timestamp = DateTime.UtcNow.AddHours(-1)
        });

        await _context.SaveChangesAsync();

        // Act
        List<Transaction> results = (await _repository.GetHistoryAsync(persona)).ToList();

        // Assert - newest first
        Assert.That(results[0].Amount, Is.EqualTo(50));
        Assert.That(results[1].Amount, Is.EqualTo(100));
    }

    [Test]
    public async Task GetHistoryAsync_SupportsPagination()
    {
        // Arrange
        PersonaId persona = PersonaId.FromCharacter(CharacterId.New());
        PersonaId other = PersonaId.FromOrganization(OrganizationId.New());

        // Add 5 transactions
        for (int i = 0; i < 5; i++)
        {
            _context.Transactions.Add(new Transaction
            {
                FromPersonaId = persona.ToString(),
                ToPersonaId = other.ToString(),
                Amount = 100 + i,
                Timestamp = DateTime.UtcNow.AddHours(-i)
            });
        }
        await _context.SaveChangesAsync();

        // Act - get page 0, size 2
        List<Transaction> page0 = (await _repository.GetHistoryAsync(persona, pageSize: 2, page: 0)).ToList();
        List<Transaction> page1 = (await _repository.GetHistoryAsync(persona, pageSize: 2, page: 1)).ToList();

        // Assert
        Assert.That(page0.Count, Is.EqualTo(2));
        Assert.That(page1.Count, Is.EqualTo(2));
        Assert.That(page0[0].Amount, Is.Not.EqualTo(page1[0].Amount)); // Different results
    }

    #endregion

    #region GetOutgoingAsync Tests

    [Test]
    public async Task GetOutgoingAsync_ReturnsOnlyOutgoing()
    {
        // Arrange
        PersonaId persona = PersonaId.FromCharacter(CharacterId.New());
        PersonaId other1 = PersonaId.FromOrganization(OrganizationId.New());
        PersonaId other2 = PersonaId.FromCharacter(CharacterId.New());

        // Outgoing
        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = persona.ToString(),
            ToPersonaId = other1.ToString(),
            Amount = 100
        });

        // Incoming (should not be returned)
        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = other2.ToString(),
            ToPersonaId = persona.ToString(),
            Amount = 50
        });

        await _context.SaveChangesAsync();

        // Act
        List<Transaction> results = (await _repository.GetOutgoingAsync(persona)).ToList();

        // Assert
        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Amount, Is.EqualTo(100));
    }

    #endregion

    #region GetIncomingAsync Tests

    [Test]
    public async Task GetIncomingAsync_ReturnsOnlyIncoming()
    {
        // Arrange
        PersonaId persona = PersonaId.FromCharacter(CharacterId.New());
        PersonaId other1 = PersonaId.FromOrganization(OrganizationId.New());
        PersonaId other2 = PersonaId.FromCharacter(CharacterId.New());

        // Outgoing (should not be returned)
        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = persona.ToString(),
            ToPersonaId = other1.ToString(),
            Amount = 100
        });

        // Incoming
        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = other2.ToString(),
            ToPersonaId = persona.ToString(),
            Amount = 50
        });

        await _context.SaveChangesAsync();

        // Act
        List<Transaction> results = (await _repository.GetIncomingAsync(persona)).ToList();

        // Assert
        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Amount, Is.EqualTo(50));
    }

    #endregion

    #region GetBetweenPersonasAsync Tests

    [Test]
    public async Task GetBetweenPersonasAsync_ReturnsBothDirections()
    {
        // Arrange
        PersonaId persona1 = PersonaId.FromCharacter(CharacterId.New());
        PersonaId persona2 = PersonaId.FromOrganization(OrganizationId.New());
        PersonaId other = PersonaId.FromCharacter(CharacterId.New());

        // persona1 → persona2
        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = persona1.ToString(),
            ToPersonaId = persona2.ToString(),
            Amount = 100
        });

        // persona2 → persona1
        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = persona2.ToString(),
            ToPersonaId = persona1.ToString(),
            Amount = 50
        });

        // Other transaction (should not be returned)
        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = other.ToString(),
            ToPersonaId = persona1.ToString(),
            Amount = 25
        });

        await _context.SaveChangesAsync();

        // Act
        List<Transaction> results = (await _repository.GetBetweenPersonasAsync(persona1, persona2)).ToList();

        // Assert
        Assert.That(results.Count, Is.EqualTo(2));
    }

    #endregion

    #region GetTotalSentAsync Tests

    [Test]
    public async Task GetTotalSentAsync_ReturnsSumOfOutgoing()
    {
        // Arrange
        PersonaId persona = PersonaId.FromCharacter(CharacterId.New());
        PersonaId other1 = PersonaId.FromOrganization(OrganizationId.New());
        PersonaId other2 = PersonaId.FromCharacter(CharacterId.New());

        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = persona.ToString(),
            ToPersonaId = other1.ToString(),
            Amount = 100
        });

        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = persona.ToString(),
            ToPersonaId = other2.ToString(),
            Amount = 50
        });

        // Incoming (should not count)
        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = other1.ToString(),
            ToPersonaId = persona.ToString(),
            Amount = 200
        });

        await _context.SaveChangesAsync();

        // Act
        int total = await _repository.GetTotalSentAsync(persona);

        // Assert
        Assert.That(total, Is.EqualTo(150));
    }

    [Test]
    public async Task GetTotalSentAsync_ReturnsZero_WhenNoTransactions()
    {
        // Arrange
        PersonaId persona = PersonaId.FromCharacter(CharacterId.New());

        // Act
        int total = await _repository.GetTotalSentAsync(persona);

        // Assert
        Assert.That(total, Is.EqualTo(0));
    }

    #endregion

    #region GetTotalReceivedAsync Tests

    [Test]
    public async Task GetTotalReceivedAsync_ReturnsSumOfIncoming()
    {
        // Arrange
        PersonaId persona = PersonaId.FromCharacter(CharacterId.New());
        PersonaId other1 = PersonaId.FromOrganization(OrganizationId.New());
        PersonaId other2 = PersonaId.FromCharacter(CharacterId.New());

        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = other1.ToString(),
            ToPersonaId = persona.ToString(),
            Amount = 100
        });

        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = other2.ToString(),
            ToPersonaId = persona.ToString(),
            Amount = 50
        });

        // Outgoing (should not count)
        _context.Transactions.Add(new Transaction
        {
            FromPersonaId = persona.ToString(),
            ToPersonaId = other1.ToString(),
            Amount = 200
        });

        await _context.SaveChangesAsync();

        // Act
        int total = await _repository.GetTotalReceivedAsync(persona);

        // Assert
        Assert.That(total, Is.EqualTo(150));
    }

    [Test]
    public async Task GetTotalReceivedAsync_ReturnsZero_WhenNoTransactions()
    {
        // Arrange
        PersonaId persona = PersonaId.FromCharacter(CharacterId.New());

        // Act
        int total = await _repository.GetTotalReceivedAsync(persona);

        // Assert
        Assert.That(total, Is.EqualTo(0));
    }

    #endregion
}


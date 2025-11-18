using System;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Treasuries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Treasuries;

internal sealed class InMemoryContextFactory : IDbContextFactory<PwEngineContext>
{
    private readonly DbContextOptions<PwEngineContext> _options;
    public InMemoryContextFactory(DbContextOptions<PwEngineContext> options) => _options = options;
    public PwEngineContext CreateDbContext() => new PwEngineContext(_options);
}

[TestFixture]
public class VaultRepositoryTests
{
    private PwEngineContext _context = null!;
    private VaultRepository _repo = null!;

    [SetUp]
    public void Setup()
    {
        DbContextOptions<PwEngineContext> options = new DbContextOptionsBuilder<PwEngineContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PwEngineContext(options);
        _repo = new VaultRepository(new InMemoryContextFactory(options));
    }

    [TearDown]
    public void Teardown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task GetOrCreateAsync_CreatesWhenMissing()
    {
        Guid owner = Guid.NewGuid();
        string area = "test_area";
        Vault vault = await _repo.GetOrCreateAsync(owner, area);
        Assert.That(vault.Id, Is.GreaterThan(0));
        Assert.That(vault.OwnerCharacterId, Is.EqualTo(owner));
        Assert.That(vault.AreaResRef, Is.EqualTo(area));
        Assert.That(vault.Balance, Is.EqualTo(0));
    }

    [Test]
    public async Task DepositAndWithdraw_Works()
    {
        Guid owner = Guid.NewGuid();
        string area = "test_area";
        int b1 = await _repo.DepositAsync(owner, area, 100);
        Assert.That(b1, Is.EqualTo(100));
        int withdrawn = await _repo.WithdrawAsync(owner, area, 50);
        Assert.That(withdrawn, Is.EqualTo(50));
        int b2 = await _repo.GetBalanceAsync(owner, area);
        Assert.That(b2, Is.EqualTo(50));
    }

    [Test]
    public async Task Withdraw_ClampsToBalance()
    {
        Guid owner = Guid.NewGuid();
        string area = "test_area";
        await _repo.DepositAsync(owner, area, 40);
        int withdrawn = await _repo.WithdrawAsync(owner, area, 1000);
        Assert.That(withdrawn, Is.EqualTo(40));
        int b2 = await _repo.GetBalanceAsync(owner, area);
        Assert.That(b2, Is.EqualTo(0));
    }
}

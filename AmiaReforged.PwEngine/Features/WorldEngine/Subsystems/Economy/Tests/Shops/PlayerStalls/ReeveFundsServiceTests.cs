using System;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Treasuries;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Shops.PlayerStalls;

public class FakeVaultRepository : IVaultRepository
{
    private readonly Dictionary<(Guid, string), int> _balances = new();

    public Task<Vault> GetOrCreateAsync(Guid ownerCharacterId, string areaResRef, CancellationToken ct = default)
    {
        (Guid ownerCharacterId, string areaResRef) key = (ownerCharacterId, areaResRef);
        Vault vault = new Vault
        {
            OwnerCharacterId = ownerCharacterId,
            AreaResRef = areaResRef,
            Balance = _balances.GetValueOrDefault(key, 0),
        };
        return Task.FromResult(vault);
    }

    public Task<int> GetBalanceAsync(Guid ownerCharacterId, string areaResRef, CancellationToken ct = default)
    {
        (Guid ownerCharacterId, string areaResRef) key = (ownerCharacterId, areaResRef);
        return Task.FromResult(_balances.GetValueOrDefault(key, 0));
    }

    public Task<int> DepositAsync(Guid ownerCharacterId, string areaResRef, int amount, CancellationToken ct = default)
    {
        (Guid ownerCharacterId, string areaResRef) key = (ownerCharacterId, areaResRef);
        _balances[key] = _balances.GetValueOrDefault(key, 0) + amount;
        return Task.FromResult(amount);
    }

    public Task<int> WithdrawAsync(Guid ownerCharacterId, string areaResRef, int amount, CancellationToken ct = default)
    {
        (Guid ownerCharacterId, string areaResRef) key = (ownerCharacterId, areaResRef);
        int balance = _balances.GetValueOrDefault(key, 0);
        int withdrawn = Math.Min(balance, amount);
        _balances[key] = balance - withdrawn;
        return Task.FromResult(withdrawn);
    }
}

[TestFixture]
public class ReeveFundsServiceTests
{
    [Test]
    public async Task GetHeldFundsAsync_ReturnsZero_WhenEmpty()
    {
        FakeVaultRepository vaultRepo = new FakeVaultRepository();
        ReeveFundsService svc = new ReeveFundsService(vaultRepo);

        int bal = await svc.GetHeldFundsAsync(PersonaId.FromCharacter(CharacterId.New()), "area1");
        Assert.That(bal, Is.EqualTo(0));
    }

    [Test]
    public async Task ReleaseHeldFundsAsync_WithdrawsAll_WhenRequestedZero()
    {
        FakeVaultRepository vaultRepo = new FakeVaultRepository();
        ReeveFundsService svc = new ReeveFundsService(vaultRepo);
        PersonaId persona = PersonaId.FromCharacter(CharacterId.New());

        // Deposit funds first
        await svc.DepositHeldFundsAsync(persona, "area1", 120, "test deposit");

        int granted = await svc.ReleaseHeldFundsAsync(persona, "area1", 0, async amt => true);
        Assert.That(granted, Is.EqualTo(120));
    }

    [Test]
    public async Task ReleaseHeldFundsAsync_RollsBack_WhenGrantFails()
    {
        FakeVaultRepository vaultRepo = new FakeVaultRepository();
        ReeveFundsService svc = new ReeveFundsService(vaultRepo);
        PersonaId persona = PersonaId.FromCharacter(CharacterId.New());

        // Deposit funds first
        await svc.DepositHeldFundsAsync(persona, "area1", 60, "test deposit");

        int granted = await svc.ReleaseHeldFundsAsync(persona, "area1", 50, async amt => false);
        Assert.That(granted, Is.EqualTo(0));

        int bal = await svc.GetHeldFundsAsync(persona, "area1");
        Assert.That(bal, Is.EqualTo(60));
    }
}


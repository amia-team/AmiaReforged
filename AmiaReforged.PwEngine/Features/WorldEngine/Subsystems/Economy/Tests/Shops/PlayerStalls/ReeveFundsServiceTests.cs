using System;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Treasuries;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Shops.PlayerStalls;

public class FakeVaults : IVaultRepository
{
    private readonly Dictionary<(Guid, string), int> _balances = new();

    public Task<Database.Entities.Economy.Treasuries.Vault> GetOrCreateAsync(Guid ownerCharacterId, string areaResRef, System.Threading.CancellationToken ct = default)
        => Task.FromResult(new Database.Entities.Economy.Treasuries.Vault { Id = 1, OwnerCharacterId = ownerCharacterId, AreaResRef = areaResRef, Balance = _balances.GetValueOrDefault((ownerCharacterId, areaResRef), 0) });

    public Task<int> GetBalanceAsync(Guid ownerCharacterId, string areaResRef, System.Threading.CancellationToken ct = default)
        => Task.FromResult(_balances.GetValueOrDefault((ownerCharacterId, areaResRef), 0));

    public Task<int> DepositAsync(Guid ownerCharacterId, string areaResRef, int amount, System.Threading.CancellationToken ct = default)
    {
        var key = (ownerCharacterId, areaResRef);
        _balances[key] = _balances.GetValueOrDefault(key, 0) + amount;
        return Task.FromResult(_balances[key]);
    }

    public Task<int> WithdrawAsync(Guid ownerCharacterId, string areaResRef, int amount, System.Threading.CancellationToken ct = default)
    {
        var key = (ownerCharacterId, areaResRef);
        int bal = _balances.GetValueOrDefault(key, 0);
        int take = Math.Min(bal, amount);
        _balances[key] = bal - take;
        return Task.FromResult(take);
    }
}

[TestFixture]
public class ReeveFundsServiceTests
{
    [Test]
    public async Task GetHeldFundsAsync_ReturnsZero_WhenEmpty()
    {
        var svc = new ReeveFundsService(new VaultService(new FakeVaults()));
        int bal = await svc.GetHeldFundsAsync(PersonaId.FromCharacter(CharacterId.New()), "area1");
        Assert.That(bal, Is.EqualTo(0));
    }

    [Test]
    public async Task ReleaseHeldFundsAsync_WithdrawsAll_WhenRequestedZero()
    {
        var fakeRepo = new FakeVaults();
        var svc = new ReeveFundsService(new VaultService(fakeRepo));
        var persona = PersonaId.FromCharacter(CharacterId.New());
        await fakeRepo.DepositAsync(PersonaId.ToGuid(persona), "area1", 120);
        int granted = await svc.ReleaseHeldFundsAsync(persona, "area1", 0, async amt => true);
        Assert.That(granted, Is.EqualTo(120));
    }

    [Test]
    public async Task ReleaseHeldFundsAsync_RollsBack_WhenGrantFails()
    {
        var fakeRepo = new FakeVaults();
        var svc = new ReeveFundsService(new VaultService(fakeRepo));
        var persona = PersonaId.FromCharacter(CharacterId.New());
        await fakeRepo.DepositAsync(PersonaId.ToGuid(persona), "area1", 60);
        int granted = await svc.ReleaseHeldFundsAsync(persona, "area1", 50, async amt => false);
        Assert.That(granted, Is.EqualTo(0));
        int bal = await fakeRepo.GetBalanceAsync(PersonaId.ToGuid(persona), "area1");
        Assert.That(bal, Is.EqualTo(60));
    }
}


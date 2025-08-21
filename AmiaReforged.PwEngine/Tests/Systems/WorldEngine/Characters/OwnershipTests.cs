using AmiaReforged.PwEngine.Systems.WorldEngine.Features.Characters;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.Characters.Entities;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Characters;

[TestFixture]
public class OwnershipTests
{
    [Test]
    public void World_IsWorld_AndHasNoIds()
    {
        Ownership o = Ownership.World();

        Assert.That(o.IsWorld, Is.True);
        Assert.That(o.IsDm, Is.False);
        Assert.That(o.IsPlayer, Is.False);
        Assert.That(o.DmId, Is.Null);
        Assert.That(o.PlayerId, Is.Null);
        Assert.That(o.Kind, Is.EqualTo(OwnershipKind.World));
    }

    [Test]
    public void DungeonMaster_IsDm_AndHoldsDmId()
    {
        DmId dm = new DmId("DM-KEY-123");
        Ownership o = Ownership.DungeonMaster(dm);

        Assert.That(o.IsDm, Is.True);
        Assert.That(o.IsWorld, Is.False);
        Assert.That(o.IsPlayer, Is.False);
        Assert.That(o.DmId, Is.EqualTo(dm));
        Assert.That(o.PlayerId, Is.Null);
        Assert.That(o.Kind, Is.EqualTo(OwnershipKind.DungeonMaster));
    }

    [Test]
    public void Player_IsPlayer_AndHoldsPlayerId()
    {
        PlayerId player = new PlayerId("PLAYER-KEY-ABC");
        Ownership o = Ownership.Player(player);

        Assert.That(o.IsPlayer, Is.True);
        Assert.That(o.IsWorld, Is.False);
        Assert.That(o.IsDm, Is.False);
        Assert.That(o.PlayerId, Is.EqualTo(player));
        Assert.That(o.DmId, Is.Null);
        Assert.That(o.Kind, Is.EqualTo(OwnershipKind.Player));
    }
}

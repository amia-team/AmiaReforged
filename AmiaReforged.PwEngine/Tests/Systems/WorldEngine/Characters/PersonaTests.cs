using AmiaReforged.PwEngine.Systems.WorldEngine.Features.Characters.Entities;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Characters;

[TestFixture]
public class PersonaTests
{
    [Test]
    public void Create_WithKey_SetsId_NormalizesKey_InitializesState()
    {
        Persona p = Persona.Create("  abcd1234  ");

        Assert.That(p.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(p.Key, Is.EqualTo("ABCD1234"));
        Assert.That(p.Unlocks, Is.Empty);
        Assert.That(p.LastUpdated, Is.InRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1)));
    }

    [Test]
    public void Create_Allows_EmptyKey_ForWorldOwnedPersona()
    {
        // World personas may not have a player/DM key; empty should be allowed and normalized to empty string.
        Persona p = Persona.Create("   ");

        Assert.That(p.Key, Is.EqualTo(string.Empty));
        Assert.That(p.Unlocks, Is.Empty);
        Assert.That(p.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void HasUnlock_IsCaseInsensitive()
    {
        Persona p = Persona.Create("user-key");
        p.GrantUnlock("Perk.FastTravel");

        Assert.That(p.HasUnlock("perk.fasttravel"), Is.True);
        Assert.That(p.HasUnlock("PERK.FASTTRAVEL"), Is.True);
        Assert.That(p.HasUnlock("other"), Is.False);
    }

    [Test]
    public void GrantUnlock_AddsUnlock_TouchesLastUpdated_IdempotentOnDuplicate()
    {
        Persona p = Persona.Create("user-key");
        DateTime before = p.LastUpdated;

        bool added = p.GrantUnlock("skin-1");
        Assert.That(added, Is.True);
        Assert.That(p.HasUnlock("SKIN-1"), Is.True);
        DateTime afterFirst = p.LastUpdated;
        Assert.That(afterFirst, Is.GreaterThanOrEqualTo(before));

        bool addedAgain = p.GrantUnlock("skin-1");
        Assert.That(addedAgain, Is.False);
        Assert.That(p.LastUpdated, Is.EqualTo(afterFirst));
    }

    [Test]
    public void RevokeUnlock_RemovesUnlock_TouchesLastUpdated_IdempotentOnMissing()
    {
        Persona p = Persona.Create("user-key");
        p.GrantUnlock("skin-1");
        DateTime before = p.LastUpdated;

        bool removed = p.RevokeUnlock("skin-1");
        Assert.That(removed, Is.True);
        Assert.That(p.HasUnlock("skin-1"), Is.False);
        DateTime afterFirst = p.LastUpdated;
        Assert.That(afterFirst, Is.GreaterThanOrEqualTo(before));

        bool removedAgain = p.RevokeUnlock("skin-1");
        Assert.That(removedAgain, Is.False);
        Assert.That(p.LastUpdated, Is.EqualTo(afterFirst));
    }

    [Test]
    public void UnlockOperations_IgnoreNullOrWhitespace()
    {
        Persona p = Persona.Create("user-key");
        DateTime before = p.LastUpdated;

        Assert.That(p.GrantUnlock(null!), Is.False);
        Assert.That(p.GrantUnlock(" "), Is.False);
        Assert.That(p.RevokeUnlock(null!), Is.False);
        Assert.That(p.RevokeUnlock(" "), Is.False);

        // No changes to unlocks or LastUpdated
        Assert.That(p.Unlocks, Is.Empty);
        Assert.That(p.LastUpdated, Is.EqualTo(before));
    }
}

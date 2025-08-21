using AmiaReforged.PwEngine.Systems.WorldEngine.Features.Characters.Entities;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Characters;

[TestFixture]
public class CharacterTests
{
    [Test]
    public void Create_SetsFields_TrimsAndNormalizesName()
    {
        Guid personaId = Guid.NewGuid();
        Character c = Character.Create(personaId, "  Lliara Nightbreeze ");

        Assert.That(c.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(c.PersonaId, Is.EqualTo(personaId));
        Assert.That(c.Name, Is.EqualTo("Lliara Nightbreeze"));
        Assert.That(c.NameNormalized, Is.EqualTo("LLIARA NIGHTBREEZE"));
        Assert.That(c.Status, Is.EqualTo(CharacterStatus.Active));
        Assert.That(c.RetiredUtc, Is.Null);
        Assert.That(c.CreatedUtc, Is.InRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1)));
        Assert.That(c.LastUpdated, Is.EqualTo(c.CreatedUtc));
    }

    [Test]
    public void Create_Throws_OnInvalidInputs()
    {
        Assert.Throws<ArgumentException>(() => Character.Create(Guid.Empty, "Name"));
        Assert.Throws<ArgumentException>(() => Character.Create(Guid.NewGuid(), " "));
    }

    [Test]
    public void Rename_UpdatesName_Normalized_TouchesLastUpdated()
    {
        Character c = Character.Create(Guid.NewGuid(), "Old Name");
        DateTime before = c.LastUpdated;

        c.Rename(" New Name ");

        Assert.That(c.Name, Is.EqualTo("New Name"));
        Assert.That(c.NameNormalized, Is.EqualTo("NEW NAME"));
        Assert.That(c.LastUpdated, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void Rename_Throws_WhenCharacterNotActive()
    {
        Character c = Character.Create(Guid.NewGuid(), "Old Name");
        c.Retire();

        Assert.Throws<InvalidOperationException>(() => c.Rename("Another"));
    }

    [Test]
    public void Retire_And_Reinstate_AreIdempotent_AndTouchLastUpdatedOnStateChange()
    {
        Character c = Character.Create(Guid.NewGuid(), "Hero");
        DateTime before = c.LastUpdated;

        c.Retire();
        Assert.That(c.Status, Is.EqualTo(CharacterStatus.Retired));
        Assert.That(c.RetiredUtc, Is.Not.Null);
        DateTime afterRetire = c.LastUpdated;
        Assert.That(afterRetire, Is.GreaterThanOrEqualTo(before));

        c.Retire(); // idempotent
        Assert.That(c.LastUpdated, Is.EqualTo(afterRetire));

        c.Reinstate();
        Assert.That(c.Status, Is.EqualTo(CharacterStatus.Active));
        Assert.That(c.RetiredUtc, Is.Null);
        DateTime afterReinstate = c.LastUpdated;
        Assert.That(afterReinstate, Is.GreaterThanOrEqualTo(afterRetire));

        c.Reinstate(); // idempotent
        Assert.That(c.LastUpdated, Is.EqualTo(afterReinstate));
    }

    [Test]
    public void TransferToPersona_UpdatesOwner_TouchesLastUpdated_IdempotentOnSameId()
    {
        Guid originalPersonaId = Guid.NewGuid();
        Character c = Character.Create(originalPersonaId, "Rogue");
        DateTime before = c.LastUpdated;

        Guid newPersonaId = Guid.NewGuid();
        c.TransferToPersona(newPersonaId);

        Assert.That(c.PersonaId, Is.EqualTo(newPersonaId));
        DateTime afterTransfer = c.LastUpdated;
        Assert.That(afterTransfer, Is.GreaterThanOrEqualTo(before));

        c.TransferToPersona(newPersonaId); // idempotent when unchanged
        Assert.That(c.LastUpdated, Is.EqualTo(afterTransfer));
    }

    [Test]
    public void TransferToPersona_Throws_OnEmptyGuid()
    {
        Character c = Character.Create(Guid.NewGuid(), "Mage");
        Assert.Throws<ArgumentException>(() => c.TransferToPersona(Guid.Empty));
    }
}

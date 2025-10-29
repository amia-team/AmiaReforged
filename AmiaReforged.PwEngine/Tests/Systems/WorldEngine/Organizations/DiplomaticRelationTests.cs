using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Organizations;

[TestFixture]
public class DiplomaticRelationTests
{
    private OrganizationId _org1Id;
    private OrganizationId _org2Id;
    private DiplomaticRelation _relation = null!;

    [SetUp]
    public void SetUp()
    {
        _org1Id = OrganizationId.New();
        _org2Id = OrganizationId.New();

        _relation = new DiplomaticRelation
        {
            Id = Guid.NewGuid(),
            SourceOrganizationId = _org1Id,
            TargetOrganizationId = _org2Id,
            Stance = DiplomaticStance.Neutral,
            EstablishedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow,
            Treaties = []
        };
    }

    [Test]
    public void DiplomaticRelation_CanBeCreated()
    {
        // Assert
        Assert.That(_relation.SourceOrganizationId, Is.EqualTo(_org1Id));
        Assert.That(_relation.TargetOrganizationId, Is.EqualTo(_org2Id));
        Assert.That(_relation.Stance, Is.EqualTo(DiplomaticStance.Neutral));
    }

    [Test]
    public void DiplomaticRelation_AlliedIsPositive()
    {
        // Arrange
        _relation.Stance = DiplomaticStance.Allied;

        // Assert
        Assert.That(_relation.IsPositive(), Is.True);
        Assert.That(_relation.IsNegative(), Is.False);
    }

    [Test]
    public void DiplomaticRelation_RivalIsNegative()
    {
        // Arrange
        _relation.Stance = DiplomaticStance.Rival;

        // Assert
        Assert.That(_relation.IsNegative(), Is.True);
        Assert.That(_relation.IsPositive(), Is.False);
    }

    [Test]
    public void DiplomaticRelation_NeutralIsNeitherPositiveNorNegative()
    {
        // Arrange
        _relation.Stance = DiplomaticStance.Neutral;

        // Assert
        Assert.That(_relation.IsPositive(), Is.False);
        Assert.That(_relation.IsNegative(), Is.False);
    }

    [Test]
    public void DiplomaticRelation_WarDetected()
    {
        // Arrange
        _relation.Stance = DiplomaticStance.War;

        // Assert
        Assert.That(_relation.AtWar(), Is.True);
        Assert.That(_relation.IsNegative(), Is.True);
    }

    [Test]
    public void DiplomaticRelation_CanHaveTreaties()
    {
        // Arrange
        _relation.Treaties.Add("Trade Agreement");
        _relation.Treaties.Add("Mutual Defense Pact");

        // Assert
        Assert.That(_relation.Treaties, Has.Count.EqualTo(2));
        Assert.That(_relation.Treaties, Contains.Item("Trade Agreement"));
    }

    [Test]
    public void DiplomaticStance_HasProgression()
    {
        // Assert - verify stances have logical ordering
        Assert.That(DiplomaticStance.Allied > DiplomaticStance.Friendly, Is.True);
        Assert.That(DiplomaticStance.Friendly > DiplomaticStance.Neutral, Is.True);
        Assert.That(DiplomaticStance.Neutral > DiplomaticStance.Unfriendly, Is.True);
        Assert.That(DiplomaticStance.Unfriendly > DiplomaticStance.Rival, Is.True);
        Assert.That(DiplomaticStance.Rival > DiplomaticStance.Hostile, Is.True);
        Assert.That(DiplomaticStance.Hostile > DiplomaticStance.War, Is.True);
    }

    [Test]
    public void DiplomaticRelation_CanChangeStance()
    {
        // Arrange
        _relation.Stance = DiplomaticStance.Friendly;
        DateTime originalDate = _relation.LastModifiedDate;

        // Act
        Thread.Sleep(10); // Ensure time difference
        _relation.Stance = DiplomaticStance.Allied;
        _relation.LastModifiedDate = DateTime.UtcNow;

        // Assert
        Assert.That(_relation.Stance, Is.EqualTo(DiplomaticStance.Allied));
        Assert.That(_relation.LastModifiedDate, Is.GreaterThan(originalDate));
    }
}


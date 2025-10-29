using FluentAssertions;
using NUnit.Framework;

namespace WorldSimulator.Tests.Domain.ValueObjects;

/// <summary>
/// Tests for strongly-typed ID value objects.
/// Demonstrates Parse, Don't Validate pattern.
/// </summary>
[TestFixture]
public class IdentifierTests
{
    [Test]
    public void GovernmentId_Parse_ValidGuid_ReturnsGovernmentId()
    {
        // Arrange
        Guid guid = Guid.NewGuid();
        string input = guid.ToString();

        // Act
        GovernmentId result = GovernmentId.Parse(input);

        // Assert
        result.Value.Should().Be(guid);
    }

    [Test]
    public void GovernmentId_Parse_InvalidGuid_ThrowsFormatException()
    {
        // Arrange
        string input = "not-a-guid";

        // Act & Assert
        Func<GovernmentId> act = () => GovernmentId.Parse(input);
        act.Should().Throw<FormatException>()
            .WithMessage("*Invalid GovernmentId format*");
    }

    [Test]
    public void GovernmentId_Constructor_EmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        Func<GovernmentId> act = () => new GovernmentId(Guid.Empty);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*GovernmentId cannot be empty*");
    }

    [Test]
    public void GovernmentId_New_ReturnsUniqueIds()
    {
        // Act
        GovernmentId id1 = GovernmentId.New();
        GovernmentId id2 = GovernmentId.New();

        // Assert
        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(Guid.Empty);
        id2.Value.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void ItemId_Parse_ValidResRef_ReturnsItemId()
    {
        // Arrange
        string resRef = "nw_wswdg001";

        // Act
        ItemId result = ItemId.Parse(resRef);

        // Assert
        result.ResRef.Should().Be("nw_wswdg001");
    }

    [Test]
    public void ItemId_Parse_MixedCase_ConvertsToLowerCase()
    {
        // Arrange
        string resRef = "NW_WSWDG001";

        // Act
        ItemId result = ItemId.Parse(resRef);

        // Assert
        result.ResRef.Should().Be("nw_wswdg001");
    }

    [Test]
    public void ItemId_Constructor_TooLong_ThrowsArgumentException()
    {
        // Arrange
        string resRef = "this_is_way_too_long_for_nwn";

        // Act & Assert
        Func<ItemId> act = () => new ItemId(resRef);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot exceed 16 characters*");
    }

    [Test]
    public void ItemId_Constructor_Empty_ThrowsArgumentException()
    {
        // Act & Assert
        Func<ItemId> act = () => new ItemId("");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ResRef cannot be empty*");
    }

    [Test]
    public void DifferentIdTypes_CannotBeCompared()
    {
        // This test demonstrates compile-time type safety
        // The following code would NOT compile:
        // var govId = GovernmentId.New();
        // var settlementId = SettlementId.New();
        // if (govId == settlementId) { } // ❌ Compiler error!

        // This is a benefit of strongly-typed IDs
        Assert.Pass("Type safety is enforced at compile-time");
    }
}


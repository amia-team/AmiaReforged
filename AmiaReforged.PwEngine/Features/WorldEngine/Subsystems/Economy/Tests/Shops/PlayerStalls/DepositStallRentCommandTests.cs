using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Shops.PlayerStalls;

/// <summary>
/// Tests for DepositStallRentCommand validation and creation.
/// </summary>
[TestFixture]
public class DepositStallRentCommandTests
{
    [Test]
    public void Create_WithValidParameters_CreatesCommand()
    {
        long stallId = 42;
        int depositAmount = 5000;
        string depositorPersonaId = "Character:test-guid";
        string depositorDisplayName = "Aria Moonwhisper";
        DateTime timestamp = DateTime.UtcNow;

        DepositStallRentCommand command = DepositStallRentCommand.Create(
            stallId,
            depositAmount,
            depositorPersonaId,
            depositorDisplayName,
            timestamp);

        Assert.Multiple(() =>
        {
            Assert.That(command.StallId, Is.EqualTo(stallId));
            Assert.That(command.DepositAmount, Is.EqualTo(depositAmount));
            Assert.That(command.DepositorPersonaId, Is.EqualTo(depositorPersonaId));
            Assert.That(command.DepositorDisplayName, Is.EqualTo(depositorDisplayName));
            Assert.That(command.DepositTimestamp, Is.EqualTo(timestamp));
        });
    }

    [Test]
    public void Create_WithZeroStallId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DepositStallRentCommand.Create(
                0,
                1000,
                "Character:test",
                "Test User",
                DateTime.UtcNow));

        Assert.That(ex!.Message, Does.Contain("Stall ID must be positive"));
        Assert.That(ex.ParamName, Is.EqualTo("stallId"));
    }

    [Test]
    public void Create_WithNegativeStallId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DepositStallRentCommand.Create(
                -1,
                1000,
                "Character:test",
                "Test User",
                DateTime.UtcNow));

        Assert.That(ex!.Message, Does.Contain("Stall ID must be positive"));
        Assert.That(ex.ParamName, Is.EqualTo("stallId"));
    }

    [Test]
    public void Create_WithZeroDepositAmount_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DepositStallRentCommand.Create(
                42,
                0,
                "Character:test",
                "Test User",
                DateTime.UtcNow));

        Assert.That(ex!.Message, Does.Contain("Deposit amount must be positive"));
        Assert.That(ex.ParamName, Is.EqualTo("depositAmount"));
    }

    [Test]
    public void Create_WithNegativeDepositAmount_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DepositStallRentCommand.Create(
                42,
                -500,
                "Character:test",
                "Test User",
                DateTime.UtcNow));

        Assert.That(ex!.Message, Does.Contain("Deposit amount must be positive"));
        Assert.That(ex.ParamName, Is.EqualTo("depositAmount"));
    }

    [Test]
    public void Create_WithNullDepositorPersonaId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DepositStallRentCommand.Create(
                42,
                1000,
                null!,
                "Test User",
                DateTime.UtcNow));

        Assert.That(ex!.Message, Does.Contain("Depositor persona ID is required"));
        Assert.That(ex.ParamName, Is.EqualTo("depositorPersonaId"));
    }

    [Test]
    public void Create_WithEmptyDepositorPersonaId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DepositStallRentCommand.Create(
                42,
                1000,
                "",
                "Test User",
                DateTime.UtcNow));

        Assert.That(ex!.Message, Does.Contain("Depositor persona ID is required"));
        Assert.That(ex.ParamName, Is.EqualTo("depositorPersonaId"));
    }

    [Test]
    public void Create_WithWhitespaceDepositorPersonaId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DepositStallRentCommand.Create(
                42,
                1000,
                "   ",
                "Test User",
                DateTime.UtcNow));

        Assert.That(ex!.Message, Does.Contain("Depositor persona ID is required"));
        Assert.That(ex.ParamName, Is.EqualTo("depositorPersonaId"));
    }

    [Test]
    public void Create_WithNullDepositorDisplayName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DepositStallRentCommand.Create(
                42,
                1000,
                "Character:test",
                null!,
                DateTime.UtcNow));

        Assert.That(ex!.Message, Does.Contain("Depositor display name is required"));
        Assert.That(ex.ParamName, Is.EqualTo("depositorDisplayName"));
    }

    [Test]
    public void Create_WithEmptyDepositorDisplayName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DepositStallRentCommand.Create(
                42,
                1000,
                "Character:test",
                "",
                DateTime.UtcNow));

        Assert.That(ex!.Message, Does.Contain("Depositor display name is required"));
        Assert.That(ex.ParamName, Is.EqualTo("depositorDisplayName"));
    }

    [Test]
    public void Create_WithWhitespaceDepositorDisplayName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DepositStallRentCommand.Create(
                42,
                1000,
                "Character:test",
                "   ",
                DateTime.UtcNow));

        Assert.That(ex!.Message, Does.Contain("Depositor display name is required"));
        Assert.That(ex.ParamName, Is.EqualTo("depositorDisplayName"));
    }

    [Test]
    public void Create_WithMaxIntDepositAmount_CreatesCommand()
    {
        DepositStallRentCommand command = DepositStallRentCommand.Create(
            42,
            int.MaxValue,
            "Character:test",
            "Test User",
            DateTime.UtcNow);

        Assert.That(command.DepositAmount, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void Create_WithLargeStallId_CreatesCommand()
    {
        long largeStallId = long.MaxValue;

        DepositStallRentCommand command = DepositStallRentCommand.Create(
            largeStallId,
            1000,
            "Character:test",
            "Test User",
            DateTime.UtcNow);

        Assert.That(command.StallId, Is.EqualTo(largeStallId));
    }
}


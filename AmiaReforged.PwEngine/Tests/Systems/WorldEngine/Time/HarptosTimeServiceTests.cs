using System;
using AmiaReforged.PwEngine.Features.WorldEngine.Time;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Time;

[TestFixture]
public class HarptosTimeServiceTests
{
    private readonly HarptosTimeService _service = new();

    [Test]
    public void Convert_JanuaryFirst2025_ReturnsHammerOne()
    {
        DateTimeOffset real = new(2025, 1, 1, 12, 34, 0, TimeSpan.Zero);

        HarptosDateTime result = _service.Convert(real);

        result.Year.Should().Be(1393);
        result.IsFestival.Should().BeFalse();
        result.Month.Should().Be(HarptosMonth.Hammer);
        result.Day.Should().Be(1);
        result.TimeOfDay.Should().Be(new TimeOnly(12, 34));
    }

    [Test]
    public void Convert_JanuaryThirtyFirst2025_ReturnsMidwinter()
    {
        DateTimeOffset real = new(2025, 1, 31, 6, 0, 0, TimeSpan.Zero);

        HarptosDateTime result = _service.Convert(real);

        result.Year.Should().Be(1393);
        result.IsFestival.Should().BeTrue();
        result.Festival.Should().Be(HarptosFestival.Midwinter);
        result.Month.Should().BeNull();
        result.Day.Should().BeNull();
    }

    [Test]
    public void Convert_AugustFirst2025_ReturnsMidsummer()
    {
        DateTimeOffset real = new(2025, 8, 1, 21, 15, 0, TimeSpan.Zero);

        HarptosDateTime result = _service.Convert(real);

        result.Festival.Should().Be(HarptosFestival.Midsummer);
        result.Year.Should().Be(1393);
    }

    [Test]
    public void Convert_MaySecond2025_ReturnsGreengrass()
    {
        DateTimeOffset real = new(2025, 5, 2, 10, 0, 0, TimeSpan.Zero);

        HarptosDateTime result = _service.Convert(real);

        result.Festival.Should().Be(HarptosFestival.Greengrass);
        result.Year.Should().Be(1393);
    }

    [Test]
    public void Convert_AugustSecond2025_ReturnsEleasisFirst()
    {
        DateTimeOffset real = new(2025, 8, 2, 0, 0, 0, TimeSpan.Zero);

        HarptosDateTime result = _service.Convert(real);

        result.Month.Should().Be(HarptosMonth.Eleasis);
        result.Day.Should().Be(1);
        result.Festival.Should().BeNull();
    }

    [Test]
    public void Convert_AugustFirst2024_ReturnsShieldmeet()
    {
        DateTimeOffset real = new(2024, 8, 1, 8, 45, 0, TimeSpan.Zero);

        HarptosDateTime result = _service.Convert(real);

        result.Year.Should().Be(1392);
        result.Festival.Should().Be(HarptosFestival.Shieldmeet);
        result.Month.Should().BeNull();
    }

    [Test]
    public void ToDisplayString_FormatsFestivalCorrectly()
    {
        DateTimeOffset real = new(2025, 9, 21, 9, 0, 0, TimeSpan.Zero);

        HarptosDateTime result = _service.Convert(real);

        result.Festival.Should().BeNull(); // Ensure we landed on a month day
        result.ToDisplayString().Should().Be("Eleint 21, 1393 DR @ 09:00");
    }
}

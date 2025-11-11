using System;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Time;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Time;

[TestFixture]
public sealed class TimeOfDayNamesTests
{
    [Test]
    [TestCase(5, 0, "Dawn")]
    [TestCase(5, 30, "Dawn")]
    [TestCase(5, 59, "Dawn")]
    public void GetTimeOfDayName_DawnHours_ReturnsDawn(int hour, int minute, string expected)
    {
        // Arrange
        var time = new TimeOnly(hour, minute);

        // Act
        string result = TimeOfDayNames.GetTimeOfDayName(time);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase(6, 0, "Morning")]
    [TestCase(8, 30, "Morning")]
    [TestCase(10, 45, "Morning")]
    [TestCase(11, 59, "Morning")]
    public void GetTimeOfDayName_MorningHours_ReturnsMorning(int hour, int minute, string expected)
    {
        // Arrange
        var time = new TimeOnly(hour, minute);

        // Act
        string result = TimeOfDayNames.GetTimeOfDayName(time);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase(12, 0, "Highsun")]
    [TestCase(12, 30, "Highsun")]
    [TestCase(12, 59, "Highsun")]
    public void GetTimeOfDayName_NoonHours_ReturnsHighsun(int hour, int minute, string expected)
    {
        // Arrange
        var time = new TimeOnly(hour, minute);

        // Act
        string result = TimeOfDayNames.GetTimeOfDayName(time);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase(13, 0, "Afternoon")]
    [TestCase(14, 30, "Afternoon")]
    [TestCase(15, 45, "Afternoon")]
    [TestCase(16, 59, "Afternoon")]
    public void GetTimeOfDayName_AfternoonHours_ReturnsAfternoon(int hour, int minute, string expected)
    {
        // Arrange
        var time = new TimeOnly(hour, minute);

        // Act
        string result = TimeOfDayNames.GetTimeOfDayName(time);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase(17, 0, "Dusk")]
    [TestCase(17, 30, "Dusk")]
    [TestCase(17, 59, "Dusk")]
    public void GetTimeOfDayName_DuskHours_ReturnsDusk(int hour, int minute, string expected)
    {
        // Arrange
        var time = new TimeOnly(hour, minute);

        // Act
        string result = TimeOfDayNames.GetTimeOfDayName(time);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase(18, 0, "Sunset")]
    [TestCase(18, 30, "Sunset")]
    [TestCase(18, 59, "Sunset")]
    public void GetTimeOfDayName_SunsetHours_ReturnsSunset(int hour, int minute, string expected)
    {
        // Arrange
        var time = new TimeOnly(hour, minute);

        // Act
        string result = TimeOfDayNames.GetTimeOfDayName(time);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase(19, 0, "Evening")]
    [TestCase(20, 30, "Evening")]
    [TestCase(22, 45, "Evening")]
    [TestCase(23, 59, "Evening")]
    public void GetTimeOfDayName_EveningHours_ReturnsEvening(int hour, int minute, string expected)
    {
        // Arrange
        var time = new TimeOnly(hour, minute);

        // Act
        string result = TimeOfDayNames.GetTimeOfDayName(time);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase(0, 0, "Midnight")]
    [TestCase(0, 30, "Midnight")]
    [TestCase(0, 59, "Midnight")]
    public void GetTimeOfDayName_MidnightHours_ReturnsMidnight(int hour, int minute, string expected)
    {
        // Arrange
        var time = new TimeOnly(hour, minute);

        // Act
        string result = TimeOfDayNames.GetTimeOfDayName(time);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase(1, 0, "Moondark")]
    [TestCase(1, 30, "Moondark")]
    [TestCase(2, 0, "Moondark")]
    [TestCase(2, 59, "Moondark")]
    public void GetTimeOfDayName_MoondarkHours_ReturnsMoondark(int hour, int minute, string expected)
    {
        // Arrange
        var time = new TimeOnly(hour, minute);

        // Act
        string result = TimeOfDayNames.GetTimeOfDayName(time);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase(3, 0, "Night's end")]
    [TestCase(3, 30, "Night's end")]
    [TestCase(4, 0, "Night's end")]
    [TestCase(4, 59, "Night's end")]
    public void GetTimeOfDayName_NightsEndHours_ReturnsNightsEnd(int hour, int minute, string expected)
    {
        // Arrange
        var time = new TimeOnly(hour, minute);

        // Act
        string result = TimeOfDayNames.GetTimeOfDayName(time);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void FormatTimeOfDay_WithIncludeTime_ReturnsNameAndTime()
    {
        // Arrange
        var time = new TimeOnly(8, 30);

        // Act
        string result = TimeOfDayNames.FormatTimeOfDay(time, includeTime: true);

        // Assert
        Assert.That(result, Is.EqualTo("Morning (08:30)"));
    }

    [Test]
    public void FormatTimeOfDay_WithoutIncludeTime_ReturnsNameOnly()
    {
        // Arrange
        var time = new TimeOnly(12, 0);

        // Act
        string result = TimeOfDayNames.FormatTimeOfDay(time, includeTime: false);

        // Assert
        Assert.That(result, Is.EqualTo("Highsun"));
    }

    [Test]
    public void FormatTimeOfDay_DefaultParameter_IncludesTime()
    {
        // Arrange
        var time = new TimeOnly(18, 45);

        // Act
        string result = TimeOfDayNames.FormatTimeOfDay(time);

        // Assert
        Assert.That(result, Is.EqualTo("Sunset (18:45)"));
    }

    [Test]
    public void GetTimeOfDayName_AllHoursOfDay_ReturnsValidNames()
    {
        // Arrange & Act & Assert - verify every hour has a valid name
        for (int hour = 0; hour < 24; hour++)
        {
            var time = new TimeOnly(hour, 0);
            string result = TimeOfDayNames.GetTimeOfDayName(time);
            
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }
    }

    [Test]
    public void GetTimeOfDayName_BoundaryTransitions_ReturnsCorrectNames()
    {
        // Test the boundary transitions between time periods
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(4, 59)), Is.EqualTo("Night's end"));
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(5, 0)), Is.EqualTo("Dawn"));
        
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(5, 59)), Is.EqualTo("Dawn"));
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(6, 0)), Is.EqualTo("Morning"));
        
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(11, 59)), Is.EqualTo("Morning"));
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(12, 0)), Is.EqualTo("Highsun"));
        
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(12, 59)), Is.EqualTo("Highsun"));
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(13, 0)), Is.EqualTo("Afternoon"));
        
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(16, 59)), Is.EqualTo("Afternoon"));
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(17, 0)), Is.EqualTo("Dusk"));
        
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(17, 59)), Is.EqualTo("Dusk"));
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(18, 0)), Is.EqualTo("Sunset"));
        
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(18, 59)), Is.EqualTo("Sunset"));
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(19, 0)), Is.EqualTo("Evening"));
        
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(23, 59)), Is.EqualTo("Evening"));
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(0, 0)), Is.EqualTo("Midnight"));
        
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(0, 59)), Is.EqualTo("Midnight"));
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(1, 0)), Is.EqualTo("Moondark"));
        
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(2, 59)), Is.EqualTo("Moondark"));
        Assert.That(TimeOfDayNames.GetTimeOfDayName(new TimeOnly(3, 0)), Is.EqualTo("Night's end"));
    }
}

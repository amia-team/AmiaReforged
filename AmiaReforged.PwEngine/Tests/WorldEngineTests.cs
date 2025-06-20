using System.Diagnostics;
using AmiaReforged.PwEngine.Systems.WorldEngine;
using Moq;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests;

[TestFixture]
public class WorldEngineTests
{
    private WorldEngineLoader _engineLoader = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        LoggingConfiguration config = new();
        ConsoleTarget consoleTarget = new("console")
        {
            Layout = "${longdate} ${level:uppercase=true} ${message} ${exception:format=tostring}"
        };

        config.AddTarget(consoleTarget);
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);
        LogManager.Configuration = config;

        Trace.Listeners.Add(new ConsoleTraceListener());

        Environment.SetEnvironmentVariable("ECONOMY_RESOURCES_PATH", TestConfig.ResourcesPath);

        Mock<IWorldConfigProvider> fakeConfig = new Mock<IWorldConfigProvider>();
        fakeConfig.Setup(c => c.GetBoolean(It.IsAny<string>())).Returns(true);

        _engineLoader = new(fakeConfig.Object);
    }

    [Test]
    public void Should_Find_Resources_Directory()
    {
        Assert.That(
            _engineLoader.ResourceDirectoryExists(),
            Is.True,
            "Resource directory should exist"
        );
    }

    [Test]
    public void Should_Find_Materials()
    {
        Assert.That(
            _engineLoader.Materials,
            Is.Not.Empty,
            "Materials should have been loaded."
        );
    }

    [Test]
    public void Should_Find_ResourceNodes()
    {
        Assert.That(
            _engineLoader.ResourceNodes,
            Is.Not.Empty,
            "Resource Nodes should have been loaded."
        );
    }

    [Test]
    public void Should_Find_Environments()
    {
        Assert.That(
            _engineLoader.Climates,
            Is.Not.Empty,
            "Climates should not be empty"
        );
    }

    [Test]
    public void Should_Find_Regions()
    {
        Assert.That(
            _engineLoader.Regions,
            Is.Not.Empty,
            "Regions should have been loaded."
        );
    }

    [Test]
    public void Should_Have_No_Empty_Regions()
    {
        Assert.That(
            _engineLoader.Regions.All(r => r.Areas.Count > 0),
            Is.True,
            "A region must have at least one area assigned."
        );
    }
}
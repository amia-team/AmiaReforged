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
    private WorldEngine _engine = null!;

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

        _engine = new(fakeConfig.Object);
    }

    [Test]
    public void Should_Find_Resources_Directory()
    {
        Assert.That(
            _engine.ResourceDirectoryExists(),
            Is.True,
            "Resource directory should exist"
        );
    }

    [Test]
    public void Should_Find_Materials()
    {
        Assert.That(
            _engine.Materials,
            Is.Not.Empty,
            "Materials should have been loaded."
        );
    }

    [Test]
    public void Should_Find_ResourceNodes()
    {
        Assert.That(
            _engine.ResourceNodes,
            Is.Not.Empty,
            "Resource Nodes should have been loaded."
        );
    }
    
    [Test]
    public void Should_Find_Environments()
    {
        Assert.That(
            _engine.Climates,
            Is.Not.Empty,
            "Climates should not be empty"
        );

        Assert.That(
            _engine.Climates.All(c => c.WhitelistedNodeTags.Count > 0),
            Is.True,
            "Climates must always have a list of allowed resources"
        );
    }
}
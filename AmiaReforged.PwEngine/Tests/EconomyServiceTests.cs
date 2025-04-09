using System.Diagnostics;
using AmiaReforged.PwEngine.Systems.Economy;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests;

[TestFixture]
public class EconomyServiceTests
{
    private EconomyService _economyService;

    [OneTimeSetUp]
    public void Setup()
    {
        LoggingConfiguration config = new LoggingConfiguration();
        ConsoleTarget consoleTarget = new ConsoleTarget("console")
        {
            Layout = "${longdate} ${level:uppercase=true} ${message} ${exception:format=tostring}"
        };

        config.AddTarget(consoleTarget);
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);
        LogManager.Configuration = config;

        Trace.Listeners.Add(new ConsoleTraceListener());

        Environment.SetEnvironmentVariable("ECONOMY_RESOURCES_PATH", TestConfig.ResourcesPath);

        _economyService = new();
    }

    [Test]
    public void Should_Find_Resources_Directory()
    {
        Assert.That(
            _economyService.DirectoryExists(),
            Is.True,
            "Resources directory should exist"
        );
    }

    [Test]
    public void Should_Read_Materials()
    {
        Assert.That(
            _economyService.Materials,
            Is.Not.Empty,
            "Materials should not be empty"
        );
    }


    [Test]
    public void Should_Read_EnvironmentTraits()
    {
        Assert.That(
            _economyService.EnvironmentTraits,
            Is.Not.Empty,
            "EnvironmentTraits should not be empty"
        );
    }

    [Test]
    public void Should_Read_PersistentResources()
    {
        Assert.That(
            _economyService.PersistentResources,
            Is.Not.Empty,
            "PersistentResources should not be empty"
        );
    }

    [Test]
    public void Should_Read_CultivatedResources()
    {
        Assert.That(
            _economyService.CultivatedResources,
            Is.Not.Empty,
            "CultivatedResources should not be empty"
        );
    }

    [Test]
    public void Should_Read_Professions()
    {
        Assert.That(
            _economyService.Professions,
            Is.Not.Empty,
            "Professions should not be empty"
        );
    }

    [Test]
    public void Should_Read_Innovations()
    {
        Assert.That(
            _economyService.Innovations,
            Is.Not.Empty,
            "Innovations should not be empty"
        );
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        Trace.Flush();
        Trace.Listeners.Clear();
    }
}

public static class TestConfig
{
    static TestConfig()
    {
        DotNetEnv.Env.Load("Tests/Systems/Economy/Resources/test.env");
        ResourcesPath = Environment.GetEnvironmentVariable("TEST_FILE_LOCATION") ?? "";
    }

    public static string ResourcesPath { get; set; }
}
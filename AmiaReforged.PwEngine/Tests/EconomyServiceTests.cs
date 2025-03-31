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
        string resourcesPath =
            "/home/cltalmadge/RiderProjects/AmiaReforged/AmiaReforged.PwEngine/Resources/EconomySystem";
        Assert.That(TestConfig.ResourcesPath, Is.EqualTo(resourcesPath),
            "Test environment variable not configured: TEST_FILE_LOCATION");

        string serviceResource = _economyService.ResourcesPath;
        
        Assert.That(
            serviceResource,
            Is.EqualTo(TestConfig.ResourcesPath),
            "Resources path should be set correctly"
        );
        
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
    public void Should_Not_Have_Null_Materials()
    {
        Assert.That(
            _economyService.Materials,
            Is.Not.Null,
            "Materials should not be null"
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
    public void Should_Not_Have_Null_EnvironmentTraits()
    {
        Assert.That(
            _economyService.EnvironmentTraits,
            Is.Not.Null,
            "EnvironmentTraits should not be null"
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
    public void Should_Not_Have_Null_PersistentResources()
    {
        Assert.That(
            _economyService.PersistentResources,
            Is.Not.Null,
            "PersistentResources should not be null"
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
    public void Should_Not_Have_Null_CultivatedResources()
    {
        Assert.That(
            _economyService.CultivatedResources,
            Is.Not.Null,
            "CultivatedResources should not be null"
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
    public void Should_Not_Have_Null_Professions()
    {
        Assert.That(
            _economyService.Professions,
            Is.Not.Null,
            "Professions should not be null"
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
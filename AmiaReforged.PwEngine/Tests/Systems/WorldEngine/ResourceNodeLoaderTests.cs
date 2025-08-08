using System.Diagnostics;
using AmiaReforged.PwEngine.Systems.WorldEngine.Economy.YamlLoaders;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine;

[TestFixture]
public class ResourceNodeLoaderTests
{
    private ResourceNodeLoader _sut = null!;

    [OneTimeSetUp]
    public void Setup()
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
        Console.WriteLine("TEST PATH: " + TestConfig.ResourcesPath);
        Console.WriteLine("DIR EXISTS: " + Directory.Exists(TestConfig.ResourcesPath));
    }

    [Test]
    public void Should_Load_With_No_Errors()
    {
        _sut = new ResourceNodeLoader();

        _sut.LoadAll();

        if (_sut.Failures.Count == 0) return;

        string failureString = _sut.Failures.Aggregate("",
            (current, resourceLoadError) =>
                $"{resourceLoadError.FilePath}, {resourceLoadError.ErrorMessage}, {resourceLoadError.Exception}\n{current}");

        Assert.Fail($"Failed to load some or all resource definitions: some files had invalid parameters\n {failureString}");
    }

    [Test]
    public void Result_Should_Not_Be_Empty_Set()
    {
        _sut = new ResourceNodeLoader();

        _sut.LoadAll();

        Assert.That(_sut.LoadedResources, Is.Not.Empty, "Definitions should have been loaded.");
    }
}

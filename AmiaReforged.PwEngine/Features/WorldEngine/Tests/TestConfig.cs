using DotNetEnv;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Tests;

public static class TestConfig
{
    static TestConfig()
    {
        Env.Load("Tests/Systems/Economy/Resources/test.env",  LoadOptions.TraversePath());
        ResourcesPath = Environment.GetEnvironmentVariable("TEST_FILE_LOCATION") ?? "";
    }

    public static string ResourcesPath { get; set; }
}
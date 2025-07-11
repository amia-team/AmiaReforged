using DotNetEnv;

namespace AmiaReforged.PwEngine.Tests;

public static class TestConfig
{
    static TestConfig()
    {
        DotNetEnv.Env.Load("Tests/Systems/Economy/Resources/test.env",  new LoadOptions());
        ResourcesPath = Environment.GetEnvironmentVariable("TEST_FILE_LOCATION") ?? "";
    }

    public static string ResourcesPath { get; set; }
}
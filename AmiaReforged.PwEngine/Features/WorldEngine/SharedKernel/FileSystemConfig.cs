using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

public static class FileSystemConfig
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static string WorldEngineResourcesPath { get; } = GetFilePath();

    private static string GetFilePath()
    {
        string resources = Environment.GetEnvironmentVariable("ECONOMY_RESOURCES_PATH") ?? string.Empty;

        if (resources == string.Empty)
        {
            Log.Error("No directory defined.");
            return string.Empty;
        }

        return resources;
    }
}

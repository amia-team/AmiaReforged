namespace AmiaReforged.System.Services;

public sealed class ResetTimeKeeperSingleton
{
    private static ResetTimeKeeperSingleton _instance;

    private ResetTimeKeeperSingleton()
    {
    }

    public static ResetTimeKeeperSingleton Instance
    {
        get
        {
            if (_instance == null) _instance = new();

            return _instance;
        }
    }

    public long ResetStartTime { get; set; }

    public long Uptime() => ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds() - ResetStartTime;
}
namespace AmiaReforged.PwEngine.Systems.Chat.Commands;

public sealed class ResetTimeKeeperSingleton
{
    private static ResetTimeKeeperSingleton? _instance;

    private ResetTimeKeeperSingleton()
    {
    }

    public static ResetTimeKeeperSingleton Instance => _instance ??= new();

    public long ResetStartTime { get; set; }

    public long Uptime() => ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds() - ResetStartTime;
}
using Anvil.API;
using NLog;

namespace AmiaReforged.System.Dynamic.Quest;

public class MiniQuest
{
    private readonly NwCreature? _questGiver;
    private readonly NwPlayer? _player;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();


    public MiniQuest(NwCreature? questGiver, NwPlayer? player)
    {
        _questGiver = questGiver;
        _player = player;
    }
    public void Reward()
    {
        _questGiver?.SpeakString("Jes is amazing!");
        _player?.LoginCreature?.SpeakString("I agree!");
    }
}
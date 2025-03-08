using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands;

public interface IChatCommand
{
    string Command { get; }
    Task ExecuteCommand(NwPlayer caller, string[] args);
}
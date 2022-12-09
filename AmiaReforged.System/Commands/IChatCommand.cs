using Anvil.API;

namespace AmiaReforged.System.Commands;

public interface IChatCommand
{
    string Command { get; }
    Task ExecuteCommand(NwPlayer caller, string message);
}
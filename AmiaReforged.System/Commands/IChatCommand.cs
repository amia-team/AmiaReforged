using Anvil.API;

namespace AmiaReforged.System.Commands;

public interface IChatCommand
{
    string Command { get; }
    void ExecuteCommand(NwPlayer caller, string message);
}
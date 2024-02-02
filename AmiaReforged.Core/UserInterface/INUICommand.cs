namespace AmiaReforged.Core.UserInterface;

public interface INUICommand
{
    /// <summary>
    /// Executes a NUI command and returns status.
    /// </summary>
    /// <returns>Command</returns>
    public CommandResult Execute();
}
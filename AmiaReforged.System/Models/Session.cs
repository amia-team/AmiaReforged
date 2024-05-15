namespace AmiaReforged.System.Services;

public class Session
{
    public DateTime SessionStart { get; init; }
    public DateTime SessionEnd { get; private set; }

    public Session()
    {
        SessionStart = DateTime.Now;
    }
    
    public void EndSession()
    {
        SessionEnd = DateTime.Now;
    }
}
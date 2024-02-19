namespace AmiaReforged.Core.Models;

public class DmLogin
{
    public int LoginNumber { get; set; }
    public string? CdKey { get; set; }
    public string? LoginName { get; set; }
    public DateTime SessionStart { get; set; }
    public DateTime? SessionEnd { get; set; }
}
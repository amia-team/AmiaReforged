using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models;

public class DmLogin
{
    [Key] public int LoginNumber { get; init; }
    public string? CdKey { get; set; }
    public string? LoginName { get; set; }
    public DateTime SessionStart { get; set; }
    public DateTime? SessionEnd { get; set; }
    
    [ForeignKey("PcKey")] public Dm Dm { get; set; }
}
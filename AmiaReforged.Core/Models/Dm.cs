using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models;

public class Dm
{
    [Key] public string CdKey { get; set; } = null!;
    public string LoginName { get; set; }
}
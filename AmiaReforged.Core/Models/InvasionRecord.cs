using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models;

public class InvasionRecord
{
    [Key] public int Id { get; set; }
    public string AreaZone { get; set; }
    public int InvasionPercent { get; set; }
    public int RealmChaos  { get; set; }
}
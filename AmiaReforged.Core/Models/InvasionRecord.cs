using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models;

public class InvasionRecord
{
    public string AreaZone { get; set; }
    
    public int InvasionPercent { get; set; }

    public InvasionRecord(string areaName, int invasionPercent)
    {
        AreaZone = areaName;
        InvasionPercent = invasionPercent;
    }

}
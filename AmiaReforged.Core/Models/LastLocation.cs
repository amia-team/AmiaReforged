using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models;

using System.Numerics;
using Anvil.API;

public class LastLocation
{
    [Key] public int Id { get; set; }
    public string PCKey { get; set; }
    public string AreaResRef { get; set; }
    public float Orientation { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

}
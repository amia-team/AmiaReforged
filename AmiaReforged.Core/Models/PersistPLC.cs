using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models;
using Anvil.API;

public class PersistPLC
{
    [Key] public int Id { get; set; }
    public uint PLC { get; set; }
    public uint Area { get; set; }
    public float Orientation { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    
}
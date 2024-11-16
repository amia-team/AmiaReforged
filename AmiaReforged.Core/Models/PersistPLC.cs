using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models;
using Anvil.API;

public class PersistPLC
{
    [Key] public int Id { get; set; }
    public uint PLC { get; set; }
    public Location Location { get; set; }
    
}
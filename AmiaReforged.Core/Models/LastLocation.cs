using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models;
using Anvil.API;

public class LastLocation
{
    [Key] public int Id { get; set; }
    public string PCKey { get; set; }
    public Location Location { get; set; }

}
using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models;

public class AmiaCharacter
{
    
    [Key]
    public string PcKey { get; set; }
    
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
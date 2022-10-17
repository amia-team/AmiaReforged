using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models;

public class AmiaCharacter
{
    public string PcKey { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

}
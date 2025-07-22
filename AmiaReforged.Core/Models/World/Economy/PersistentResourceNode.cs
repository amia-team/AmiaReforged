using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models.World.Economy;

public class PersistentResourceNode
{
    [Key] public long Id { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Anvil.API;

namespace AmiaReforged.PwEngine.Database.Entities.Economy;

public class ResourceNodeInstance
{
    [Key] public long Id { get; set; }

    public string DefinitionId { get; set; }
    [ForeignKey("DefinitionId")] public required ResourceNodeDefinition Definition { get; set; }

    public long LocationId { get; set; }
    [ForeignKey("LocationId")] public required SavedLocation Location { get; set; }
    public float Richness { get; set; }
    public int Quantity { get; set; }
    public float Scale { get; set; }
}

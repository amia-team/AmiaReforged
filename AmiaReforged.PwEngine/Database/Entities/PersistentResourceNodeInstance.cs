using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Database.Entities;

public class PersistentResourceNodeInstance
{
    [Key] public Guid Id { get; set; }
    public string Area { get; set; } = null!;
    public string DefinitionTag { get; set; } = null!;
    public int Uses { get; set; }
    public int Quality { get; set; } // enum as int
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Rotation { get; set; }

}

using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace AmiaReforged.PwEngine.Database.Entities;

public class SavedLocation
{
    [Key] public long Id { get; set; }

    [StringLength(16, MinimumLength = 1)] public required string AreaResRef { get; set; }
    public required float X { get; set; }
    public required float Y { get; set; }
    public required float Z { get; set; }
    public required float Orientation { get; set; }

    public Vector3 Position => new(X, Y, Z);
}

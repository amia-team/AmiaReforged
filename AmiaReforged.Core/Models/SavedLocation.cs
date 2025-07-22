using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace AmiaReforged.Core.Models;

public class SavedLocation
{
    [Key] public long Id { get; set; }

    [Length(1, 16)] public required string AreaResRef { get; set; }

    public required float X { get; set; }
    public required float Y { get; set; }
    public required float Z { get; set; }
    public required float Orientation { get; set; }

    public Vector3 Position => new(X, Y, Z);
}

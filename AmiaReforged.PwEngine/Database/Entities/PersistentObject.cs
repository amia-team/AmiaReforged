using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace AmiaReforged.PwEngine.Database.Entities;

public class PersistentObject
{
    [Key] public long Id { get; set; }
    public required int Type { get; set; }
    public required byte[] Serialized { get; set; }
    public Guid? CharacterId { get; set; }
    public long LocationId { get; set; }
    [ForeignKey("LocationId")] public SavedLocation? Location { get; set; }
    
    /// <summary>
    /// Serialized item data for the original item that was placed as furniture.
    /// Null for non-furniture placeables or system-placed objects.
    /// Used to recover items when properties are foreclosed.
    /// </summary>
    public byte[]? SourceItemData { get; set; }
}

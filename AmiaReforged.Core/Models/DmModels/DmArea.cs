using System.ComponentModel.DataAnnotations;
using System;

namespace AmiaReforged.Core.Models.DmModels;

public class DmArea
{
    [Key] public long Id { get; set; }

    public required string CdKey { get; set; }

    /// <summary>
    /// Used to find the correct area to clone.
    /// </summary>
    public required string OriginalResRef { get; set; }

    public required string NewName { get; set; }

    public required byte[] SerializedARE { get; set; }
    public required byte[] SerializedGIT { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

}

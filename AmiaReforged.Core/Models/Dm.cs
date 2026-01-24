﻿using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models;

public class Dm
{
    [Key] public string CdKey { get; set; } = null!;
    public string LoginName { get; set; }

    public virtual List<DmPlaytimeRecord> PlaytimeRecords { get; set; } = new();
}

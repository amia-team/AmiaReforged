﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Database.Entities;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Entities;

public class ItemStorageUser
{
    [Key] public long Id { get; set; }

    public long ItemStorageId { get; set; }
    [ForeignKey("ItemStorageId")] public ItemStorage ItemStorage { get; set; }
    
    public long WorldCharacterId { get; set; }
    [ForeignKey("WorldCharacterId")] public WorldCharacter WorldCharacter { get; set; }
}
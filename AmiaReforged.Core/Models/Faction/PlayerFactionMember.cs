﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models.Faction;

public class PlayerFactionMember
{
    [Key] public long Id { get; set; }

    public long FactionId { get; set; }
    [ForeignKey("FactionId")] public FactionEntity Faction { get; set; }
    
    public Guid CharacterId { get; set; }
    [ForeignKey("CharacterId")] public PlayerCharacter Character { get; set; }
}
﻿namespace AmiaReforged.PwEngine.Systems.JobSystem.Entities;

public class LedgerItem
{
    public required string Name { get; set; }
    public QualityEnum QualityEnum { get; set; }
    public MaterialEnum MaterialEnum { get; set; }
    public float MagicModifier { get; set; }
    public float DurabilityModifier { get; set; }
    public int BaseValue { get; set; }
}
using System;
using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

public sealed class MarketReeveLockupWindowConfig
{
    public MarketReeveLockupWindowConfig(
        PersonaId persona,
        string? areaResRef,
        string? title,
        IReadOnlyList<ReeveLockupItemSummary>? initialItems,
        NwCreature recipient)
    {
        Persona = persona;
        AreaResRef = areaResRef;
        Title = string.IsNullOrWhiteSpace(title) ? "Market Reeve Lockup" : title!;
        InitialItems = initialItems ?? Array.Empty<ReeveLockupItemSummary>();
        Recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
    }

    public PersonaId Persona { get; }

    public string? AreaResRef { get; }

    public string Title { get; }

    public IReadOnlyList<ReeveLockupItemSummary> InitialItems { get; }

    public NwCreature Recipient { get; }
}

using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Spells.SpellCrafting;

[ServiceBinding(typeof(SpellCraftingService))]
public class SpellCraftingService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public SpellCraftingService()
    {
        NwModule.Instance.OnSpellCast += CraftSpell;
    }

    private static void CraftSpell(OnSpellCast eventData)
    {
        if (eventData.TargetObject is not NwItem targetItem) return;
        if (eventData.Spell is not { } spell) return;

        CraftSpell craftSpell = new(eventData, spell, targetItem);
        craftSpell.DoCraftSpell();

        eventData.PreventSpellCast = true;
    }
}

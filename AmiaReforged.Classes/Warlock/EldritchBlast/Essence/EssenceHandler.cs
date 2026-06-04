using AmiaReforged.Classes.Warlock.EldritchBlast.Shape;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence;

[ServiceBinding(typeof(EssenceHandler))]
public class EssenceHandler
{
    private const string EssenceVar = "warlock_essence";
    private const int RemoveEssenceId = 1299;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static EssenceFactory? _essenceFactory;

    public EssenceHandler(EssenceFactory essenceFactory)
    {
        _essenceFactory = essenceFactory;
        NwModule.Instance.OnSpellAction += OnEldritchEssence;
        Log.Info(message: "Warlock Essence Handler initialized.");
    }

    private void OnEldritchEssence(OnSpellAction eventData)
    {
        int spellId = eventData.Spell.Id;
        if (!Enum.IsDefined(typeof(EssenceType), spellId)) return;

        NwCreature warlock = eventData.Caster;
        LocalVariableInt essenceKey = warlock.GetObjectVariable<LocalVariableInt>(EssenceVar);
        EssenceType essenceType = (EssenceType)spellId;

        if (essenceType == EssenceType.None)
            essenceKey.Delete();
        else
            essenceKey.Value = spellId;

        eventData.PreventSpellCast = true;

        if (!warlock.IsPlayerControlled(out NwPlayer? player)) return;

        string essenceName = eventData.Spell.Name.ToString();
        SendEssenceMessage(player, essenceName, essenceType);

        NwItem? weapon = eventData.Caster.GetItemInSlot(InventorySlot.RightHand);
        if (weapon == null) return;
        if (!WeaponHasHideousBlow(weapon)) return;

        EssenceData? essence = _essenceFactory?.GetEssenceData(warlock, warlock.GetInvocationCasterLevel());
        if (essence == null) return;
        ChangeHideousBlowGlow(weapon, essence.Value);
    }

    private static void SendEssenceMessage(NwPlayer player, string essenceName, EssenceType essenceType)
    {
        if (essenceType == EssenceType.None)
        {
            player.SendServerMessage("Eldritch essence removed.".ColorWarlock());
            player.FloatingTextString("*Essence Deactivated*".ColorWarlock(), false, false);
            return;
        }

        player.SendServerMessage($"{essenceName} applied.".ColorWarlock());
        player.FloatingTextString($"*{essenceName} Activated*".ColorWarlock(), false, false);
    }

    private static bool WeaponHasHideousBlow(NwItem weapon)
        => weapon.ItemProperties.Any(ip => ip.Tag == nameof(ShapeType.HideousBlow));

    /// <summary>
    /// Hideous Blow recharges every two rounds, and it's possible to change the essence on the fly, so we have to
    /// change the visual effect to match that
    /// </summary>
    private static void ChangeHideousBlowGlow(NwItem weapon, EssenceData essence)
    {
        foreach (ItemProperty ip in weapon.ItemProperties)
        {
            if (ip is { Tag: nameof(ShapeType.HideousBlow), Property.PropertyType: ItemPropertyType.VisualEffect })
                weapon.RemoveItemProperty(ip);
        }
        ItemProperty weaponGlow = ItemProperty.VisualEffect(essence.HideousBlowVfx);
        weaponGlow.Tag = nameof(ShapeType.HideousBlow);
        weapon.AddItemProperty(weaponGlow, EffectDuration.Temporary, TimeSpan.FromHours(8));
    }
}





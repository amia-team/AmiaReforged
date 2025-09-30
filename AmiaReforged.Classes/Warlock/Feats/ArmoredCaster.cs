using Anvil.API;

namespace AmiaReforged.Classes.Warlock.Feats;

public static class ArmoredCaster
{
    public static int CalculateAsf(NwCreature warlock)
    {
        int effectiveAsf = warlock.ArcaneSpellFailure;

        byte warlockLevel = warlock.GetClassInfo(WarlockConstants.WarlockClass)?.Level ?? 0;

        bool majorityLevelWarlock = warlockLevel > warlock.Level / 2;
        if (!majorityLevelWarlock) return effectiveAsf;

        NwItem? armor = warlock.GetItemInSlot(InventorySlot.Chest);
        if (armor != null)
            effectiveAsf -= armor.BaseACValue switch
            {
                1 => 5,
                2 => 10,
                3 => 20,
                _ => 0
            };

        NwItem? shield = warlock.GetItemInSlot(InventorySlot.LeftHand);
        if (shield != null && shield.BaseItem.ItemType == BaseItemType.SmallShield)
            effectiveAsf -= shield.BaseItem.ArcaneSpellFailure;

        return effectiveAsf;
    }
}

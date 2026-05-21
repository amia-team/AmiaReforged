using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.EffectUtils.ItemBuff;

[ServiceBinding(typeof(ItemBuffService))]
public class ItemBuffService(ScriptHandleFactory scriptHandleFactory)
{
    public void ApplyItemBuff(NwItem targetItem, NwSpell spell, ItemProperty[] itemProperties, EffectSubType subType,
        TimeSpan duration, string? tag = null)
    {
        if (itemProperties.Length == 0) return;

        if (WeaponBuffUtils.IsMeleeWeapon(targetItem))
            WeaponBuffUtils.RemoveConflictingWeaponBuffs(spell, targetItem);

        Effect itemBuffEffect = EffectItemBuff(targetItem, itemProperties, duration);
        itemBuffEffect.SubType = subType;
        if (tag != null) itemBuffEffect.Tag = tag;

        targetItem.ApplyEffect(EffectDuration.Temporary, itemBuffEffect, duration);
    }

    private Effect EffectItemBuff(NwItem targetItem, ItemProperty[] itemProperties, TimeSpan duration)
    {
        ScriptCallbackHandle onApplyItemBuff = scriptHandleFactory.CreateUniqueHandler(_ =>
        {
            foreach (ItemProperty ip in itemProperties)
                targetItem.AddItemProperty(ip, EffectDuration.Temporary, duration);

            return ScriptHandleResult.Handled;
        });

        ScriptCallbackHandle onRemoveItemBuff = scriptHandleFactory.CreateUniqueHandler(_ =>
        {
            foreach (ItemProperty ip in itemProperties)
                targetItem.RemoveItemProperty(ip);

            return ScriptHandleResult.Handled;
        });

        return Effect.RunAction(onAppliedHandle: onApplyItemBuff, onRemovedHandle: onRemoveItemBuff);
    }
}

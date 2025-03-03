// General monk ability handling that's applied across abilities
using AmiaReforged.Classes.Monk.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(AbilityRestrictionsHandler))]
public class AbilityRestrictionsHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public AbilityRestrictionsHandler()
    {
        string environment = UtilPlugin.GetEnvironmentVariable("SERVER_MODE");

        if (environment == "live") return;
        
        NwModule.Instance.OnUseFeat += PreventWhenArmored;
        NwModule.Instance.OnUseFeat += PreventHostileActionToFriendly;
        NwModule.Instance.OnUseFeat += PreventAbilityInNoCastingArea;
        Log.Info("Monk Ability Restrictions Handler initialized.");
    }
    
    private static void PreventWhenArmored(OnUseFeat eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk) is null) return;
        int feat = eventData.Feat.Id;
        bool isMonkAbility = feat is MonkFeat.EmptyBody or MonkFeat.KiBarrier or MonkFeat.KiShout 
            or MonkFeat.WholenessOfBody or MonkFeat.QuiveringPalm or MonkFeat.StunningStrike 
            or MonkFeat.EagleStrike or MonkFeat.AxiomaticStrike;
        if (!isMonkAbility) return;

        NwCreature monk = eventData.Creature;
        
        // Technique can only be used unarmored
        bool isArmored = monk.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;
        bool isWieldingShield = monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category is BaseItemCategory.Shield;
        if (isArmored || isWieldingShield)
        {
            eventData.PreventFeatUse = true;
            if (monk.IsPlayerControlled(out NwPlayer? player)) 
                player.SendServerMessage($"{eventData.Feat.Name} can only be used while unarmored.", MonkColors.MonkColorScheme);
        }
    }
    /*private static void PreventHostileActionToFriendly(OnUseFeat eventData)
    {
        // If monk and targets friendly with a hostile ability
        if (eventData.Creature.GetClassInfo(ClassType.Monk) is null) return;
        if (!eventData.Creature.IsReactionTypeFriendly((NwCreature)eventData.TargetObject)) return;
        if (eventData.Feat.Id is not MonkFeat.QuiveringPalm) return;

        eventData.PreventFeatUse = true;
        if (eventData.Creature.IsPlayerControlled(out NwPlayer? player))
            player.SendServerMessage("You cannot perform that action on a friendly target due to PvP settings");
    }

    private static void PreventAbilityInNoCastingArea(OnUseFeat eventData)
    {
        // If monk, in a no-cast area, and uses a monk ability
        if (eventData.Creature.GetClassInfo(ClassType.Monk) is null) return;
        if (eventData.Creature.Area?.GetObjectVariable<LocalVariableInt>("NoCasting").Value is 0) return;
        
        int feat = eventData.Feat.Id;
        bool isMonkAbility = feat is MonkFeat.EmptyBody or MonkFeat.KiBarrier or MonkFeat.KiShout 
            or MonkFeat.WholenessOfBody or MonkFeat.QuiveringPalm;
        
        if (!isMonkAbility) return;

        eventData.PreventFeatUse = true;
        if (eventData.Creature.IsPlayerControlled(out NwPlayer? player))
            player.SendServerMessage("- You cannot cast magic in this area! -");
    }*/
}
// General monk ability handling that's applied across abilities
using AmiaReforged.Classes.Monk.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.VisualBasic;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(AbilityRestrictionsHandler))]
public class AbilityRestrictionsHandler
{
    // Base item ID for base item "Focus"
    private const int FocusId = 222;
    // Recurring restriction booleans
    private static bool _hasArmor;
    private static bool _hasShield;
    private static bool _hasFocusWithoutUnarmed;
    
    
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    public AbilityRestrictionsHandler(EventService eventService)
    {
        string environment = UtilPlugin.GetEnvironmentVariable("SERVER_MODE");

        if (environment == "live") return;
        
        eventService.SubscribeAll<OnLoadCharacterFinish, OnLoadCharacterFinish.Factory>(HideDefaultFeedback, EventCallbackType.After);
        NwModule.Instance.OnUseFeat += EnforceTechniqueRestrictions;
        NwModule.Instance.OnEffectApply += DeactivateMartialTechnique;
        NwModule.Instance.OnEffectApply += DeactivateStaticBonuses;
        NwModule.Instance.OnUseFeat += PreventHostileTechniqueToFriendly;
        NwModule.Instance.OnUseFeat += PreventTechniqueInNoCastingArea;
        Log.Info("Monk Ability Restrictions Handler initialized.");
    }
    private static void HideDefaultFeedback(OnLoadCharacterFinish eventData)
    {
        if (eventData.Player.ControlledCreature is not NwCreature monk) return;
        if (monk.GetClassInfo(ClassType.Monk) is null) return;
        
        FeedbackPlugin.SetFeedbackMessageHidden
            (FeedbackPlugin.NWNX_FEEDBACK_EQUIP_MONK_ABILITIES, NWScript.TRUE, monk);
    }

    private static void EnforceTechniqueRestrictions(OnUseFeat eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk) is null) return;
        
        int featId = eventData.Feat.Id;
        
        bool isMonkAbility = featId is MonkFeat.EmptyBody or MonkFeat.KiBarrier or MonkFeat.KiShout 
            or MonkFeat.WholenessOfBody or MonkFeat.QuiveringPalm or MonkFeat.StunningStrike 
            or MonkFeat.EagleStrike or MonkFeat.AxiomaticStrike;
        
        if (!isMonkAbility) return;

        NwCreature monk = eventData.Creature;
        
        _hasArmor = monk.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;
        _hasShield = monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category is BaseItemCategory.Shield;
        _hasFocusWithoutUnarmed = monk.GetItemInSlot(InventorySlot.RightHand)!.IsValid
                                             && monk.GetItemInSlot(InventorySlot.LeftHand)!.BaseItem.Id == FocusId;
            
        if (_hasArmor || _hasShield || _hasFocusWithoutUnarmed)
            eventData.PreventFeatUse = true;
        
        if (!monk.IsPlayerControlled(out NwPlayer? player)) return;
            
        if (_hasArmor)
            player.SendServerMessage($"Having equipped an armor has prevented your {eventData.Feat.Name}.");
        if (_hasShield)
            player.SendServerMessage($"Having equipped a shield has prevented your {eventData.Feat.Name}.");
        if (_hasFocusWithoutUnarmed)
            player.SendServerMessage($"Having equipped a focus without being unarmed has prevented your {eventData.Feat.Name}.");
    }
    
    private static void DeactivateMartialTechnique(OnEffectApply eventData)
    {
        if (eventData.Object is not NwCreature monk) return;

        if (eventData.Effect.Tag is null) return;
        
        // Effect must be a martial technique
        if (!eventData.Effect.Tag.Contains(MonkTechnique.MartialTechnique)) return;
        
        _hasArmor = monk.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;
        _hasShield = monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category is BaseItemCategory.Shield;
        _hasFocusWithoutUnarmed = monk.GetItemInSlot(InventorySlot.RightHand)!.IsValid
                                  && monk.GetItemInSlot(InventorySlot.LeftHand)!.BaseItem.Id == FocusId;
        
        if (_hasArmor || _hasShield || _hasFocusWithoutUnarmed)
            monk.RemoveEffect(eventData.Effect);
        
        string techniqueName = eventData.Effect.Tag switch
        {
            MonkTechnique.StunningTag => "Stunning Strike",
            MonkTechnique.EagleTag => "Eagle Strike",
            MonkTechnique.AxiomaticTag  => "Axiomatic Strike",
            _ => ""
        };
        
        if (!monk.IsPlayerControlled(out NwPlayer? player)) return;
        
        if (_hasArmor)
            player.SendServerMessage($"Having equipped an armor has prevented your {techniqueName}.");
        if (_hasShield)
            player.SendServerMessage($"Having equipped a shield has prevented your {techniqueName}.");
        if (_hasFocusWithoutUnarmed)
            player.SendServerMessage($"Having equipped a focus without being unarmed has prevented your {techniqueName}.");
    }
    private static void DeactivateStaticBonuses(OnEffectApply eventData)
    {
        if (eventData.Object is not NwCreature monk) return;
        
        if (eventData.Effect.Tag is null) return;
        
        // Effect must be a martial technique
        if (eventData.Effect.Tag != "monk_staticeffects") return;
        
        _hasArmor = monk.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;
        _hasShield = monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category is BaseItemCategory.Shield;
        _hasFocusWithoutUnarmed = monk.GetItemInSlot(InventorySlot.RightHand)!.IsValid
                                  && monk.GetItemInSlot(InventorySlot.LeftHand)!.BaseItem.Id == FocusId;
        
        if (_hasArmor || _hasShield || _hasFocusWithoutUnarmed)
            monk.RemoveEffect(eventData.Effect);
        
        if (!monk.IsPlayerControlled(out NwPlayer? player)) return;
        
        if (_hasArmor)
            player.SendServerMessage($"Equipping this armor has disabled your monk abilities.");
        if (_hasShield)
            player.SendServerMessage($"Equipping this shield has disabled your monk abilities.");
        if (_hasFocusWithoutUnarmed)
            player.SendServerMessage($"Equipping a focus without being unarmed has disabled your monk abilities.");
    }
    
    private static void PreventHostileTechniqueToFriendly(OnUseFeat eventData)
    {
        // If monk and targets friendly with a hostile ability
        if (eventData.Creature.GetClassInfo(ClassType.Monk) is null) return;
        if (!eventData.Creature.IsReactionTypeFriendly((NwCreature)eventData.TargetObject)) return;
        if (eventData.Feat.Id is not MonkFeat.QuiveringPalm) return;

        eventData.PreventFeatUse = true;
        if (eventData.Creature.IsPlayerControlled(out NwPlayer? player))
            player.SendServerMessage("You cannot perform that action on a friendly target due to PvP settings");
    }

    private static void PreventTechniqueInNoCastingArea(OnUseFeat eventData)
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
    }
}
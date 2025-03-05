// General monk ability handling that's applied across abilities
using AmiaReforged.Classes.Monk.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
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
    private static NwCreature? _monk;
    
    private static readonly bool HasArmor = _monk?.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;
    
    private static readonly bool HasShield =
        _monk?.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category is BaseItemCategory.Shield;
    
    private static readonly bool HasFocusWithoutUnarmed = _monk?.GetItemInSlot(InventorySlot.RightHand) is not null
                                                           && _monk.GetItemInSlot(InventorySlot.LeftHand)!.BaseItem.Id == FocusId;
     
    
    
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    public AbilityRestrictionsHandler()
    {
        string environment = UtilPlugin.GetEnvironmentVariable("SERVER_MODE");

        if (environment == "live") return;
        
        NwModule.Instance.OnModuleLoad += HideDefaultFeedback;
        NwModule.Instance.OnUseFeat += EnforceTechniqueRestrictions;
        NwModule.Instance.OnEffectApply += PreventMartialTechnique;
        NwModule.Instance.OnEffectApply += PreventStaticBonuses;
        NwModule.Instance.OnUseFeat += PreventHostileTechniqueToFriendly;
        NwModule.Instance.OnUseFeat += PreventTechniqueInNoCastingArea;
        Log.Info("Monk Ability Restrictions Handler initialized.");
    }
    private static void HideDefaultFeedback(ModuleEvents.OnModuleLoad eventData)
    {
        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_EQUIP_MONK_ABILITIES, NWScript.TRUE);
    }

    private static void EnforceTechniqueRestrictions(OnUseFeat eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk) is null) return;

        if (eventData.Feat.Id is not (MonkFeat.EmptyBody or MonkFeat.KiBarrier or MonkFeat.KiShout
            or MonkFeat.WholenessOfBody or MonkFeat.QuiveringPalm or MonkFeat.StunningStrike
            or MonkFeat.EagleStrike or MonkFeat.AxiomaticStrike)) return;

        _monk = eventData.Creature;
            
        if (HasArmor || HasShield || HasFocusWithoutUnarmed)
            eventData.PreventFeatUse = true;
        
        if (!_monk.IsPlayerControlled(out NwPlayer? player)) return;
            
        if (HasArmor)
            player.SendServerMessage($"Having equipped an armor has prevented your {eventData.Feat.Name}.");
        if (HasShield)
            player.SendServerMessage($"Having equipped a shield has prevented your {eventData.Feat.Name}.");
        if (HasFocusWithoutUnarmed)
            player.SendServerMessage($"Having equipped a focus without being unarmed has prevented your {eventData.Feat.Name}.");
    }
    
    private static void PreventMartialTechnique(OnEffectApply eventData)
    {
        if (eventData.Object is not NwCreature creature) return;
        
        // Effect must be a martial technique
        if (eventData.Effect.Tag is not (MonkTechnique.StunningTag or MonkTechnique.EagleTag 
            or MonkTechnique.AxiomaticTag)) return;

        _monk = creature;

        if (HasArmor || HasShield || HasFocusWithoutUnarmed)
            eventData.PreventApply = true;
        
        string techniqueName = eventData.Effect.Tag switch
        {
            MonkTechnique.StunningTag => "Stunning Strike",
            MonkTechnique.EagleTag => "Eagle Strike",
            MonkTechnique.AxiomaticTag  => "Axiomatic Strike",
            _ => ""
        };
        
        if (!_monk.IsPlayerControlled(out NwPlayer? player)) return;
        
        if (HasArmor)
            player.SendServerMessage($"Having equipped an armor has prevented your {techniqueName}.");
        if (HasShield)
            player.SendServerMessage($"Having equipped a shield has prevented your {techniqueName}.");
        if (HasFocusWithoutUnarmed)
            player.SendServerMessage($"Having equipped a focus without being unarmed has prevented your {techniqueName}.");
    }
    
    /// <summary>
    /// This event function is called by the StaticBonusesService for checking equipment restrictions
    /// </summary>
    private static void PreventStaticBonuses(OnEffectApply eventData)
    {
        if (eventData.Object is not NwCreature creature) return;
        
        if (eventData.Effect.Tag is not "monk_staticbonuses") return;
            
        _monk = creature;
        
        if (HasArmor || HasShield || HasFocusWithoutUnarmed)
            eventData.PreventApply = true;
        
        if (!_monk.IsPlayerControlled(out NwPlayer? player)) return;
        
        if (HasArmor)
            player.SendServerMessage("Equipping this armor has disabled your monk abilities.");
        if (HasShield)
            player.SendServerMessage("Equipping this shield has disabled your monk abilities.");
        if (HasFocusWithoutUnarmed)
            player.SendServerMessage("Equipping a focus without being unarmed has disabled your monk abilities.");
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

        if (eventData.Feat.Id is not (MonkFeat.EmptyBody or MonkFeat.KiBarrier or MonkFeat.KiShout
            or MonkFeat.WholenessOfBody or MonkFeat.QuiveringPalm)) return;

        eventData.PreventFeatUse = true;
        if (eventData.Creature.IsPlayerControlled(out NwPlayer? player))
            player.SendServerMessage("- You cannot cast magic in this area! -");
    }
}
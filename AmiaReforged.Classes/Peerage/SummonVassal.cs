using System.ComponentModel;
using AmiaReforged.Classes.EffectUtils.Summoning;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Peerage;

[ServiceBinding(typeof(SummonVassal))]
public class SummonVassal(HenchmanService henchmanService)
{
    private const string VassalResRefPrefix = "summon_vassal";
    private const string VassalWidgetTag = "l_vassal";
    private const int PeerageClassIndex = 54;

    [ScriptHandler("summon_vassal")]
    public void SummonVassalHandler(CallInfo info)
    {
        if (info.ObjectSelf is not NwCreature peer
            || HasVassal(peer)
            || !peer.IsPlayerControlled(out NwPlayer? player)
            || peer.Location == null)
            return;

        NwItem? vassalWidget = peer.Inventory.Items.FirstOrDefault(item => item.Tag == VassalWidgetTag);
        if (vassalWidget == null)
        {
            player.SendServerMessage("You do not have a vassal widget. Summoning failed. Contact a DM!");
            return;
        }

        string vassalResRef = GetVassalResRef(peer, vassalWidget);
        henchmanService.SummonHenchman(summoner: peer, vassalResRef, peer.Location, henchman: out NwCreature? vassal);
        if (vassal == null) return;

        Effect persuadeBonusEffect = GetPersuadeBonusEffect(peer.GetSkillRank(Skill.Persuade!, true));
        vassal.ApplyEffect(EffectDuration.Permanent, persuadeBonusEffect);

        string? vassalName = vassalWidget.GetObjectVariable<LocalVariableString>("vassalName").Value;
        if (vassalName != null)
        {
            vassal.Name = vassalName;
        }
    }

    private static Effect GetPersuadeBonusEffect(int persuadeRank)
    {
        Effect vassalEffect = Effect.VisualEffect(VfxType.None);
        if (persuadeRank < 10) return vassalEffect;

        List<Effect> effects = [];

        switch (persuadeRank)
        {
            case >= 30:
                effects.Add(Effect.AbilityIncrease(Ability.Strength, 4));
                effects.Add(Effect.AbilityIncrease(Ability.Constitution, 6));
                effects.Add(Effect.Regenerate(1, TimeSpan.FromSeconds(6)));
                break;

            case >= 25:
                effects.Add(Effect.AbilityIncrease(Ability.Strength, 2));
                effects.Add(Effect.AbilityIncrease(Ability.Constitution, 5));
                break;

            case >= 20:
                effects.Add(Effect.AbilityIncrease(Ability.Constitution, 4));
                break;

            case >= 15:
                effects.Add(Effect.AbilityIncrease(Ability.Constitution, 3));
                break;

            case >= 10:
                effects.Add(Effect.AbilityIncrease(Ability.Constitution, 2));
                break;
        }

        foreach (Effect effect in effects)
        {
            vassalEffect = Effect.LinkEffects(vassalEffect, effect);
        }
        vassalEffect.SubType = EffectSubType.Unyielding;
        return vassalEffect;
    }


    private static string GetVassalResRef(NwCreature peer, NwItem vassalWidget)
    {
        bool vassalIsFemale = vassalWidget.GetObjectVariable<LocalVariableBool>("vassalFemale").Value;
        int vassalRace = vassalWidget.GetObjectVariable<LocalVariableInt>("vassalRace").Value;
        int peerageLevel = NWScript.GetLevelByClass(PeerageClassIndex, peer);
        bool isEpicVassal = peer.Level >= 20 && peerageLevel == 5;

        if (vassalRace == 0)
        {
            vassalRace = 1;
        }

        // Default to human if race value is out of range
        if (vassalRace is < 1 or > 5)
            vassalRace = 1;

        // This logic is a bit of a mess due to how the numbering was designed in the creature blueprints
        int variantOffset = (vassalIsFemale, isEpicVassal) switch
        {
            (false, false) => 1,
            (false, true) => 2,
            (true, false) => 3,
            (true, true) => 4
        };

        int vassalNumber = ((vassalRace - 1) * 4) + variantOffset;

        return $"{VassalResRefPrefix}{vassalNumber}";
    }

    private static bool HasVassal(NwCreature peer)
        => peer.Associates.Any(a => a.ResRef.StartsWith(VassalResRefPrefix));
}

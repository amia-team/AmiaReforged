using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.IdentityModel.Tokens;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.CharacterCustomization;

[ServiceBinding(typeof(MagicalQuiver))]
public class MagicalQuiver
{
    private readonly MagicalQuiver _magicalQuiver;
    private const string QuiverTag = "magical_quiver";

    private const string PcKeyResRef = "ds_pckey";

    private const string MagicalQuiverPcKeyLocalObject = "magic_quiver";
    private const string IsSetUpLocalInt = "is_setup";
    private const string SelectedRaceLocalInt = "selected_race";

    private const string QuiverVfxTag = "quiver_vfx";
    private const string ArrowVfxTag = "arrow_vfx";

    private readonly Dictionary<int, string> _raceLabels = new()
    {
        { 0, "Dwarf Size" },
        { 1, "Elf Size" },
        { 2, "Gnome Size" },
        { 3, "Hin Size" },
        { 4, "Half-Elf Size" },
        { 5, "Half-Orc Size" },
        { 6, "Human Size" }
    };
    public MagicalQuiver(MagicalQuiver magicalQuiver)
    {
        NwModule.Instance.OnItemUse += HandleMagicalQuiver;
        _magicalQuiver = magicalQuiver;
    }

    private void HandleMagicalQuiver(OnItemUse obj)
    {
        if (obj.Item.Tag != QuiverTag) return;

        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;

        NwItem? pcKey = obj.UsedBy.Inventory.Items.FirstOrDefault(i => i.Tag == PcKeyResRef);
        if (pcKey is null) return;

        bool isSetUp = NWScript.GetLocalInt(obj.Item, IsSetUpLocalInt) == NWScript.TRUE;

        switch (obj.Item.Tag)
        {
            case QuiverTag:
                if (!isSetUp)
                {
                    NWScript.SetLocalObject(pcKey, MagicalQuiverPcKeyLocalObject, obj.Item);
                    player.ActionStartConversation(player.LoginCreature!, "quivracesel_conv", true, false);
                    return;
                }

                NWScript.SetLocalObject(pcKey, MagicalQuiverPcKeyLocalObject, obj.Item);
                player.ActionStartConversation(player.LoginCreature!, "quivchanger_conv", true, false);
                return;

            default:
                return;
        }
    }

    [ScriptHandler(scriptName: "quiv_race_select")]
    public void QuiverRaceSelection(CallInfo info)
    {
        uint player = info.ObjectSelf;
        int selectedRace = NWScript.GetLocalInt(player, "race");

        if (NWScript.GetIsObjectValid(player) != NWScript.TRUE)
        {
            return;
        }


        uint pcKey = NWScript.GetItemPossessedBy(player, PcKeyResRef);

        uint magicalQuiver = NWScript.GetLocalObject(pcKey, MagicalQuiverPcKeyLocalObject);
        NWScript.SetLocalInt(magicalQuiver, SelectedRaceLocalInt, selectedRace);
        NWScript.SetLocalInt(magicalQuiver, IsSetUpLocalInt, NWScript.TRUE);

        NWScript.SetName(magicalQuiver, $"{_raceLabels[selectedRace]} Quiver");
    }

    [ScriptHandler(scriptName: "quiver_select")]
    public void QuiverSelection(CallInfo info)
    {
        uint player = info.ObjectSelf;

        int selectedQuiver = NWScript.GetLocalInt(player, "quiver");

        if (NWScript.GetIsObjectValid(player) != NWScript.TRUE)
        {
            return;
        }

        NwCreature? playerCreature = player.ToNwObjectSafe<NwCreature>();

        if (playerCreature is null)
        {
            return;
        }


        WearableQuiver quiverEnum = (WearableQuiver)selectedQuiver;
        NwItem? pcKey = playerCreature.Inventory.Items.FirstOrDefault(i => i.Tag == PcKeyResRef);
        if (pcKey is null)
        {
            return;
        }


        Effect? existingQuiver = playerCreature.ActiveEffects.FirstOrDefault(e => e.Tag == QuiverVfxTag);
        if (existingQuiver is not null)
        {
            playerCreature.RemoveEffect(existingQuiver);
        }

        // The quiver was already removed, so return if their selection was "none"
        if (quiverEnum == WearableQuiver.None)
        {
            return;
        }

        uint magicalquiver = NWScript.GetLocalObject(pcKey, MagicalQuiverPcKeyLocalObject);
        int selectedRace = NWScript.GetLocalInt(magicalquiver, SelectedRaceLocalInt);
        int gender = NWScript.GetGender(player);

        int? vfx = gender == NWScript.GENDER_MALE
            ? _magicalQuiver.QuiverForRace[selectedRace].maleQuivers.GetValueOrDefault(quiverEnum)
            : _magicalQuiver.QuiverForRace[selectedRace].femaleQuivers.GetValueOrDefault(quiverEnum);

        if (vfx is null)
        {
            NWScript.SendMessageToPC(playerCreature,
                $"BUG REPORT: Could not select quiver from {selectedQuiver}. Screenshot this and send this to staff on Discord or on the Forums");
            return;
        }

        VisualEffectTableEntry quiverEntry = NwGameTables.VisualEffectTable.GetRow((int)vfx);
        Effect quiverVfx = Effect.VisualEffect(quiverEntry);
        quiverVfx.Tag = QuiverVfxTag;

        playerCreature.ApplyEffect(EffectDuration.Permanent, quiverVfx);
    }
}

[ServiceBinding(typeof(MagicQuiver))]
public sealed class MagicQuiver
{
    public Dictionary<int, RaceQuivers> QuiversForRace { get; }
    public Dictionary<int, RaceArrows> ArrowsForRace { get; }


    public MagicQuiver()
    {
        QuiversForRace = new Dictionary<int, RaceQuivers>
        {
            { 0, _dwarfQuivers },
            { 1, _elfQuivers },
            { 2, _hinQuivers },
            { 3, _hinQuivers },
            { 4, _humanQuivers },
            { 5, _halfOrcQuivers },
            { 6, _humanQuivers }
        };

        ArrowsForRace = new Dictionary<int, RaceArrows>
        {
            { 0, _dwarfArrows },
            { 1, _elfArrows },
            { 2, _hinArrows },
            { 3, _hinArrows },
            { 4, _humanArrows },
            { 5, _halfOrcArrows },
            { 6, _humanArrows },
        };
    }


    private readonly RaceQuivers _dwarfQuivers = new(new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 0 },
            { WearableQuiver.QuiverBlack, 0 },
            { WearableQuiver.QuiverBlue, 0 },
            { WearableQuiver.QuiverWhite, 0 },
            { WearableQuiver.QuiverGray, 0 },
            { WearableQuiver.QuiverAqua, 0 },
            { WearableQuiver.QuiverGreen, 0 },
            { WearableQuiver.QuiverPurple, 0 },
            { WearableQuiver.QuiverYellow, 0 },
            { WearableQuiver.QuiverRed, 0 },
            { WearableQuiver.ArrowRed, 0 },
            { WearableQuiver.ArrowBlue, 0 },
            { WearableQuiver.ArrowGreen, 0 },
            { WearableQuiver.ArrowGray, 0 },
            { WearableQuiver.ArrowWhite, 0 },
            { WearableQuiver.ArrowBlack, 0 },
            { WearableQuiver.ArrowYellow, 0 },
            { WearableQuiver.ArrowOrange, 0 },
            { WearableQuiver.ArrowPurple, 0 },
            { WearableQuiver.ArrowAqua, 0 }

        }, new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 0 },
            { WearableQuiver.QuiverBlack, 0 },
            { WearableQuiver.QuiverBlue, 0 },
            { WearableQuiver.QuiverWhite, 0 },
            { WearableQuiver.QuiverGray, 0 },
            { WearableQuiver.QuiverAqua, 0 },
            { WearableQuiver.QuiverGreen, 0 },
            { WearableQuiver.QuiverPurple, 0 },
            { WearableQuiver.QuiverYellow, 0 },
            { WearableQuiver.QuiverRed, 0 },
            { WearableQuiver.ArrowRed, 0 },
            { WearableQuiver.ArrowBlue, 0 },
            { WearableQuiver.ArrowGreen, 0 },
            { WearableQuiver.ArrowGray, 0 },
            { WearableQuiver.ArrowWhite, 0 },
            { WearableQuiver.ArrowBlack, 0 },
            { WearableQuiver.ArrowYellow, 0 },
            { WearableQuiver.ArrowOrange, 0 },
            { WearableQuiver.ArrowPurple, 0 },
            { WearableQuiver.ArrowAqua, 0 }
        }
    );

    private readonly RaceQuivers _humanQuivers = new RaceQuivers(new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 0 },
            { WearableQuiver.QuiverBlack, 0 },
            { WearableQuiver.QuiverBlue, 0 },
            { WearableQuiver.QuiverWhite, 0 },
            { WearableQuiver.QuiverGray, 0 },
            { WearableQuiver.QuiverAqua, 0 },
            { WearableQuiver.QuiverGreen, 0 },
            { WearableQuiver.QuiverPurple, 0 },
            { WearableQuiver.QuiverYellow, 0 },
            { WearableQuiver.QuiverRed, 0 },
            { WearableQuiver.ArrowRed, 0 },
            { WearableQuiver.ArrowBlue, 0 },
            { WearableQuiver.ArrowGreen, 0 },
            { WearableQuiver.ArrowGray, 0 },
            { WearableQuiver.ArrowWhite, 0 },
            { WearableQuiver.ArrowBlack, 0 },
            { WearableQuiver.ArrowYellow, 0 },
            { WearableQuiver.ArrowOrange, 0 },
            { WearableQuiver.ArrowPurple, 0 },
            { WearableQuiver.ArrowAqua, 0 }

        }, new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 0 },
            { WearableQuiver.QuiverBlack, 0 },
            { WearableQuiver.QuiverBlue, 0 },
            { WearableQuiver.QuiverWhite, 0 },
            { WearableQuiver.QuiverGray, 0 },
            { WearableQuiver.QuiverAqua, 0 },
            { WearableQuiver.QuiverGreen, 0 },
            { WearableQuiver.QuiverPurple, 0 },
            { WearableQuiver.QuiverYellow, 0 },
            { WearableQuiver.QuiverRed, 0 },
            { WearableQuiver.ArrowRed, 0 },
            { WearableQuiver.ArrowBlue, 0 },
            { WearableQuiver.ArrowGreen, 0 },
            { WearableQuiver.ArrowGray, 0 },
            { WearableQuiver.ArrowWhite, 0 },
            { WearableQuiver.ArrowBlack, 0 },
            { WearableQuiver.ArrowYellow, 0 },
            { WearableQuiver.ArrowOrange, 0 },
            { WearableQuiver.ArrowPurple, 0 },
            { WearableQuiver.ArrowAqua, 0 }
        }
    );

    private readonly RaceQuivers _elfQuivers = new RaceQuivers(new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 0 },
            { WearableQuiver.QuiverBlack, 0 },
            { WearableQuiver.QuiverBlue, 0 },
            { WearableQuiver.QuiverWhite, 0 },
            { WearableQuiver.QuiverGray, 0 },
            { WearableQuiver.QuiverAqua, 0 },
            { WearableQuiver.QuiverGreen, 0 },
            { WearableQuiver.QuiverPurple, 0 },
            { WearableQuiver.QuiverYellow, 0 },
            { WearableQuiver.QuiverRed, 0 },
            { WearableQuiver.ArrowRed, 0 },
            { WearableQuiver.ArrowBlue, 0 },
            { WearableQuiver.ArrowGreen, 0 },
            { WearableQuiver.ArrowGray, 0 },
            { WearableQuiver.ArrowWhite, 0 },
            { WearableQuiver.ArrowBlack, 0 },
            { WearableQuiver.ArrowYellow, 0 },
            { WearableQuiver.ArrowOrange, 0 },
            { WearableQuiver.ArrowPurple, 0 },
            { WearableQuiver.ArrowAqua, 0 }

        }, new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 0 },
            { WearableQuiver.QuiverBlack, 0 },
            { WearableQuiver.QuiverBlue, 0 },
            { WearableQuiver.QuiverWhite, 0 },
            { WearableQuiver.QuiverGray, 0 },
            { WearableQuiver.QuiverAqua, 0 },
            { WearableQuiver.QuiverGreen, 0 },
            { WearableQuiver.QuiverPurple, 0 },
            { WearableQuiver.QuiverYellow, 0 },
            { WearableQuiver.QuiverRed, 0 },
            { WearableQuiver.ArrowRed, 0 },
            { WearableQuiver.ArrowBlue, 0 },
            { WearableQuiver.ArrowGreen, 0 },
            { WearableQuiver.ArrowGray, 0 },
            { WearableQuiver.ArrowWhite, 0 },
            { WearableQuiver.ArrowBlack, 0 },
            { WearableQuiver.ArrowYellow, 0 },
            { WearableQuiver.ArrowOrange, 0 },
            { WearableQuiver.ArrowPurple, 0 },
            { WearableQuiver.ArrowAqua, 0 }
        }
    );

    private readonly RaceQuivers _hinQuivers = new RaceQuivers(new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 0 },
            { WearableQuiver.QuiverBlack, 0 },
            { WearableQuiver.QuiverBlue, 0 },
            { WearableQuiver.QuiverWhite, 0 },
            { WearableQuiver.QuiverGray, 0 },
            { WearableQuiver.QuiverAqua, 0 },
            { WearableQuiver.QuiverGreen, 0 },
            { WearableQuiver.QuiverPurple, 0 },
            { WearableQuiver.QuiverYellow, 0 },
            { WearableQuiver.QuiverRed, 0 },
            { WearableQuiver.ArrowRed, 0 },
            { WearableQuiver.ArrowBlue, 0 },
            { WearableQuiver.ArrowGreen, 0 },
            { WearableQuiver.ArrowGray, 0 },
            { WearableQuiver.ArrowWhite, 0 },
            { WearableQuiver.ArrowBlack, 0 },
            { WearableQuiver.ArrowYellow, 0 },
            { WearableQuiver.ArrowOrange, 0 },
            { WearableQuiver.ArrowPurple, 0 },
            { WearableQuiver.ArrowAqua, 0 }
        },
        new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 0 },
            { WearableQuiver.QuiverBlack, 0 },
            { WearableQuiver.QuiverBlue, 0 },
            { WearableQuiver.QuiverWhite, 0 },
            { WearableQuiver.QuiverGray, 0 },
            { WearableQuiver.QuiverAqua, 0 },
            { WearableQuiver.QuiverGreen, 0 },
            { WearableQuiver.QuiverPurple, 0 },
            { WearableQuiver.QuiverYellow, 0 },
            { WearableQuiver.QuiverRed, 0 },
            { WearableQuiver.ArrowRed, 0 },
            { WearableQuiver.ArrowBlue, 0 },
            { WearableQuiver.ArrowGreen, 0 },
            { WearableQuiver.ArrowGray, 0 },
            { WearableQuiver.ArrowWhite, 0 },
            { WearableQuiver.ArrowBlack, 0 },
            { WearableQuiver.ArrowYellow, 0 },
            { WearableQuiver.ArrowOrange, 0 },
            { WearableQuiver.ArrowPurple, 0 },
            { WearableQuiver.ArrowAqua, 0 }
        }
    );

    private readonly RaceQuivers _halfOrcQuivers = new RaceQuivers(new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 0 },
            { WearableQuiver.QuiverBlack, 0 },
            { WearableQuiver.QuiverBlue, 0 },
            { WearableQuiver.QuiverWhite, 0 },
            { WearableQuiver.QuiverGray, 0 },
            { WearableQuiver.QuiverAqua, 0 },
            { WearableQuiver.QuiverGreen, 0 },
            { WearableQuiver.QuiverPurple, 0 },
            { WearableQuiver.QuiverYellow, 0 },
            { WearableQuiver.QuiverRed, 0 },
            { WearableQuiver.ArrowRed, 0 },
            { WearableQuiver.ArrowBlue, 0 },
            { WearableQuiver.ArrowGreen, 0 },
            { WearableQuiver.ArrowGray, 0 },
            { WearableQuiver.ArrowWhite, 0 },
            { WearableQuiver.ArrowBlack, 0 },
            { WearableQuiver.ArrowYellow, 0 },
            { WearableQuiver.ArrowOrange, 0 },
            { WearableQuiver.ArrowPurple, 0 },
            { WearableQuiver.ArrowAqua, 0 }
        },
        new Dictionary<WearableQuiver, int>()
        {
            { WearableQuiver.QuiverBrown, 0 },
            { WearableQuiver.QuiverBlack, 0 },
            { WearableQuiver.QuiverBlue, 0 },
            { WearableQuiver.QuiverWhite, 0 },
            { WearableQuiver.QuiverGray, 0 },
            { WearableQuiver.QuiverAqua, 0 },
            { WearableQuiver.QuiverGreen, 0 },
            { WearableQuiver.QuiverPurple, 0 },
            { WearableQuiver.QuiverYellow, 0 },
            { WearableQuiver.QuiverRed, 0 },
            { WearableQuiver.ArrowRed, 0 },
            { WearableQuiver.ArrowBlue, 0 },
            { WearableQuiver.ArrowGreen, 0 },
            { WearableQuiver.ArrowGray, 0 },
            { WearableQuiver.ArrowWhite, 0 },
            { WearableQuiver.ArrowBlack, 0 },
            { WearableQuiver.ArrowYellow, 0 },
            { WearableQuiver.ArrowOrange, 0 },
            { WearableQuiver.ArrowPurple, 0 },
            { WearableQuiver.ArrowAqua, 0 }
        }
    );
}

public record RaceQuivers(Dictionary<WearableQuiver, int> maleQuivers, Dictionary<WearableQuiver, int> femaleQuivers);

public enum WearableQuiver
{
    None = 0,

    // Quivers
    QuiverBrown = 1,
    QuiverBlack = 2,
    QuiverBlue = 3,
    QuiverWhite = 4,
    QuiverGray = 5,
    QuiverAqua = 6,
    QuiverGreen = 7,
    QuiverPurple = 8,
    QuiverYellow = 9,
    QuiverRed = 10,

    // Arrows
    ArrowRed = 11,
    ArrowBlue = 12,
    ArrowGreen = 13,
    ArrowGray = 14,
    ArrowWhite = 15,
    ArrowBlack = 16,
    ArrowYellow = 17,
    ArrowOrange = 18,
    ArrowPurple = 19,
    ArrowAqua = 20,
}


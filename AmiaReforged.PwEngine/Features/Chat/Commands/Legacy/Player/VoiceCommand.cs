using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.Player;

/// <summary>
/// Make a companion, familiar, summon, or other associate speak text.
/// Ported from f_Voice() in mod_pla_cmd.nss.
/// Usage: ./voice &lt;type&gt; &lt;text&gt;
/// Types: c(ompanion), f(amiliar), s(ummon), d(ominated), b(ottled), v(assal), h(enchman)
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class VoiceCommand : IChatCommand
{
    public string Command => "./voice";
    public string Description => "Make associate speak: c/f/s/d/b/v/h <text>";
    public string AllowedRoles => "Player";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        await NwTask.SwitchToMainThread();

        NwCreature? creature = caller.LoginCreature;
        if (creature == null) return;

        if (args.Length < 1)
        {
            ShowHelp(caller);
            return;
        }

        string option = args[0].ToLowerInvariant();
        if (option is "?" or "help")
        {
            ShowHelp(caller);
            return;
        }

        if (args.Length < 2)
        {
            caller.SendServerMessage("Usage: ./voice <type> <text to say>", ColorConstants.Orange);
            return;
        }

        string text = string.Join(" ", args[1..]);
        NwCreature? target = ResolveAssociate(creature, option, caller);

        if (target == null)
        {
            caller.SendServerMessage($"No valid associate found for type '{option}'.", ColorConstants.Orange);
            return;
        }

        // Copy language locals from PC to target (for language system compatibility)
        CopyLanguageLocals(creature, target);

        target.SpeakString(text);
        caller.SendServerMessage($"Assigning text to {target.Name}.", ColorConstants.Lime);
    }

    private static NwCreature? ResolveAssociate(NwCreature creature, string type, NwPlayer caller)
    {
        string cdKey = caller.CDKey;

        return type switch
        {
            "c" => creature.GetAssociate(AssociateType.AnimalCompanion),
            "f" => creature.GetAssociate(AssociateType.Familiar),
            "s" => creature.GetAssociate(AssociateType.Summoned),
            "d" => creature.GetAssociate(AssociateType.Dominated),
            "b" => ResolveBottledCompanion(creature, cdKey),
            "v" => FindNearestByTag(creature, $"vassal_{cdKey}"),
            "h" => ResolveHenchman(creature, cdKey),
            _ => null
        };
    }

    private static NwCreature? ResolveBottledCompanion(NwCreature creature, string cdKey)
    {
        // Check if a bottled companion is marked by UUID
        int isMarked = creature.GetObjectVariable<LocalVariableInt>("marked_bc").Value;
        if (isMarked == 1)
        {
            string uuid = creature.GetObjectVariable<LocalVariableString>("bc_marked").Value ?? "";
            if (!string.IsNullOrEmpty(uuid))
            {
                NwGameObject? obj = NWScript.GetObjectByUUID(uuid).ToNwObject<NwCreature>();
                if (obj is NwCreature markedCreature)
                    return markedCreature;
            }
        }

        // Fall back to finding by tag
        return FindNearestByTag(creature, $"ds_npc_{cdKey}");
    }

    private static NwCreature? ResolveHenchman(NwCreature creature, string cdKey)
    {
        // If henchman 1 is the vassal, use henchman 2
        NwCreature? vassal = FindNearestByTag(creature, $"vassal_{cdKey}");
        NwCreature? henchman1 = creature.GetAssociate(AssociateType.Henchman);

        if (vassal != null && henchman1 != null && henchman1.Tag == vassal.Tag)
        {
            // Get second henchman
            return NWScript.GetHenchman(creature, 2).ToNwObject<NwCreature>();
        }

        return henchman1;
    }

    private static NwCreature? FindNearestByTag(NwCreature creature, string tag)
    {
        return NWScript.GetNearestObjectByTag(tag, creature).ToNwObject<NwCreature>();
    }

    private static void CopyLanguageLocals(NwCreature source, NwCreature target)
    {
        target.GetObjectVariable<LocalVariableInt>("chat_language").Value =
            source.GetObjectVariable<LocalVariableInt>("chat_language").Value;
        target.GetObjectVariable<LocalVariableInt>("chat_reverse").Value =
            source.GetObjectVariable<LocalVariableInt>("chat_reverse").Value;
        target.GetObjectVariable<LocalVariableString>("chat_emote").Value =
            source.GetObjectVariable<LocalVariableString>("chat_emote").Value ?? "";
    }

    private static void ShowHelp(NwPlayer caller)
    {
        caller.SendServerMessage("=== Voice Command ===", ColorConstants.Cyan);
        caller.SendServerMessage("Usage: ./voice <type> <text>", ColorConstants.White);
        caller.SendServerMessage("Types:", ColorConstants.Yellow);
        caller.SendServerMessage("  c - Animal Companion", ColorConstants.White);
        caller.SendServerMessage("  f - Familiar", ColorConstants.White);
        caller.SendServerMessage("  s - Summon", ColorConstants.White);
        caller.SendServerMessage("  d - Dominated creature", ColorConstants.White);
        caller.SendServerMessage("  b - Bottled Companion", ColorConstants.White);
        caller.SendServerMessage("  v - Vassal", ColorConstants.White);
        caller.SendServerMessage("  h - Henchman", ColorConstants.White);
        caller.SendServerMessage("Example: ./voice f Hello world!", ColorConstants.White);
    }
}

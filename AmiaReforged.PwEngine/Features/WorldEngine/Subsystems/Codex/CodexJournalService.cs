using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;
using NWN.Core.NWNX;
using NWN.Native.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex;

[ServiceBinding(typeof(CodexJournalService))]
public class CodexJournalService
{
    private readonly ICodexSubsystem _codex;
    private const string OpenJournalHandle = "open_codex";
    private const string CloseJournalHandle = "close_codex";

    private const string OpenCodexWithJournalVar = "OPEN_CODEX_WITH_JOURNAL";

    public CodexJournalService(ScriptHandleFactory handleFactory, ICodexSubsystem codex)
    {
        _codex = codex;

        bool isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
        if (!isEnabled) return;

        EventsPlugin.SubscribeEvent(EventsPlugin.NWNX_ON_JOURNAL_OPEN_AFTER, OpenJournalHandle);
        EventsPlugin.SubscribeEvent(EventsPlugin.NWNX_ON_JOURNAL_CLOSE_AFTER, CloseJournalHandle);

        handleFactory.RegisterScriptHandler(OpenJournalHandle, HandleJournalOpen);
        handleFactory.RegisterScriptHandler(CloseJournalHandle, HandleJournalClose);
    }


    private ScriptHandleResult HandleJournalOpen(CallInfo arg)
    {
        if (NWScript.StringToObject(EventsPlugin.GetEventData("CREATURE")).ToNwObject<NwCreature>() is not NwCreature
                creature || !creature.IsPlayerControlled(out NwPlayer? player))
        {
            return ScriptHandleResult.Handled;
        }

        NwItem? pcKey = creature.Inventory.Items.FirstOrDefault(item => item.ResRef == "ds_pckey");
        if (pcKey is null)
        {
            return ScriptHandleResult.Handled;
        }

        bool openCodexWithJournal = pcKey.GetObjectVariable<LocalVariableBool>(OpenCodexWithJournalVar).Value;

        if (openCodexWithJournal)
        {
            _codex.OpenCodexAsync(player);
        }

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult HandleJournalClose(CallInfo arg)
    {
        if (NWScript.StringToObject(EventsPlugin.GetEventData("CREATURE")).ToNwObject<NwCreature>() is not NwCreature
                creature || !creature.IsPlayerControlled(out NwPlayer? player))
        {
            return ScriptHandleResult.Handled;
        }

        NwItem? pcKey = creature.Inventory.Items.FirstOrDefault(item => item.ResRef == "ds_pckey");
        if (pcKey is null)
        {
            return ScriptHandleResult.Handled;
        }

        bool openCodexWithJournal = pcKey.GetObjectVariable<LocalVariableBool>(OpenCodexWithJournalVar).Value;

        if (openCodexWithJournal)
        {
            _codex.CloseCodexAsync(player);
        }

        return ScriptHandleResult.Handled;
    }
}

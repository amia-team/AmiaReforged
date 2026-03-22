using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NLog.Fluent;
using NWN.Core;
using NWN.Core.NWNX;
using NWN.Native.API;
using JournalEntry = Anvil.API.JournalEntry;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex;

[ServiceBinding(typeof(CodexJournalService))]
public class CodexJournalService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
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

        NwModule.Instance.OnClientEnter += NotifyOfCodex;
    }

    private void NotifyOfCodex(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.IsDM) return;

        JournalEntry codexData = new JournalEntry
        {
            Name = "The Codex",
            Text = "The Codex is your personal journal where you can track quests, lore, notes, and reputation. To open the Codex, simply open your journal when you receive your PC key. If you ever decide that this is not the behavior you wish, you can type ./codex no-journal into the talk channel and use ./codex to open the codex directly.",
        };
        obj.Player.AddCustomJournalEntry(codexData);
    }


    private ScriptHandleResult HandleJournalOpen(CallInfo arg)
    {
        NwObject? gameObject = arg.ObjectSelf;

        if (gameObject is null)
        {
            Log.Info("Creature somehow null????");
            return ScriptHandleResult.Handled;
        }

        if (gameObject is not NwCreature creature)
        {
            Log.Info("Somehow, a non-creature opened the journal????");
            return ScriptHandleResult.Handled;
        }

        if (!creature.IsPlayerControlled(out NwPlayer? player))
        {
            return ScriptHandleResult.Handled;
        }

        NwItem? pcKey = creature.Inventory.Items.FirstOrDefault(item => item.ResRef == "ds_pckey");
        if (pcKey is null)
        {
            Log.Info("Player doesn't have a pcKey");
            return ScriptHandleResult.Handled;
        }


        Log.Info($"Opening codex for {player.PlayerName}");

        if (creature.GetObjectVariable<LocalVariableInt>("no_journal").Value == 1)
        {
            return ScriptHandleResult.Handled;
        }

        _codex.OpenCodexAsync(player);

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult HandleJournalClose(CallInfo arg)
    {
        NwObject? gameObject = arg.ObjectSelf;

        if (gameObject is null)
        {
            Log.Info("Creature somehow null????");
        }

        if (gameObject is not NwCreature creature)
        {
            Log.Info("Somehow, a non-creature opened the journal????");
            return ScriptHandleResult.Handled;
        }

        if (!creature.IsPlayerControlled(out NwPlayer? player))
        {
            return ScriptHandleResult.Handled;
        }

        NwItem? pcKey = creature.Inventory.Items.FirstOrDefault(item => item.ResRef == "ds_pckey");
        if (pcKey is null)
        {
            return ScriptHandleResult.Handled;
        }

        if (creature.GetObjectVariable<LocalVariableInt>("no_journal").Value == 1)
        {
            return ScriptHandleResult.Handled;
        }

        _codex.CloseCodexAsync(player);

        return ScriptHandleResult.Handled;
    }
}

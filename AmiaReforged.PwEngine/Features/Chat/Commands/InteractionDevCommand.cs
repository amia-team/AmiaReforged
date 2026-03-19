using System.Text;
using AmiaReforged.PwEngine.Features.Glyph.Integration;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;
using Anvil.API;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Chat.Commands;

/// <summary>
/// Temporary dev/test command for QA-ing the Interaction Framework.
/// Disabled on the live server via <c>SERVER_MODE</c> environment variable.
/// <list type="bullet">
///   <item><c>./interaction list</c> — lists all registered interaction types</item>
///   <item><c>./interaction &lt;tag&gt;</c> — performs a tick of the named interaction on the caller</item>
///   <item><c>./interaction &lt;tag&gt; auto</c> — runs the interaction to completion (all rounds)</item>
/// </list>
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class InteractionDevCommand : IChatCommand
{
    private readonly IInteractionSubsystem _interactions;
    private readonly RuntimeCharacterService _characters;
    private readonly GlyphInteractionHookService? _glyphHook;
    private readonly bool _isEnabled;

    public InteractionDevCommand(
        IInteractionSubsystem interactions,
        RuntimeCharacterService characters,
        GlyphInteractionHookService? glyphHook = null)
    {
        _interactions = interactions;
        _characters = characters;
        _glyphHook = glyphHook;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public string Command => "./interaction";
    public string Description => "Dev tool — list or perform interactions (disabled on live)";
    public string AllowedRoles => "All";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!_isEnabled)
        {
            caller.SendServerMessage("This command is disabled on the live server.", ColorConstants.Red);
            return;
        }

        if (args.Length == 0)
        {
            SendUsage(caller);
            return;
        }

        string subCommand = args[0].ToLowerInvariant();

        if (subCommand == "list")
        {
            ListInteractions(caller);
            return;
        }

        bool autoComplete = args.Length >= 2 && args[1].Equals("auto", StringComparison.OrdinalIgnoreCase);

        // Treat the first arg as an interaction tag.
        await PerformInteraction(caller, args[0], autoComplete);
    }

    private void ListInteractions(NwPlayer caller)
    {
        IReadOnlyCollection<string> types = _interactions.GetRegisteredInteractionTypes();

        if (types.Count == 0)
        {
            caller.SendServerMessage("No interaction types are currently registered.", ColorConstants.Orange);
            return;
        }

        StringBuilder sb = new();
        sb.AppendLine($"Registered interaction types ({types.Count}):");
        foreach (string tag in types)
        {
            sb.AppendLine($"  • {tag}");
        }

        caller.SendServerMessage(sb.ToString(), ColorConstants.Lime);
    }

    private async Task PerformInteraction(NwPlayer caller, string tag, bool autoComplete)
    {
        NwCreature? creature = caller.LoginCreature;
        if (creature is null)
        {
            return;
        }

        if (!_characters.TryGetPlayerKey(caller, out Guid playerKey))
        {
            caller.SendServerMessage("Could not resolve your character ID.", ColorConstants.Red);
            return;
        }

        CharacterId characterId = CharacterId.From(playerKey);
        Guid targetId = creature.UUID;
        string? areaResRef = creature.Area?.ResRef;

        Trace(caller, $"═══ Interaction Trace: '{tag}' ═══");
        Trace(caller, $"Character: {creature.Name} ({characterId.Value})");
        Trace(caller, $"Target: {targetId}");
        Trace(caller, $"Area: {areaResRef ?? "(none)"}");

        InteractionInfo? preSession = _interactions.GetActiveInteraction(characterId);
        if (preSession != null)
        {
            Trace(caller, $"Active session: {preSession.InteractionTag} — " +
                          $"progress {preSession.Progress}/{preSession.RequiredRounds}");
        }
        else
        {
            Trace(caller, "No active session — a new session will be created.");
        }

        if (autoComplete)
        {
            await RunToCompletion(caller, characterId, tag, targetId, areaResRef);
        }
        else
        {
            await RunSingleTick(caller, characterId, tag, targetId, areaResRef);
        }
    }

    private async Task RunSingleTick(NwPlayer caller, CharacterId characterId, string tag,
        Guid targetId, string? areaResRef)
    {
        InteractionInfo? pre = _interactions.GetActiveInteraction(characterId);
        Trace(caller, pre == null ? "─── Attempt ───" : "─── Tick ───");

        CommandResult result = await _interactions.PerformInteractionAsync(
            characterId, tag, targetId, areaResRef);

        TraceStageReports(caller, characterId);
        TraceResult(caller, tag, characterId, result);
    }

    private async Task RunToCompletion(NwPlayer caller, CharacterId characterId, string tag,
        Guid targetId, string? areaResRef)
    {
        Trace(caller, "Mode: auto (running all rounds to completion)");

        int step = 0;
        const int maxTicks = 100; // safety limit

        while (step < maxTicks)
        {
            step++;
            InteractionInfo? pre = _interactions.GetActiveInteraction(characterId);
            Trace(caller, pre == null ? "─── Attempt ───" : $"─── Tick {step} ───");

            CommandResult result = await _interactions.PerformInteractionAsync(
                characterId, tag, targetId, areaResRef);

            TraceStageReports(caller, characterId);
            TraceResult(caller, tag, characterId, result);

            if (!result.Success)
            {
                Trace(caller, $"Interaction stopped (failure on step {step}).");
                break;
            }

            string status = result.Data?.TryGetValue("status", out object? s) == true
                ? s?.ToString() ?? ""
                : "";

            if (status is "Completed" or "Failed")
            {
                Trace(caller, $"Interaction finished on step {step}.");
                break;
            }
        }

        if (step >= maxTicks)
        {
            Trace(caller, "Safety limit reached — aborting auto-run.", ColorConstants.Orange);
        }

        Trace(caller, "═══ Trace complete ═══");
    }

    private void TraceResult(NwPlayer caller, string tag, CharacterId characterId, CommandResult result)
    {
        // Show result
        if (result.Success)
        {
            string status = result.Data?.TryGetValue("status", out object? s) == true
                ? s?.ToString() ?? "OK"
                : "OK";
            Trace(caller, $"Result: SUCCESS — status={status}", ColorConstants.Lime);
        }
        else
        {
            Trace(caller, $"Result: FAILED — {result.ErrorMessage ?? "Unknown error"}", ColorConstants.Red);
        }

        // Show returned data
        if (result.Data is { Count: > 0 })
        {
            foreach (KeyValuePair<string, object> kvp in result.Data)
            {
                Trace(caller, $"  data[{kvp.Key}] = {kvp.Value}");
            }
        }

        // Show post-tick session state
        InteractionInfo? postSession = _interactions.GetActiveInteraction(characterId);
        if (postSession != null)
        {
            Trace(caller, $"Session: {postSession.InteractionTag} — " +
                          $"progress {postSession.Progress}/{postSession.RequiredRounds}");
        }
        else
        {
            Trace(caller, "Session: ended (no active session).");
        }
    }

    /// <summary>
    /// Fetches any accumulated Glyph stage trace reports for the character and dumps them to chat.
    /// Called after each <see cref="IInteractionSubsystem.PerformInteractionAsync"/> call
    /// so the developer can see exactly what each pipeline stage did.
    /// </summary>
    private void TraceStageReports(NwPlayer caller, CharacterId characterId)
    {
        if (_glyphHook == null) return;

        List<StageTraceReport> reports = _glyphHook.GetAndClearStageTraces(characterId.Value.ToString());
        if (reports.Count == 0) return;

        foreach (StageTraceReport report in reports)
        {
            Trace(caller, $"┌─ Glyph Stage: {report.StageName} ─ Graph: {report.GraphName}",
                new Color(100, 200, 255)); // light blue header
            Trace(caller, $"│  Steps executed: {report.StepsExecuted}");

            // Trace log entries (per-node execution details)
            if (report.TraceEntries.Count > 0)
            {
                Trace(caller, $"│  Trace log ({report.TraceEntries.Count} entries):");
                foreach (string entry in report.TraceEntries)
                {
                    Trace(caller, $"│    {entry}");
                }
            }
            else
            {
                Trace(caller, "│  Trace log: (empty)");
            }

            // Context snapshot: flags, interaction metadata, variables
            if (report.ContextSnapshot.Count > 0)
            {
                Trace(caller, "│  Context:");
                foreach (KeyValuePair<string, string> kvp in report.ContextSnapshot)
                {
                    Color entryColor = kvp.Key is "blocked" or "cancelled"
                        ? ColorConstants.Orange
                        : ColorConstants.Silver;
                    Trace(caller, $"│    {kvp.Key} = {kvp.Value}", entryColor);
                }
            }

            Trace(caller, "└──────────────────────────", new Color(100, 200, 255));
        }
    }

    private static void Trace(NwPlayer caller, string message, Color? color = null)
    {
        caller.SendServerMessage($"[TRACE] {message}", color ?? ColorConstants.Silver);
    }

    private static void SendUsage(NwPlayer caller)
    {
        caller.SendServerMessage("Usage: ./interaction list | ./interaction <tag> [auto]", ColorConstants.White);
    }
}

using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Conditions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;

/// <summary>
/// Mutable runtime state for an active dialogue conversation.
/// Tracks the current position in the dialogue tree and handles navigation.
/// </summary>
public sealed class DialogueSession
{
    private const int CharsPerPage = 400;

    /// <summary>
    /// The dialogue tree being played.
    /// </summary>
    public DialogueTree Tree { get; }

    /// <summary>
    /// The player in the conversation.
    /// </summary>
    public NwPlayer Player { get; }

    /// <summary>
    /// The player's character ID for condition evaluation.
    /// </summary>
    public Guid CharacterId { get; }

    /// <summary>
    /// The NPC creature the player is talking to.
    /// </summary>
    public NwCreature Npc { get; }

    /// <summary>
    /// The current node in the dialogue tree.
    /// </summary>
    public DialogueNodeId CurrentNodeId { get; private set; }

    /// <summary>
    /// Current text page index (0-based) for paginated NPC text.
    /// </summary>
    public int TextPage { get; set; }

    /// <summary>
    /// Whether this session has ended.
    /// </summary>
    public bool IsEnded { get; private set; }

    public DialogueSession(DialogueTree tree, NwPlayer player, Guid characterId, NwCreature npc)
    {
        Tree = tree;
        Player = player;
        CharacterId = characterId;
        Npc = npc;
        CurrentNodeId = tree.RootNodeId;
    }

    /// <summary>
    /// Gets the current dialogue node.
    /// </summary>
    public DialogueNode? GetCurrentNode() => Tree.FindNode(CurrentNodeId);

    /// <summary>
    /// Gets the speaker tag for the current node (node override or tree default).
    /// </summary>
    public string GetCurrentSpeakerTag()
    {
        DialogueNode? node = GetCurrentNode();
        return node?.SpeakerTag ?? Tree.SpeakerTag ?? Npc.Tag;
    }

    /// <summary>
    /// Gets the NPC portrait ResRef from the speaking creature.
    /// </summary>
    public string GetPortraitResRef() => Npc.PortraitResRef ?? string.Empty;

    /// <summary>
    /// Gets the NPC display name.
    /// </summary>
    public string GetNpcName() => Npc.Name;

    // ──────────────────── Text Pagination ────────────────────

    /// <summary>
    /// Gets the current page of NPC text.
    /// </summary>
    public string GetCurrentTextPage()
    {
        DialogueNode? node = GetCurrentNode();
        if (node == null) return string.Empty;

        string[] pages = PaginateText(node.Text);
        if (TextPage < 0 || TextPage >= pages.Length) return string.Empty;
        return pages[TextPage];
    }

    /// <summary>
    /// Gets the total number of text pages for the current node.
    /// </summary>
    public int GetTotalTextPages()
    {
        DialogueNode? node = GetCurrentNode();
        if (node == null) return 0;
        return PaginateText(node.Text).Length;
    }

    /// <summary>
    /// Whether there's a next text page.
    /// </summary>
    public bool HasNextTextPage() => TextPage < GetTotalTextPages() - 1;

    /// <summary>
    /// Whether there's a previous text page.
    /// </summary>
    public bool HasPreviousTextPage() => TextPage > 0;

    // ──────────────────── Choice Evaluation ────────────────────

    /// <summary>
    /// Gets all choices from the current node whose conditions are satisfied.
    /// </summary>
    public async Task<List<DialogueChoice>> GetVisibleChoicesAsync(DialogueConditionRegistry conditionRegistry)
    {
        DialogueNode? node = GetCurrentNode();
        if (node == null) return [];

        List<DialogueChoice> visible = [];

        foreach (DialogueChoice choice in node.Choices.OrderBy(c => c.SortOrder))
        {
            bool conditionsMet = await conditionRegistry.EvaluateAllAsync(
                choice.Conditions, Player, CharacterId);

            if (conditionsMet)
            {
                visible.Add(choice);
            }
        }

        return visible;
    }

    // ──────────────────── Navigation ────────────────────

    /// <summary>
    /// Advances the conversation to the target node of the selected choice.
    /// Returns the new node, or null if the choice was invalid.
    /// </summary>
    public DialogueNode? SelectChoice(DialogueChoice choice)
    {
        DialogueNode? targetNode = Tree.FindNode(choice.TargetNodeId);
        if (targetNode == null) return null;

        CurrentNodeId = choice.TargetNodeId;
        TextPage = 0;

        if (targetNode.Type == DialogueNodeType.End)
        {
            IsEnded = true;
        }

        return targetNode;
    }

    /// <summary>
    /// Marks the session as ended.
    /// </summary>
    public void End()
    {
        IsEnded = true;
    }

    // ──────────────────── Helpers ────────────────────

    private static string[] PaginateText(string text)
    {
        if (string.IsNullOrEmpty(text)) return [string.Empty];
        if (text.Length <= CharsPerPage) return [text];

        List<string> pages = [];
        int start = 0;

        while (start < text.Length)
        {
            int length = Math.Min(CharsPerPage, text.Length - start);

            // Try to break at a word boundary
            if (start + length < text.Length)
            {
                int lastSpace = text.LastIndexOf(' ', start + length, Math.Min(length, 80));
                if (lastSpace > start)
                {
                    length = lastSpace - start;
                }
            }

            pages.Add(text.Substring(start, length).Trim());
            start += length;
        }

        return pages.ToArray();
    }
}

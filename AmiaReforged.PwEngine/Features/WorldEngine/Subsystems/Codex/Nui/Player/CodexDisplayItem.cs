using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;

/// <summary>
/// Unified display interface for all codex entry types.
/// Prevents the presenter from switching on entity type everywhere.
/// </summary>
public interface ICodexDisplayItem
{
    string DisplayName { get; }
    string DetailTitle { get; }
    string DetailBody { get; }
    string Subtitle { get; }
}

public sealed class LoreDisplayItem : ICodexDisplayItem
{
    private readonly CodexLoreEntry _entry;

    public LoreDisplayItem(CodexLoreEntry entry) => _entry = entry;

    public string DisplayName => _entry.Title;
    public string DetailTitle => _entry.Title;

    public string DetailBody
    {
        get
        {
            string body = _entry.Content;
            body = $"Category: {_entry.Category.DisplayName()}\n\n{body}";
            if (!string.IsNullOrEmpty(_entry.DiscoverySource))
                body += $"\n\nSource: {_entry.DiscoverySource}";
            if (!string.IsNullOrEmpty(_entry.DiscoveryLocation))
                body += $"\nLocation: {_entry.DiscoveryLocation}";
            return body;
        }
    }

    public string Subtitle => _entry.Tier.ToString();
}

public sealed class QuestDisplayItem : ICodexDisplayItem
{
    private readonly CodexQuestEntry _entry;

    public QuestDisplayItem(CodexQuestEntry entry) => _entry = entry;

    public string DisplayName => _entry.Title;
    public string DetailTitle => _entry.Title;

    public string DetailBody
    {
        get
        {
            string body = _entry.Description;
            if (!string.IsNullOrEmpty(_entry.QuestGiver))
                body = $"Quest Giver: {_entry.QuestGiver}\n\n{body}";
            if (!string.IsNullOrEmpty(_entry.Location))
                body += $"\n\nLocation: {_entry.Location}";
            if (_entry.CurrentStageId > 0)
                body += $"\n\nCurrent Stage: {_entry.CurrentStageId}";
            if (_entry.DateCompleted.HasValue)
                body += $"\n\nCompleted: {_entry.DateCompleted.Value:yyyy-MM-dd}";
            return body;
        }
    }

    public string Subtitle => _entry.State.ToString();
}

public sealed class NoteDisplayItem : ICodexDisplayItem
{
    private readonly CodexNoteEntry _entry;

    public NoteDisplayItem(CodexNoteEntry entry) => _entry = entry;

    public string DisplayName => _entry.Title ?? "Untitled Note";
    public string DetailTitle => _entry.Title ?? "Untitled Note";

    public string DetailBody
    {
        get
        {
            string body = _entry.Content;
            body += $"\n\nCategory: {_entry.Category}";
            body += $"\nCreated: {_entry.DateCreated:yyyy-MM-dd}";
            if (_entry.LastModified != _entry.DateCreated)
                body += $"\nModified: {_entry.LastModified:yyyy-MM-dd}";
            if (_entry.IsDmNote)
                body += "\n[DM Note]";
            return body;
        }
    }

    public string Subtitle => _entry.Category.ToString();
}

public sealed class ReputationDisplayItem : ICodexDisplayItem
{
    private readonly FactionReputation _entry;

    public ReputationDisplayItem(FactionReputation entry) => _entry = entry;

    public string DisplayName => _entry.FactionName;
    public string DetailTitle => _entry.FactionName;

    public string DetailBody
    {
        get
        {
            string body = $"Standing: {_entry.GetStanding()}";
            body += $"\nScore: {_entry.CurrentScore.Value}";
            if (!string.IsNullOrEmpty(_entry.Description))
                body += $"\n\n{_entry.Description}";
            body += $"\n\nEstablished: {_entry.DateEstablished:yyyy-MM-dd}";
            body += $"\nLast Changed: {_entry.LastChanged:yyyy-MM-dd}";

            if (_entry.History.Count > 0)
            {
                body += "\n\nRecent History:";
                foreach (ReputationChange change in _entry.History.TakeLast(5))
                {
                    string sign = change.Delta > 0 ? "+" : "";
                    body += $"\n  {sign}{change.Delta}: {change.Reason}";
                }
            }

            return body;
        }
    }

    public string Subtitle => $"{_entry.CurrentScore.Value} ({_entry.GetStanding()})";
}

public sealed class TraitDisplayItem : ICodexDisplayItem
{
    private readonly CodexTraitEntry _entry;

    public TraitDisplayItem(CodexTraitEntry entry) => _entry = entry;

    public string DisplayName => _entry.Name;
    public string DetailTitle => _entry.Name;

    public string DetailBody
    {
        get
        {
            string body = $"Category: {_entry.Category.DisplayName()}";
            body += $"\nAcquired: {_entry.DateAcquired:yyyy-MM-dd}";
            body += $"\nMethod: {_entry.AcquisitionMethod}";
            if (!string.IsNullOrEmpty(_entry.Description))
                body += $"\n\n{_entry.Description}";
            return body;
        }
    }

    public string Subtitle => _entry.Category.DisplayName();
}

public sealed class KnowledgeDisplayItem : ICodexDisplayItem
{
    private readonly CharacterKnowledge _ck;
    private Knowledge Def => _ck.Definition;

    public KnowledgeDisplayItem(CharacterKnowledge ck) => _ck = ck;

    public string DisplayName => Def.Name;
    public string DetailTitle => Def.Name;

    public string DetailBody
    {
        get
        {
            string body = Def.Description;
            body += $"\n\nRequired Tier: {Def.Level}";
            body += $"\nPoint Cost: {Def.PointCost}";

            if (!string.IsNullOrEmpty(Def.Branch))
                body += $"\nBranch: {Def.Branch}";

            if (Def.Prerequisites.Count > 0)
            {
                body += "\n\nPrerequisites:";
                foreach (string prereq in Def.Prerequisites)
                    body += $"\n  - {prereq}";
            }

            if (Def.CraftingModifiers.Count > 0)
            {
                body += "\n\nCrafting Modifiers:";
                foreach (CraftingModifier mod in Def.CraftingModifiers)
                    body += $"\n  - {mod.StepModified} ({mod.Scope}): {mod.Operation} {mod.Value:+0.##;-0.##}";
            }

            if (Def.HarvestEffects.Count > 0)
            {
                body += "\n\nHarvest Effects:";
                foreach (KnowledgeHarvestEffect effect in Def.HarvestEffects)
                    body += $"\n  - {effect.NodeTag}: {effect.StepModified} {effect.Operation} {effect.Value:+0.##;-0.##}";
            }

            return body;
        }
    }

    public string Subtitle => Def.Level.ToString();
}

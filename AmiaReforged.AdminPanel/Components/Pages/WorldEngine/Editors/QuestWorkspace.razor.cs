using System.Text.Json;
using System.Text.RegularExpressions;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.Shared.Quests;
using Microsoft.AspNetCore.Components;

namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;

public partial class QuestWorkspace
{
    [Parameter, EditorRequired] public QuestDefinitionDto Model { get; set; } = new();
    [Parameter] public bool IsCreating { get; set; }
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public IReadOnlyList<IndustryDefinitionDto> Industries { get; set; } = [];
    [Parameter] public RenderFragment? Browser { get; set; }
    [Parameter] public EventCallback Changed { get; set; }
    [Parameter] public EventCallback<(int StageId, int GroupIndex, int ObjectiveIndex, string Mode)> GraphEditorRequested { get; set; }

    private QuestDefinitionDto? _loadedModel;
    private QuestStageDto? _selectedStage;
    private QuestStageDto? _removingStage;
    private bool _idEdited;
    private bool _showAllErrors;
    private readonly HashSet<string> _touched = [];
    private List<QuestValidationError> _errors = [];
    private readonly Dictionary<ObjectiveDefinitionDto, string> _configDrafts = [];
    private readonly Dictionary<ObjectiveDefinitionDto, string> _configErrors = [];
    private readonly Dictionary<QuestStageDto, string> _stageIdErrors = [];
    private readonly Dictionary<QuestStageDto, string> _stageIdDrafts = [];

    public bool HasInvalidInput => _configErrors.Count > 0 || _stageIdErrors.Count > 0;

    public void RefreshConfiguration(ObjectiveDefinitionDto objective)
    {
        _configDrafts.Remove(objective);
        _configErrors.Remove(objective);
    }

    protected override void OnParametersSet()
    {
        if (_loadedModel != Model)
        {
            _loadedModel = Model;
            _selectedStage = Model.Stages.OrderBy(s => s.StageId).FirstOrDefault();
            _idEdited = !string.IsNullOrEmpty(Model.QuestId);
            _showAllErrors = false;
            _touched.Clear();
            _configDrafts.Clear();
            _configErrors.Clear();
            _stageIdErrors.Clear();
            _stageIdDrafts.Clear();
        }
        _errors = QuestDefinitionValidator.Validate(Model);
    }

    public void ShowErrors()
    {
        _showAllErrors = true;
        if (_errors.FirstOrDefault() is { } first) SelectError(first);
        StateHasChanged();
    }

    private void SelectError(QuestValidationError error) =>
        _selectedStage = error.StageId.HasValue ? Model.Stages.FirstOrDefault(s => s.StageId == error.StageId) : null;

    private void SelectStage(QuestStageDto? stage) => _selectedStage = stage;
    private void SelectObjectiveError(ObjectiveDefinitionDto objective) => _selectedStage =
        Model.Stages.FirstOrDefault(s => s.ObjectiveGroups.Any(g => g.Objectives.Contains(objective)));
    private string StageField(QuestStageDto stage) => $"Stages[{Model.Stages.IndexOf(stage)}]";
    private static string Text(ChangeEventArgs e) => e.Value?.ToString() ?? "";
    private static bool Checked(ChangeEventArgs e) => e.Value is true;
    private static int Number(ChangeEventArgs e) => int.TryParse(Text(e), out int number) ? number : 0;
    private static int? OptionalNumber(ChangeEventArgs e) => int.TryParse(Text(e), out int number) ? number : null;

    private async Task Change(string field, Action edit)
    {
        if (ReadOnly) return;
        edit();
        _touched.Add(field);
        var objectives = Model.Stages.SelectMany(s => s.ObjectiveGroups).SelectMany(g => g.Objectives).ToHashSet();
        foreach (var removed in _configErrors.Keys.Where(o => !objectives.Contains(o)).ToList()) _configErrors.Remove(removed);
        foreach (var removed in _configDrafts.Keys.Where(o => !objectives.Contains(o)).ToList()) _configDrafts.Remove(removed);
        foreach (var removed in _stageIdErrors.Keys.Where(s => !Model.Stages.Contains(s)).ToList()) _stageIdErrors.Remove(removed);
        foreach (var removed in _stageIdDrafts.Keys.Where(s => !Model.Stages.Contains(s)).ToList()) _stageIdDrafts.Remove(removed);
        _errors = QuestDefinitionValidator.Validate(Model);
        await Changed.InvokeAsync();
    }

    private Task SetTitle(ChangeEventArgs e) => Change("Title", () =>
    {
        Model.Title = Text(e);
        if (IsCreating && !_idEdited)
        {
            string slug = Regex.Replace(Model.Title.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');
            Model.QuestId = slug.Length == 0 ? "" : "quest_" + slug[..Math.Min(slug.Length, 94)];
        }
    });

    private Task SetQuestId(ChangeEventArgs e) => Change("QuestId", () => { _idEdited = true; Model.QuestId = Text(e); });

    private async Task AddStage()
    {
        QuestStageDto stage = new() { StageId = Model.Stages.Count == 0 ? 10 : Model.Stages.Max(s => s.StageId) + 10 };
        await Change("Stages", () => { Model.Stages.Add(stage); _selectedStage = stage; });
    }

    private async Task RemoveStage()
    {
        if (_removingStage is not { } stage) return;
        await Change("Stages", () =>
        {
            Model.Stages.Remove(stage);
            _selectedStage = Model.Stages.OrderBy(s => s.StageId).FirstOrDefault();
            _removingStage = null;
            _showAllErrors = true;
        });
    }

    private Task SetStageState(QuestStageDto stage, string value) => Change(StageField(stage) + ".QuestState", () =>
    {
        stage.QuestState = string.IsNullOrEmpty(value) ? null : value;
        stage.IsCompletionStage = false;
        if (QuestDefinitionValidator.IsTerminal(stage))
        {
            stage.NextStageId = null;
            foreach (ObjectiveGroupDto group in stage.ObjectiveGroups) group.CompletionStageId = null;
        }
    });

    private async Task SetStageId(QuestStageDto stage, ChangeEventArgs e)
    {
        if (ReadOnly) return;
        int id = Number(e);
        _stageIdDrafts[stage] = Text(e);
        if (id < 1 || Model.Stages.Any(s => s != stage && s.StageId == id))
        {
            _stageIdErrors[stage] = "Choose a positive, unused stage ID.";
            await Changed.InvokeAsync();
            return;
        }
        await Change(StageField(stage) + ".StageId", () =>
        {
            int previous = stage.StageId;
            stage.StageId = id;
            _stageIdErrors.Remove(stage);
            _stageIdDrafts.Remove(stage);
            foreach (QuestStageDto other in Model.Stages)
            {
                if (other.NextStageId == previous) other.NextStageId = id;
                foreach (ObjectiveGroupDto group in other.ObjectiveGroups)
                    if (group.CompletionStageId == previous) group.CompletionStageId = id;
            }
        });
    }

    private Task AddObjective(QuestStageDto stage, ObjectiveGroupDto? group = null) => Change(StageField(stage) + ".ObjectiveGroups", () =>
    {
        group ??= stage.ObjectiveGroups.FirstOrDefault();
        if (group == null) { group = new() { DisplayName = "Objectives" }; stage.ObjectiveGroups.Add(group); }
        group.Objectives.Add(new() { ObjectiveId = Guid.NewGuid().ToString("N")[..12], TypeTag = "collect", RequiredCount = 1 });
    });

    private Task AddGroup(QuestStageDto stage) => Change(StageField(stage) + ".ObjectiveGroups", () =>
    {
        ObjectiveGroupDto group = new()
        {
            DisplayName = $"Objectives {stage.ObjectiveGroups.Count + 1}",
            Objectives = [new() { ObjectiveId = Guid.NewGuid().ToString("N")[..12], TypeTag = "collect", RequiredCount = 1 }]
        };
        stage.ObjectiveGroups.Add(group);
    });

    private Task RemoveGroup(QuestStageDto stage, ObjectiveGroupDto group) => Change(StageField(stage) + ".ObjectiveGroups", () => stage.ObjectiveGroups.Remove(group));
    private Task ChangeObjectiveType(ObjectiveDefinitionDto objective, string field, string type) => Change(field + ".TypeTag", () => objective.TypeTag = type);

    private string ConfigText(ObjectiveDefinitionDto objective) => _configDrafts.GetValueOrDefault(objective)
        ?? JsonSerializer.Serialize(objective.Config, new JsonSerializerOptions { WriteIndented = true });

    private Task SetConfigValue(ObjectiveDefinitionDto objective, string field, string key, string value) => Change(field + ".Config", () =>
    {
        CodexEditor.SetConfig(objective, key, value);
        RefreshConfiguration(objective);
    });

    private static bool ConfigFlag(ObjectiveDefinitionDto objective, string key, bool fallback) =>
        bool.TryParse(CodexEditor.GetConfigString(objective, key, fallback.ToString()), out bool value) ? value : fallback;

    private bool HasFieldErrors(string prefix) => _errors.Any(e => e.Field.StartsWith(prefix, StringComparison.Ordinal)
        && (_showAllErrors || _touched.Contains(e.Field)));

    private async Task SetConfigJson(ObjectiveDefinitionDto objective, string field, string json)
    {
        _configDrafts[objective] = json;
        try
        {
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (config == null) throw new JsonException();
            _configErrors.Remove(objective);
            await Change(field + ".Config", () => objective.Config = config);
        }
        catch (JsonException)
        {
            _configErrors[objective] = "Configuration must be a valid JSON object.";
            await Changed.InvokeAsync();
        }
    }

    private static string StageLabel(QuestStageDto stage) =>
        !string.IsNullOrWhiteSpace(stage.Name) ? stage.Name
        : QuestDefinitionValidator.IsTerminal(stage) ? QuestDefinitionValidator.EffectiveState(stage)!
        : stage.ObjectiveGroups.SelectMany(g => g.Objectives).FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.DisplayText))?.DisplayText
          ?? (string.IsNullOrWhiteSpace(stage.JournalText) ? "New stage" : stage.JournalText);

    private QuestStageDto? NextStage(QuestStageDto stage) => stage.NextStageId is { } next
        ? Model.Stages.FirstOrDefault(s => s.StageId == next)
        : Model.Stages.Where(s => s.StageId > stage.StageId).OrderBy(s => s.StageId).FirstOrDefault();

    private string FallbackLabel(QuestStageDto stage)
    {
        QuestStageDto? next = Model.Stages.Where(s => s.StageId > stage.StageId).OrderBy(s => s.StageId).FirstOrDefault();
        return next == null ? "No next stage — use a dialogue action or script" : $"Next stage automatically: {next.StageId} · {StageLabel(next)}";
    }

    private string TransitionLabel(QuestStageDto stage) => QuestDefinitionValidator.IsTerminal(stage)
        ? $"Stage {stage.StageId} · Quest ends as {QuestDefinitionValidator.EffectiveState(stage)} on entry"
        : NextStage(stage) is { } next ? $"Stage {stage.StageId} → Stage {next.StageId} · {StageLabel(next)}"
        : stage.NextStageId.HasValue ? $"Linked stage {stage.NextStageId} is missing"
        : "No next stage. Completing these objectives alone will not close the quest.";

    private static string ObjectiveTypeLabel(string type) => type switch
    {
        "collect" => "Collect items", "dialog_choice" => "Speak to someone", "kill" => "Defeat creatures",
        "reach_location" => "Reach a location", "investigate" => "Investigate", "escort" => "Escort someone",
        "composite" => "Combined objectives", _ => type
    };
    private static string TargetLabel(string type) => type switch
    {
        "collect" => "Item tag", "dialog_choice" => "Dialogue node", "reach_location" => "Area tag",
        "escort" => "NPC tag", _ => "Creature tag"
    };
    private static string TargetPlaceholder(string type) => type switch { "collect" => "rat_tail", "dialog_choice" => "7ac30d12", "reach_location" => "area_tag", _ => "creature_tag" };
    private static string GroupModeLabel(string mode) => mode.ToLowerInvariant() switch { "any" => "Any one required", "sequence" => "Complete in order", _ => "All required" };
    private static string RewardSummary(RewardMixDto reward) => string.Join(", ",
        new[] { reward.Xp != 0 ? $"{reward.Xp} XP" : null, reward.Gold != 0 ? $"{reward.Gold} gold" : null,
            reward.KnowledgePoints != 0 ? $"{reward.KnowledgePoints} knowledge points" : null }
        .Where(s => s != null).Concat(reward.Proficiencies.Select(p => $"{p.ProficiencyXp} {p.IndustryTag} XP")));
}

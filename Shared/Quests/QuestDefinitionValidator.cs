namespace AmiaReforged.Shared.Quests;

public sealed record QuestValidationError(string Field, string Message, int? StageId = null);

/// <summary>Authoring and API writes share the quest definition contract.</summary>
public static class QuestDefinitionValidator
{
    public static readonly string[] QuestStates = ["Discovered", "InProgress", "Completed", "Failed", "Abandoned", "Expired"];
    public static readonly string[] ObjectiveTypes = ["collect", "dialog_choice", "kill", "reach_location", "investigate", "escort", "composite"];
    public static readonly string[] CompletionModes = ["All", "Any", "Sequence"];

    public static bool IsTerminal(QuestStageDto stage) =>
        EffectiveState(stage) is "Completed" or "Failed" or "Abandoned" or "Expired";

    public static string? EffectiveState(QuestStageDto stage) =>
        QuestStates.FirstOrDefault(s => s.Equals(stage.QuestState, StringComparison.OrdinalIgnoreCase))
        ?? (string.IsNullOrWhiteSpace(stage.QuestState) && stage.IsCompletionStage ? "Completed" : null);

    public static List<QuestValidationError> Validate(QuestDefinitionDto quest)
    {
        List<QuestValidationError> errors = [];
        Required(quest.QuestId, "QuestId", "Quest ID", 100);
        Required(quest.Title, "Title", "Title", 200);
        Required(quest.Description, "Description", "Description");
        if (quest.Keywords?.Length > 1000) Add("Keywords", "Keywords must not exceed 1000 characters.");

        var stageIds = quest.Stages.Where(s => s != null).Select(s => s.StageId).ToHashSet();
        if (quest.DefaultStageId is { } defaultStageId && (defaultStageId < 1 || !stageIds.Contains(defaultStageId)))
            Add("DefaultStageId", "Default stage must refer to an existing positive stage ID.");
        HashSet<int> seenStages = [];
        HashSet<string> objectiveIds = new(StringComparer.Ordinal);
        for (int si = 0; si < quest.Stages.Count; si++)
        {
            QuestStageDto? stage = quest.Stages[si];
            string field = $"Stages[{si}]";
            if (stage is null) { Add(field, "Remove the empty stage."); continue; }
            int sid = stage.StageId;
            if (sid < 1) Add(field + ".StageId", "Stage ID must be positive.", sid);
            if (!seenStages.Add(sid)) Add(field + ".StageId", $"Stage ID {sid} is used more than once.", sid);
            if (!string.IsNullOrWhiteSpace(stage.QuestState) && EffectiveState(stage) == null)
                Add(field + ".QuestState", $"Stage {sid}: unknown quest state '{stage.QuestState}'.", sid);
            if (stage.IsCompletionStage && !string.IsNullOrWhiteSpace(stage.QuestState) && EffectiveState(stage) != "Completed")
                Add(field + ".QuestState", $"Stage {sid}: the legacy completion flag conflicts with its quest state.", sid);
            Link(stage.NextStageId, field + ".NextStageId", sid);
            if (IsTerminal(stage) && stage.NextStageId.HasValue)
                Add(field + ".NextStageId", $"Stage {sid}: an ending cannot advance to another stage.", sid);

            for (int gi = 0; gi < stage.ObjectiveGroups.Count; gi++)
            {
                ObjectiveGroupDto? group = stage.ObjectiveGroups[gi];
                string groupField = field + $".ObjectiveGroups[{gi}]";
                if (group is null) { Add(groupField, $"Stage {sid}: remove the empty objective group.", sid); continue; }
                Required(group.DisplayName, groupField + ".DisplayName", $"Stage {sid}: objective group name", stageId: sid);
                if (!CompletionModes.Contains(group.CompletionMode, StringComparer.OrdinalIgnoreCase))
                    Add(groupField + ".CompletionMode", $"Stage {sid}: choose All, Any, or Sequence.", sid);
                Link(group.CompletionStageId, groupField + ".CompletionStageId", sid);
                if (IsTerminal(stage) && group.CompletionStageId.HasValue)
                    Add(groupField + ".CompletionStageId", $"Stage {sid}: an ending cannot branch to another stage.", sid);
                if (group.Objectives.Count == 0)
                    Add(groupField + ".Objectives", $"Stage {sid}: add an objective or remove this group.", sid);

                for (int oi = 0; oi < group.Objectives.Count; oi++)
                {
                    ObjectiveDefinitionDto? objective = group.Objectives[oi];
                    string objectiveField = groupField + $".Objectives[{oi}]";
                    if (objective is null) { Add(objectiveField, $"Stage {sid}: remove the empty objective.", sid); continue; }
                    Required(objective.ObjectiveId, objectiveField + ".ObjectiveId", $"Stage {sid}: objective ID", stageId: sid);
                    if (!string.IsNullOrWhiteSpace(objective.ObjectiveId) && !objectiveIds.Add(objective.ObjectiveId))
                        Add(objectiveField + ".ObjectiveId", $"Stage {sid}: objective ID '{objective.ObjectiveId}' is used more than once.", sid);
                    if (!ObjectiveTypes.Contains(objective.TypeTag))
                        Add(objectiveField + ".TypeTag", $"Stage {sid}: unknown objective type '{objective.TypeTag}'.", sid);
                    Required(objective.DisplayText, objectiveField + ".DisplayText", $"Stage {sid}: objective journal text", stageId: sid);
                    if (objective.RequiredCount < 1)
                        Add(objectiveField + ".RequiredCount", $"Stage {sid}: objective count must be at least 1.", sid);
                    if (objective.TypeTag is "collect" or "dialog_choice" or "kill" or "reach_location" or "escort")
                        Required(objective.TargetTag, objectiveField + ".TargetTag", $"Stage {sid}: objective target", stageId: sid);
                }
            }
            Reward(stage.Rewards, field + ".Rewards", $"Stage {sid}", sid);
        }
        Reward(quest.CompletionReward, "CompletionReward", "Completion reward");
        return errors;

        void Add(string field, string message, int? stageId = null) => errors.Add(new(field, message, stageId));
        void Required(string? value, string field, string label, int? maximum = null, int? stageId = null)
        {
            if (string.IsNullOrWhiteSpace(value)) Add(field, label + " is required.", stageId);
            else if (maximum.HasValue && value.Length > maximum) Add(field, $"{label} must not exceed {maximum} characters.", stageId);
        }
        void Link(int? target, string field, int sid)
        {
            if (!target.HasValue) return;
            if (!stageIds.Contains(target.Value)) Add(field, $"Stage {sid}: linked stage {target} does not exist. Choose another stage.", sid);
            else if (target.Value <= sid) Add(field, $"Stage {sid}: the next stage must have a higher ID.", sid);
        }
        void Reward(RewardMixDto? reward, string field, string context, int? sid = null)
        {
            if (reward == null) return;
            if (reward.Xp < 0) Add(field + ".Xp", context + ": XP cannot be negative.", sid);
            if (reward.Gold < 0) Add(field + ".Gold", context + ": gold cannot be negative.", sid);
            if (reward.KnowledgePoints < 0) Add(field + ".KnowledgePoints", context + ": knowledge points cannot be negative.", sid);
            for (int pi = 0; pi < reward.Proficiencies.Count; pi++)
            {
                ProficiencyRewardDto? proficiency = reward.Proficiencies[pi];
                string profField = field + $".Proficiencies[{pi}]";
                if (proficiency == null) { Add(profField, context + ": remove the empty proficiency reward.", sid); continue; }
                Required(proficiency.IndustryTag, profField + ".IndustryTag", context + ": industry", stageId: sid);
                if (proficiency.ProficiencyXp < 0) Add(profField + ".ProficiencyXp", context + ": proficiency XP cannot be negative.", sid);
            }
        }
    }
}

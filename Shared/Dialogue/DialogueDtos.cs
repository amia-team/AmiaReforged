using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Shared.Dialogue;


public class DialogueTreeDto
{
    [Required]
    [StringLength(100)]
    public string DialogueTreeId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? RootNodeId { get; set; }
    public string? SpeakerTag { get; set; }
    public List<DialogueNodeDto> Nodes { get; set; } = [];
    public DateTime? CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
}

public class DialogueNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "NpcText";
    public string? SpeakerTag { get; set; }
    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? ParentNodeId { get; set; }
    public List<DialogueConditionDto> Conditions { get; set; } = [];
    public List<DialogueChoiceDto> Choices { get; set; } = [];
    public List<DialogueActionDto> Actions { get; set; } = [];
}

public class DialogueChoiceDto
{
    public string Id { get; set; } = string.Empty;
    public bool IsContinue { get; set; }
    public string TargetNodeId { get; set; } = string.Empty;
    public string ResponseText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<DialogueConditionDto> Conditions { get; set; } = [];
}

public class DialogueConditionDto
{
    public string Type { get; set; } = string.Empty;
    public bool Negate { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public class DialogueActionDto
{
    public string ActionType { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
    public int ExecutionOrder { get; set; }
}

public record DialogueRuntimeStatusDto
{
    public string State { get; init; } = "Pending";
    public DateTime? SavedRevisionUtc { get; init; }
    public DateTime? AppliedRevisionUtc { get; init; }
    public int MatchedNpcCount { get; init; }
    public string? Error { get; init; }
}

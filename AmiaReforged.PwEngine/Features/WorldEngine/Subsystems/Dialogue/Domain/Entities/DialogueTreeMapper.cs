using AmiaReforged.Shared.Dialogue;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;

public static class DialogueTreeMapper
{
    public static DialogueTree FromDto(DialogueTreeDto dto)
    {
        DialogueDefinitionValidator.Normalize(dto);
        List<string> errors = DialogueDefinitionValidator.Validate(dto);
        if (errors.Count > 0) throw new FormatException(string.Join("; ", errors));
        return new DialogueTree
        {
            Id = new DialogueTreeId(dto.DialogueTreeId), Title = dto.Title, Description = dto.Description,
            RootNodeId = new DialogueNodeId(Guid.Parse(dto.RootNodeId!)), SpeakerTag = dto.SpeakerTag,
            CreatedUtc = dto.CreatedUtc ?? DateTime.UtcNow, UpdatedUtc = dto.UpdatedUtc,
            Nodes = dto.Nodes.Select(n => new DialogueNode
            {
                Id = new DialogueNodeId(Guid.Parse(n.Id)), Name = n.Name, Type = Enum.Parse<DialogueNodeType>(n.Type),
                SpeakerTag = n.SpeakerTag, Text = n.Text ?? "", SortOrder = n.SortOrder,
                ParentNodeId = Guid.TryParse(n.ParentNodeId, out Guid parent) ? new DialogueNodeId(parent) : null,
                Conditions = n.Conditions.Select(FromDto).ToList(),
                Choices = n.Choices.Select(c => new DialogueChoice
                {
                    Id = Guid.Parse(c.Id), IsContinue = c.IsContinue,
                    TargetNodeId = new DialogueNodeId(Guid.Parse(c.TargetNodeId)), ResponseText = c.ResponseText,
                    SortOrder = c.SortOrder, Conditions = c.Conditions.Select(FromDto).ToList()
                }).ToList(),
                Actions = n.Actions.Select(a => new DialogueAction
                {
                    ActionType = Enum.Parse<DialogueActionType>(a.ActionType), Parameters = a.Parameters ?? [], ExecutionOrder = a.ExecutionOrder
                }).ToList()
            }).ToList()
        };
    }

    public static DialogueCondition FromDto(DialogueConditionDto condition) => new()
    {
        Type = Enum.Parse<DialogueConditionType>(condition.Type), Negate = condition.Negate, Parameters = condition.Parameters ?? []
    };

    public static DialogueTreeDto ToDto(DialogueTree tree) => new()
    {
        DialogueTreeId = tree.Id.Value, Title = tree.Title, Description = tree.Description,
        RootNodeId = tree.RootNodeId.Value.ToString(), SpeakerTag = tree.SpeakerTag,
        Nodes = tree.Nodes.Select(ToDto).ToList(), CreatedUtc = tree.CreatedUtc, UpdatedUtc = tree.UpdatedUtc
    };

    public static DialogueNodeDto ToDto(DialogueNode node) => new()
    {
        Id = node.Id.Value.ToString(), Name = node.Name, Type = node.Type.ToString(), Text = node.Text,
        SpeakerTag = node.SpeakerTag, SortOrder = node.SortOrder, ParentNodeId = node.ParentNodeId?.Value.ToString(),
        Conditions = node.Conditions.Select(ToDto).ToList(),
        Choices = node.Choices.Select(c => new DialogueChoiceDto
        {
            Id = c.Id.ToString(), IsContinue = c.IsContinue, TargetNodeId = c.TargetNodeId.Value.ToString(),
            ResponseText = c.ResponseText, SortOrder = c.SortOrder, Conditions = c.Conditions.Select(ToDto).ToList()
        }).ToList(),
        Actions = node.Actions.Select(a => new DialogueActionDto
        {
            ActionType = a.ActionType.ToString(), Parameters = a.Parameters, ExecutionOrder = a.ExecutionOrder
        }).ToList()
    };

    private static DialogueConditionDto ToDto(DialogueCondition condition) => new()
    {
        Type = condition.Type.ToString(), Negate = condition.Negate, Parameters = condition.Parameters
    };
}

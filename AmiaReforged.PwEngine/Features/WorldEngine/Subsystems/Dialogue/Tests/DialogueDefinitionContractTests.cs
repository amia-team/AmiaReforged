using System.Text.Json;
using AmiaReforged.Shared.Dialogue;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Tests;

[TestFixture]
public class DialogueDefinitionContractTests
{
    private static DialogueTreeDto Definition()
    {
        DialogueNodeDto root = new() { Id = Guid.NewGuid().ToString(), Type = "Root", Text = "Hello" };
        return new() { DialogueTreeId = "hello", Title = "Hello", RootNodeId = root.Id, Nodes = [root] };
    }

    [Test]
    public void SimpleGreetingWithoutRepliesIsPlayable()
    {
        DialogueTreeDto dto = Definition();
        Assert.That(DialogueDefinitionValidator.Validate(dto), Is.Empty);
        Assert.That(DialogueTreeMapper.FromDto(dto).Validate(), Is.Empty);
    }

    [Test]
    public void LegacyReplyGetsIdOnceWithoutChangingQuestNodeReferences()
    {
        DialogueTreeDto dto = Definition();
        DialogueNodeDto target = new() { Id = Guid.NewGuid().ToString(), Type = "NpcText", Text = "Farewell" };
        dto.Nodes.Add(target);
        dto.Nodes[0].Choices.Add(new() { TargetNodeId = target.Id, ResponseText = "Tell me more" });
        string rootId = dto.RootNodeId!;
        DialogueDefinitionValidator.Normalize(dto);
        string replyId = dto.Nodes[0].Choices[0].Id;
        DialogueDefinitionValidator.Normalize(dto);
        DialogueTreeDto roundTrip = DialogueTreeMapper.ToDto(DialogueTreeMapper.FromDto(dto));
        Assert.That(roundTrip.RootNodeId, Is.EqualTo(rootId));
        Assert.That(roundTrip.Nodes[1].Id, Is.EqualTo(target.Id));
        Assert.That(roundTrip.Nodes[0].Choices[0].Id, Is.EqualTo(replyId));
    }

    [Test]
    public void LegacyEmptyReplyIsMigratedToContinue()
    {
        DialogueTreeDto dto = Definition();
        dto.Nodes[0].Choices.Add(new() { TargetNodeId = dto.RootNodeId! });
        DialogueDefinitionValidator.Normalize(dto);
        Assert.That(dto.Nodes[0].Choices[0].IsContinue, Is.True);
        Assert.That(DialogueDefinitionValidator.Validate(dto), Is.Empty);
    }

    [TestCase("Unknown")]
    [TestCase("123")]
    public void UnknownNodeTypeIsRejectedBeforePersistence(string type)
    {
        DialogueTreeDto dto = Definition(); dto.Nodes[0].Type = type;
        Assert.That(DialogueDefinitionValidator.Validate(dto), Has.Some.Contains("unknown type"));
        Assert.Throws<FormatException>(() => DialogueTreeMapper.FromDto(dto));
    }

    [Test]
    public void AutomaticCyclesAreRejectedButPlayerReplyLoopsAreAllowed()
    {
        DialogueTreeDto dto = Definition();
        dto.Nodes[0].Choices.Add(new() { Id = Guid.NewGuid().ToString(), TargetNodeId = dto.RootNodeId!, ResponseText = "Repeat" });
        Assert.That(DialogueDefinitionValidator.Validate(dto), Is.Empty);
        DialogueNodeDto action = new() { Id = Guid.NewGuid().ToString(), Type = "Action" };
        action.Choices.Add(new() { Id = Guid.NewGuid().ToString(), TargetNodeId = action.Id, IsContinue = true });
        dto.Nodes.Add(action); dto.Nodes[0].Choices[0].TargetNodeId = action.Id;
        Assert.That(DialogueDefinitionValidator.Validate(dto), Has.Some.Contains("automatic action cycle"));
    }

    [Test]
    public void WriteValidationAndRuntimeRejectTheSameBrokenTarget()
    {
        DialogueTreeDto dto = Definition();
        dto.Nodes[0].Choices.Add(new() { Id = Guid.NewGuid().ToString(), TargetNodeId = Guid.NewGuid().ToString(), ResponseText = "Broken" });
        PersistedDialogueTree persisted = new() { DialogueTreeId = dto.DialogueTreeId, Title = dto.Title, RootNodeId = dto.RootNodeId, NodesJson = JsonSerializer.Serialize(dto.Nodes) };
        Assert.That(DialogueTreeWriteValidation.Validate(persisted), Does.Contain("non-existent node"));
        Assert.Throws<FormatException>(() => DialogueTreeMapper.FromDto(dto));
    }

    [Test]
    public void UnsupportedActionAndBadNumericParametersHaveLocatedErrors()
    {
        DialogueTreeDto dto = Definition();
        dto.Nodes[0].Actions.Add(new() { ActionType = "Custom" });
        dto.Nodes[0].Actions.Add(new() { ActionType = "GiveGold", Parameters = new() { ["amount"] = "-10" } });
        List<string> errors = DialogueDefinitionValidator.Validate(dto);
        Assert.That(errors, Has.Some.Contains("Custom is unsupported"));
        Assert.That(errors, Has.Some.Contains("positive integer"));
        Assert.That(errors.All(e => e.Contains(dto.RootNodeId!)), Is.True);
    }

    [Test]
    public void NullNodeIdAndReplyTargetAreValidationErrors()
    {
        DialogueTreeDto dto = Definition(); dto.Nodes[0].Id = null!;
        Assert.That(DialogueDefinitionValidator.Validate(dto), Has.Some.Contains("must have an ID"));
        dto = Definition(); dto.Nodes[0].Choices.Add(new() { Id = Guid.NewGuid().ToString(), TargetNodeId = null!, ResponseText = "Broken" });
        Assert.That(DialogueDefinitionValidator.Validate(dto), Has.Some.Contains("non-existent node"));
    }
}

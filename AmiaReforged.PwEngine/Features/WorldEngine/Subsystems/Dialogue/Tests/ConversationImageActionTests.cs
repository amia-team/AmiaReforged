using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Nui;
using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Tests;

[TestFixture]
public class ConversationImageActionTests
{
    [TestCase("conv_close")]
    [TestCase("btn_goodbye")]
    [TestCase("btn_prev_text")]
    [TestCase("btn_next_text")]
    [TestCase("btn_more")]
    [TestCase("btn_choice_0")]
    [TestCase("btn_choice_1")]
    [TestCase("btn_choice_2")]
    [TestCase("btn_choice_3")]
    [TestCase("btn_choice_4")]
    public void DialogueImagesActivateOnceDespiteBubbledAndDuplicateEvents(string id)
    {
        NuiImageInput input = new();
        bool enabled = ConversationPresenter.CanActivateImageAction(id, false, true, 0, 8, true, true);
        input.Handle(NuiEventType.MouseDown, id, 0, enabled);
        input.Handle(NuiEventType.MouseDown, "conv_shell", null, false, isActionElement: false);
        Assert.That(input.Handle(NuiEventType.Click, id, null, enabled), Is.False);
        Assert.That(input.Handle(NuiEventType.MouseUp, id, 0, enabled), Is.True);
        Assert.That(input.Handle(NuiEventType.MouseUp, "conv_shell", null, false, isActionElement: false), Is.False);
        Assert.That(input.Handle(NuiEventType.Click, id, null, enabled), Is.False);
        Assert.That(input.Handle(NuiEventType.MouseUp, id, 0, enabled), Is.False);
    }

    [TestCase("btn_choice_3", 1, 8)]
    [TestCase("btn_choice_4", 1, 8)]
    [TestCase("btn_choice_5", 0, 8)]
    [TestCase("btn_choice_-1", 1, 8)]
    [TestCase("btn_choice_bad", 0, 8)]
    [TestCase("btn_choice_0", 0, 0)]
    [TestCase("btn_choice_0", -1, 8)]
    [TestCase("conv_shell", 0, 8)]
    public void HiddenOrInvalidChoiceSlotsCannotActivate(string id, int page, int count)
    {
        Assert.That(ConversationPresenter.CanActivateImageAction(id, false, true, page, count, true, true), Is.False);
    }

    [TestCase("conv_close")]
    [TestCase("btn_goodbye")]
    [TestCase("btn_prev_text")]
    [TestCase("btn_next_text")]
    [TestCase("btn_more")]
    [TestCase("btn_choice_0")]
    public void AdvancementDisablesEveryActionAndRejectsItsRelease(string id)
    {
        NuiImageInput input = new();
        input.Handle(NuiEventType.MouseDown, id, 0, true);
        bool enabled = ConversationPresenter.CanActivateImageAction(id, true, true, 0, 8, true, true);
        Assert.That(enabled, Is.False);
        Assert.That(input.Handle(NuiEventType.MouseUp, id, 0, enabled), Is.False);
    }

    [TestCase("btn_choice_0")]
    [TestCase("btn_more")]
    public void RefreshOrStaleDisplayedNodeDisablesChoiceNavigation(string id)
    {
        Assert.That(ConversationPresenter.CanActivateImageAction(id, false, false, 0, 8, true, true), Is.False);
    }

    [Test]
    public void TextAndChoicePaginationHaveIndependentAvailability()
    {
        Assert.That(ConversationPresenter.CanActivateImageAction("btn_more", false, true, 1, 8, false, false), Is.True);
        Assert.That(ConversationPresenter.CanActivateImageAction("btn_more", false, true, 0, 5, true, true), Is.False);
        Assert.That(ConversationPresenter.CanActivateImageAction("btn_prev_text", false, true, 0, 8, false, true), Is.False);
        Assert.That(ConversationPresenter.CanActivateImageAction("btn_next_text", false, true, 0, 8, true, false), Is.False);
        Assert.That(ConversationPresenter.CanActivateImageAction("btn_prev_text", false, false, 0, 0, true, false), Is.True);
        Assert.That(ConversationPresenter.CanActivateImageAction("btn_next_text", false, false, 0, 0, false, true), Is.True);
        Assert.That(ConversationPresenter.CanActivateImageAction("conv_close", false, false, 0, 0, false, false), Is.True);
        Assert.That(ConversationPresenter.CanActivateImageAction("btn_goodbye", false, false, 0, 0, false, false), Is.True);
    }

    [TestCase("session")]
    [TestCase("node")]
    [TestCase("text")]
    [TestCase("choices")]
    [TestCase("refresh")]
    [TestCase("closed")]
    public void PresenterRejectsReleaseAfterItsDisplayedContextChanges(string change)
    {
        // The presenter constructor does not call native APIs; only the input path is exercised here.
        ConversationPresenter presenter = new(null!, null!, null!);
        DialogueNodeId node = DialogueNodeId.NewId();
        ConversationPresenter.ImageContext pressed = new(Session(node), node, 0, 0, 1);
        ConversationPresenter.ImageContext? released = change switch
        {
            "session" => pressed with { Session = Session(node) },
            "node" => pressed with { NodeId = DialogueNodeId.NewId() },
            "text" => pressed with { TextPage = 1 },
            "choices" => pressed with { ChoicePage = 1 },
            "refresh" => pressed with { RefreshVersion = 2 },
            _ => null
        };
        presenter.HandleImageEvent(NuiEventType.MouseDown, "btn_choice_0", 0, true, true, pressed);
        Assert.That(presenter.HandleImageEvent(NuiEventType.MouseUp, "btn_choice_0", 0, true, true, released), Is.False);
        Assert.That(presenter.HandleImageEvent(NuiEventType.MouseUp, "btn_choice_0", 0, true, true, pressed), Is.False,
            "A rejected release must discard the capture even if the old context returns.");
    }

    [Test]
    public void PresenterPreservesCaptureThroughDecorativeParentsAndIgnoresDuplicateClick()
    {
        ConversationPresenter presenter = new(null!, null!, null!);
        DialogueNodeId node = DialogueNodeId.NewId();
        ConversationPresenter.ImageContext context = new(Session(node), node, 0, 0, 1);
        presenter.HandleImageEvent(NuiEventType.MouseDown, "btn_choice_0", 0, true, true, context);
        presenter.HandleImageEvent(NuiEventType.MouseDown, "conv_shell", null, false, false, context);
        Assert.That(presenter.HandleImageEvent(NuiEventType.Click, "btn_choice_0", null, true, true, context), Is.False);
        Assert.That(presenter.HandleImageEvent(NuiEventType.MouseUp, "btn_choice_0", 0, true, true, context), Is.True);
        Assert.That(presenter.HandleImageEvent(NuiEventType.MouseUp, "conv_shell", null, false, false, context), Is.False);
        Assert.That(presenter.HandleImageEvent(NuiEventType.MouseUp, "btn_choice_0", 0, true, true, context), Is.False);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(null)]
    public void PresenterNeverCapturesNonLeftOrMissingButton(int? button)
    {
        ConversationPresenter presenter = new(null!, null!, null!);
        DialogueNodeId node = DialogueNodeId.NewId();
        ConversationPresenter.ImageContext context = new(Session(node), node, 0, 0, 1);
        presenter.HandleImageEvent(NuiEventType.MouseDown, "btn_goodbye", button, true, true, context);
        Assert.That(presenter.HandleImageEvent(NuiEventType.MouseUp, "btn_goodbye", 0, true, true, context), Is.False);
    }

    [TestCase("btn_choice_0", 0, 8, 0)]
    [TestCase("btn_choice_4", 0, 8, 4)]
    [TestCase("btn_choice_0", 1, 8, 5)]
    [TestCase("btn_choice_2", 1, 8, 7)]
    public void ChoiceSelectionUsesTheDisplayedPageIndex(string id, int page, int count, int expected)
    {
        Assert.That(ConversationPresenter.GetChoiceIndex(id, page, count), Is.EqualTo(expected));
    }

    private static DialogueSession Session(DialogueNodeId node) => new(new DialogueTree
    {
        Id = new DialogueTreeId("input-test"), RootNodeId = node,
        Nodes = [new DialogueNode { Id = node, Type = DialogueNodeType.Root, Text = "Greeting" }]
    }, null!, Guid.NewGuid(), null!);
}

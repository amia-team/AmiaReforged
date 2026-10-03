using System.Text.Json;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Prototype;
using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Nui;

[TestFixture]
public class CodexImageInputTests
{
    [Test]
    public void PressClickReleaseClick_ActivatesExactlyOnce()
    {
        CodexImageInput input = new();
        Assert.That(input.Handle(NuiEventType.MouseDown, "entry", 0, true), Is.False);
        Assert.That(input.Handle(NuiEventType.Click, "entry", null, true), Is.False);
        Assert.That(input.Handle(NuiEventType.MouseUp, "entry", 0, true), Is.True);
        Assert.That(input.Handle(NuiEventType.Click, "entry", null, true), Is.False);
        Assert.That(input.Handle(NuiEventType.MouseUp, "entry", 0, true), Is.False);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(null)]
    public void NonLeftOrMissingButton_DoesNotActivate(int? button)
    {
        CodexImageInput input = new();
        input.Handle(NuiEventType.MouseDown, "entry", button, true);
        Assert.That(input.Handle(NuiEventType.MouseUp, "entry", button, true), Is.False);
    }

    [Test]
    public void ReleaseWithoutPressOrOnAnotherControl_DoesNotActivate()
    {
        CodexImageInput input = new();
        Assert.That(input.Handle(NuiEventType.MouseUp, "close", 0, true), Is.False);
        input.Handle(NuiEventType.MouseDown, "entry", 0, true);
        Assert.That(input.Handle(NuiEventType.MouseUp, "close", 0, true), Is.False);
        Assert.That(input.Handle(NuiEventType.MouseUp, "entry", 0, true), Is.False);
    }

    [Test]
    public void DisabledAtPressOrRelease_DoesNotActivate()
    {
        CodexImageInput input = new();
        input.Handle(NuiEventType.MouseDown, "entry", 0, false);
        Assert.That(input.Handle(NuiEventType.MouseUp, "entry", 0, true), Is.False);
        input.Handle(NuiEventType.MouseDown, "entry", 0, true);
        Assert.That(input.Handle(NuiEventType.MouseUp, "entry", 0, false), Is.False);
        Assert.That(input.Handle(NuiEventType.MouseUp, "entry", 0, true), Is.False);
    }

    [Test]
    public void NonLeftRelease_ClearsPreviousLeftPress()
    {
        CodexImageInput input = new();
        input.Handle(NuiEventType.MouseDown, "entry", 0, true);
        Assert.That(input.Handle(NuiEventType.MouseUp, "entry", 2, true), Is.False);
        Assert.That(input.Handle(NuiEventType.MouseUp, "entry", 0, true), Is.False);
    }

    [Test]
    public void ReleaseOnNonActionElement_CancelsThePress()
    {
        CodexImageInput input = new();
        input.Handle(NuiEventType.MouseDown, "entry", 0, true);
        input.Handle(NuiEventType.MouseUp, "body", null, false);
        Assert.That(input.Handle(NuiEventType.MouseUp, "entry", 0, true), Is.False);
    }

    [Test]
    public void RepeatedValidPressRelease_EachActivatesOnce()
    {
        CodexImageInput input = new();
        for (int i = 0; i < 10; i++)
        {
            input.Handle(NuiEventType.MouseDown, "entry", 0, true);
            Assert.That(input.Handle(NuiEventType.MouseUp, "entry", 0, true), Is.True);
        }
    }

    [Test]
    public void Payload_UsesMouseButtonField_WithoutTreatingMissingAsLeft()
    {
        Assert.That(JsonSerializer.Deserialize<CodexMousePayload>("{\"mouse_btn\":2,\"mouse_pos\":{\"x\":12,\"y\":34}}")!.MouseButton,
            Is.EqualTo(2));
        Assert.That(JsonSerializer.Deserialize<CodexMousePayload>("{}")!.MouseButton, Is.Null);
    }
}

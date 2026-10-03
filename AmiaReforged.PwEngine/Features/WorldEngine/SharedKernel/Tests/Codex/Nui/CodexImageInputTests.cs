using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui;
using Anvil.API;
using Newtonsoft.Json;
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
    public void LayoutOrBusyStateChange_CancelsCapturedPress()
    {
        CodexImageInput input = new();
        input.Handle(NuiEventType.MouseDown, "btn_entry_0", 0, true);
        input.Reset();
        Assert.That(input.Handle(NuiEventType.MouseUp, "btn_entry_0", 0, true), Is.False,
            "An old press must not activate a replacement control with the same ID.");
        input.Handle(NuiEventType.MouseDown, "btn_entry_0", 0, true);
        Assert.That(input.Handle(NuiEventType.MouseUp, "btn_entry_0", 0, true), Is.True);
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

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void AnvilPayloadParser_MapsMouseButtonField(int button)
    {
        // GetEventPayload<T> uses JsonUtility.FromJson<T> in the installed Anvil version.
        string json = $"{{\"mouse_btn\":{button},\"mouse_pos\":{{\"x\":12,\"y\":34}}}}";
        Assert.That(JsonUtility.FromJson<CodexMousePayload>(json)!.MouseButton, Is.EqualTo(button));
    }

    [Test]
    public void AnvilPayloadParser_MissingButton_DoesNotDefaultToLeft()
    {
        Assert.That(JsonUtility.FromJson<CodexMousePayload>("{}")!.MouseButton, Is.Null);
    }

    [Test]
    public void AnvilPayloadParser_InvalidButton_RaisesTheCaughtExceptionType()
    {
        Assert.Catch<JsonException>(() =>
            JsonUtility.FromJson<CodexMousePayload>("{\"mouse_btn\":{}}"));
    }
}

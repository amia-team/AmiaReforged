using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;
using Bunit;
using Microsoft.JSInterop;
using NUnit.Framework;
using Moq;
using Microsoft.Extensions.DependencyInjection;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class GlyphCodeEditorTests
{
    [Test]
    public async Task Capture_rejects_delayed_callbacks_and_disposal_destroys_editor()
    {
        using var context = new Bunit.TestContext();
        var module = context.JSInterop.SetupModule("./js/glyph-editor.js?v=2");
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<GlyphCodeEditor.EditorSnapshot>("capture", _ => true)
            .SetResult(new("latest browser text", 3));
        string? changed = null;
        var cut = context.RenderComponent<GlyphCodeEditor>(p => p
            .Add(c => c.InitialSource, "original")
            .Add(c => c.SourceChanged, value => changed = value));
        Assert.That(cut.FindAll("textarea"), Is.Empty);
        Assert.That(await cut.InvokeAsync(() => cut.Instance.CaptureAsync()), Is.EqualTo("latest browser text"));
        await cut.InvokeAsync(() => cut.Instance.OnEditorChanged("old callback", 2));
        Assert.That(changed, Is.Null);
        await cut.InvokeAsync(() => cut.Instance.OnEditorChanged("next edit", 4));
        Assert.That(changed, Is.EqualTo("next edit"));
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());
        module.VerifyInvoke("destroy");
        await cut.InvokeAsync(() => cut.Instance.OnEditorChanged("disposed callback", 5));
        Assert.That(changed, Is.EqualTo("next edit"));
    }

    [Test]
    public async Task Failed_creation_cleans_up_module_and_allows_disposal()
    {
        using var context = new Bunit.TestContext();
        var module = context.JSInterop.SetupModule("./js/glyph-editor.js?v=2");
        module.Mode = JSRuntimeMode.Loose;
        module.SetupVoid("create", _ => true).SetException(new JSException("initialization failed"));
        var cut = context.RenderComponent<GlyphCodeEditor>(p => p.Add(c => c.InitialSource, "preserved"));
        Assert.That(cut.Find("textarea").HasAttribute("disabled"), Is.False);
        Assert.That(await cut.InvokeAsync(() => cut.Instance.CaptureAsync()), Is.EqualTo("preserved"));
        module.VerifyInvoke("destroy");
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());
        module.VerifyInvoke("destroy");
    }

    [Test]
    public async Task Failed_import_preserves_editable_source_in_fallback()
    {
        using var context = new Bunit.TestContext();
        var js = new Mock<IJSRuntime>();
        js.Setup(m => m.InvokeAsync<IJSObjectReference>("import", It.IsAny<object?[]>()))
            .ThrowsAsync(new JSException("load failed"));
        context.Services.AddSingleton(js.Object);
        var cut = context.RenderComponent<GlyphCodeEditor>(p => p.Add(c => c.InitialSource, "original"));
        Assert.That(cut.Find("textarea").HasAttribute("disabled"), Is.False);
        cut.Find("textarea").Input("fallback edit");
        Assert.That(await cut.InvokeAsync(() => cut.Instance.CaptureAsync()), Is.EqualTo("fallback edit"));
        cut.SetParametersAndRender(p => p.Add(c => c.ReadOnly, true));
        Assert.That(cut.Find("textarea").HasAttribute("disabled"), Is.True);
    }
}

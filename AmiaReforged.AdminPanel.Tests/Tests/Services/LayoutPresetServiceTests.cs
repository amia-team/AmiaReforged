using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using FluentAssertions;
using Microsoft.JSInterop;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Services;

[TestFixture]
public class LayoutPresetServiceTests
{
    private static Mock<IJSRuntime> JsReturning(string? storedJson)
    {
        var module = new Mock<IJSObjectReference>();
        module.Setup(m => m.InvokeAsync<string>("readJson", It.IsAny<object?[]>()))
            .ReturnsAsync(storedJson!);

        var js = new Mock<IJSRuntime>();
        js.Setup(s => s.InvokeAsync<IJSObjectReference>("import", It.IsAny<object?[]>()))
            .ReturnsAsync(module.Object);
        return js;
    }

    [Test]
    public void GetDefault_RegionsHasInspectorOpen()
    {
        EditorPreset preset = LayoutPresetService.GetDefault(WorldEngineEntityType.Regions);

        preset.InspectorOpen.Should().BeTrue();
        preset.PaletteOpen.Should().BeFalse();
        preset.InspectorWidth.Should().Be(340);
    }

    [Test]
    public void GetDefault_InteractionsHasBothRailsOpen()
    {
        EditorPreset preset = LayoutPresetService.GetDefault(WorldEngineEntityType.Interactions);

        preset.PaletteOpen.Should().BeTrue();
        preset.InspectorOpen.Should().BeTrue();
        preset.PaletteWidth.Should().Be(240);
        preset.InspectorWidth.Should().Be(320);
    }

    [Test]
    public void GetDefault_OtherTypesHaveNoRails()
    {
        EditorPreset preset = LayoutPresetService.GetDefault(WorldEngineEntityType.Items);

        preset.PaletteOpen.Should().BeFalse();
        preset.InspectorOpen.Should().BeFalse();
    }

    [Test]
    public async Task GetPresetAsync_ReturnsDefault_WhenStorageEmpty()
    {
        var service = new LayoutPresetService(JsReturning(null).Object);

        EditorPreset preset = await service.GetPresetAsync(WorldEngineEntityType.Regions);

        preset.Should().Be(LayoutPresetService.GetDefault(WorldEngineEntityType.Regions));
    }

    [Test]
    public async Task GetPresetAsync_PrefersStoredValues()
    {
        const string stored = """{"Interactions":{"paletteOpen":false,"inspectorOpen":true,"paletteWidth":300,"inspectorWidth":400}}""";
        var service = new LayoutPresetService(JsReturning(stored).Object);

        EditorPreset preset = await service.GetPresetAsync(WorldEngineEntityType.Interactions);

        preset.PaletteOpen.Should().BeFalse();
        preset.PaletteWidth.Should().Be(300);
        preset.InspectorWidth.Should().Be(400);
    }

    [Test]
    public async Task GetPresetAsync_ReturnsDefault_WhenJsThrows()
    {
        var js = new Mock<IJSRuntime>();
        js.Setup(s => s.InvokeAsync<IJSObjectReference>("import", It.IsAny<object?[]>()))
            .ThrowsAsync(new JSException("no js"));
        var service = new LayoutPresetService(js.Object);

        EditorPreset preset = await service.GetPresetAsync(WorldEngineEntityType.Items);

        preset.Should().Be(LayoutPresetService.GetDefault(WorldEngineEntityType.Items));
    }

    [Test]
    public async Task UpdatePresetAsync_PersistsAndReturnsUpdated()
    {
        var module = new Mock<IJSObjectReference>();
        module.Setup(m => m.InvokeAsync<string>("readJson", It.IsAny<object?[]>()))
            .ReturnsAsync((string?)null);
        var js = new Mock<IJSRuntime>();
        js.Setup(s => s.InvokeAsync<IJSObjectReference>("import", It.IsAny<object?[]>()))
            .ReturnsAsync(module.Object);
        var service = new LayoutPresetService(js.Object);

        await service.UpdatePresetAsync(
            WorldEngineEntityType.Regions, p => p with { InspectorOpen = false });

        EditorPreset preset = await service.GetPresetAsync(WorldEngineEntityType.Regions);
        preset.InspectorOpen.Should().BeFalse();
        module.Verify(m => m.InvokeAsync<object>(
            "writeJson", It.IsAny<object?[]>()), Times.Once);
    }
}

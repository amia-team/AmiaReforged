using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.System.UI.PlayerTools.AppearanceEditor;

public class AppearanceEditorView : WindowView<AppearanceEditorView>
{
    public override string Id => "playertools.appearanceoverview";
    public override string Title => "Appearance Editor";
    public override IWindowController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<AppearanceEditorController>(player);
    }

    public override NuiWindow? WindowTemplate { get; }
}

public class AppearanceEditorController : WindowController<AppearanceEditorView>
{
    public override void Init()
    {
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
    }

    protected override void OnClose()
    {
    }
}
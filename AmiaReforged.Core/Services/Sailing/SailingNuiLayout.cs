using Anvil.API;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Shared readable geometry. Long text uses NuiText, since NuiLabel is single-line.</summary>
public static class SailingNuiLayout
{
    public static NuiLabel Label(NuiProperty<string> text, float height = 28) => new(text)
    { Height = height, Margin = 0, Padding = 2, HorizontalAlign = NuiHAlign.Left, VerticalAlign = NuiVAlign.Middle, Tooltip = text };

    public static NuiText Text(NuiProperty<string> text, float height) => new(text)
    { Height = height, Margin = 0, Padding = 4, Tooltip = text };

    public static NuiListTemplateCell Cell(NuiElement element, float width) => new(element)
    { Width = width, VariableSize = false };

    public static NuiGroup Scroll(NuiLayout content) => new()
    { Element = content, Border = false, Scrollbars = NuiScrollbars.Auto, Margin = 0, Padding = 0 };

    // Anvil explicitly defines (-1,-1) as centered. A fixed readable window prevents shrinking
    // columns below their content size; the outer scroll group also accommodates smaller clients.
    public static NuiWindow Window(NuiLayout content, string title, float width, float height) => new(Scroll(content), title)
    { Geometry = new NuiRect(-1, -1, width, height), Closable = true, Resizable = false };
}

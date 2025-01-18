using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;

/// <summary>
/// This is the view responsible for the changelist panel of the Mythal Forge. See <see cref="MythalForgeView"/>.
/// </summary>
public class ChangelistView : IScryView
{
    public IScryPresenter Presenter { get; }

    public ChangelistView(IScryPresenter presenter)
    {
        Presenter = presenter;
    }

    /// <summary>
    /// Only concerned with building a NuiGroup for the changelist panel.
    /// </summary>
    /// <returns>A nui element intended only for use as an element of a larger view.</returns>
    public NuiLayout RootLayout()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiGroup
                {
                    Border = true,
                    Width = 400f,
                    Height = 400f,
                }
            }
        };
    }
}
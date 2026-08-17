using Anvil.API;

namespace AmiaReforged.Core.Services.Sailing;

public class NuiTest
{
    public void Test()
    {
        NuiLabel label = new(
            NuiProperty<string>.CreateValue("SEA SPRITE"));

        NuiButton button = new(
            NuiProperty<string>.CreateValue("AHEAD"));

        button.Id = "ahead_button";

        NuiColumn column = new();

        column.Children.Add(label);
        column.Children.Add(button);

        NuiWindow window = new(
            column,
            NuiProperty<string>.CreateValue("Sailing"));

        window.Closable = true;
    }
}
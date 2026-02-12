using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WindowingSystem;

[ServiceBinding(typeof(DevicePropertyService))]
public class DevicePropertyService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public int GetGuiWidth(NwPlayer player) => player.GetDeviceProperty(PlayerDeviceProperty.GuiWidth);

    public int GetGuiHeight(NwPlayer player) => player.GetDeviceProperty(PlayerDeviceProperty.GuiHeight);

    public int GetGuiScale(NwPlayer player) => player.GetDeviceProperty(PlayerDeviceProperty.GuiScale);
}

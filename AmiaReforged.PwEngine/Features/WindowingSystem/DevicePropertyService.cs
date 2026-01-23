using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WindowingSystem;

[ServiceBinding(typeof(DevicePropertyService))]
public class DevicePropertyService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public DevicePropertyService()
    {
        NwModule.Instance.OnClientEnter += LogDeviceDebug;
    }

    private void LogDeviceDebug(ModuleEvents.OnClientEnter obj)
    {
        Log.Info($"Gui Scale Weidth {GetGuiWidth(obj.Player)}");
        Log.Info($"Gui Scale Height {GetGuiHeight(obj.Player)}");
    }

    public int GetGuiWidth(NwPlayer player) => player.GetDeviceProperty(PlayerDeviceProperty.GuiWidth);

    public int GetGuiHeight(NwPlayer player) => player.GetDeviceProperty(PlayerDeviceProperty.GuiHeight);
}

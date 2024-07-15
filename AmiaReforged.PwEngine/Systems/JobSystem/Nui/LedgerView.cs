using AmiaReforged.PwEngine.Systems.WindowingSystem;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Nui;

public class LedgerView : NuiView<LedgerView>
{
    public override string Id { get; }
    public override string Title { get; }
    public override NuiWindow? WindowTemplate { get; }
    public override INuiController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<LedgerController>(player);
    }

    public LedgerView()
    {
        
    }
}

public class LedgerController : NuiController<LedgerView>
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
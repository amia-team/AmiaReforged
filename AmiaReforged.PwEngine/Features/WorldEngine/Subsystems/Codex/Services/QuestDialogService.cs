using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Services;

[ServiceBinding(typeof(QuestDialogService))]
public class QuestDialogService
{

    public QuestDialogService(DialogService service)
    {

    }
}

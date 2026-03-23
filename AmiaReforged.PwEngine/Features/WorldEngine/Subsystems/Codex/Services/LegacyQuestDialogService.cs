using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Services;

[ServiceBinding(typeof(LegacyQuestDialogService))]
public class LegacyQuestDialogService
{

    private const string QuestNpcTag = "quest_npc";

    public LegacyQuestDialogService(DialogService dialogService)
    {
        List<NwCreature> npcs = NwObject.FindObjectsOfType<NwCreature>().Where( c => c.Tag == QuestNpcTag ).ToList();

        foreach (NwCreature questGiver in npcs)
        {
        }
    }
}

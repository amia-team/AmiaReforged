using Anvil.API;

namespace AmiaReforged.System.Dynamic.Quest;

public interface IMiniQuestRewardStrategy
{
    public void DoRewardForEachItem(List<NwItem> questItems, NwCreature? questGiver, NwPlayer? player);
}
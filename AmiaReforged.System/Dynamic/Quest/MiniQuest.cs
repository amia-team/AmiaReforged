using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.Dynamic.Quest;

public class MiniQuest
{
    private readonly MiniQuestReward _miniQuestReward;
    private readonly NwPlayer? _player;
    private readonly NwCreature? _questGiver;

    public MiniQuest(NwCreature? questGiver, NwPlayer? player)
    {
        _questGiver = questGiver;
        _player = player;
        _miniQuestReward = new();
    }

    public void ProcessReward()
    {
        if (_questGiver == null || _player == null) return;
        if (_player.LoginCreature == null) return;

        string questTurnInItem = NWScript.GetLocalString(_questGiver, DynamicQuestLocals.MiniQuest.QuestItem);

        List<NwItem> questItems =
            _player.LoginCreature.Inventory.Items.Where(x => x.ResRef == questTurnInItem || x.Tag == questTurnInItem).ToList();

        bool hasNoQuestItems = questItems.Count == 0;
        string speakString = hasNoQuestItems
            ? NWScript.GetLocalString(_questGiver, DynamicQuestLocals.MiniQuest.QuestFailedLine)
            : NWScript.GetLocalString(_questGiver, DynamicQuestLocals.MiniQuest.QuestDoneLine);

        _miniQuestReward.DoRewardForEachItem(questItems, _questGiver, _player);

        _questGiver.SpeakString(speakString);
    }
}
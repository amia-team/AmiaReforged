using AmiaReforged.Core.Models.Settlement;
using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.Ledger;

public class PlayerJobLedger
{
    private const string JobLogTag = "js_jobjournal";
    private const string MerchantBoxTag = "js_merc_2_targ";

    private readonly NwPlayer _player;

    public IReadOnlyDictionary<string, int> JobLedger => PopulateLedger();

    public PlayerJobLedger(NwPlayer player)
    {
        _player = player;
    }

    private IReadOnlyDictionary<string, int> PopulateLedger()
    {
        NwItem? jobLog = _player.LoginCreature!.FindItemWithTag(JobLogTag);
        List<NwItem> storageBoxes = _player.LoginCreature.Inventory.Items.Where(i => i.Tag == MerchantBoxTag).ToList();

        return new Dictionary<string, int>();
    }
}

public class LedgerItem
{
    public string Name { get; set; }
    public Quality Quality { get; set; }
    public Material Material { get; set; }
}

public class JobLog
{
    private const string StoragePrefix = "storagebox";
    private readonly NwItem _jobLog;

    public JobLog(NwItem jobLog)
    {
        _jobLog = jobLog;
    }
}
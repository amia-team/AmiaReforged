using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Services.JobSystem;

/// <summary>
/// Converts legacy job log data to the new system. It makes a guess at what the old data represents and converts it to the new system.
/// </summary>
[ServiceBinding(typeof(JobLogConverter))]
public class JobLogConverter
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    public JobLogConverter()
    {
        NwArea? entry = NwModule.Instance.Areas.ToList().Find(a => a.Tag == "welcometotheeete");
        if (entry is null)
        {
            Log.Error("Could not find the entry area for the job log converter.");
            return;
        }
        
        entry.OnEnter += ConvertResources;
        Log.Info("Job log converter initialized.");
    }

    private void ConvertResources(AreaEvents.OnEnter obj)
    {
    }
}
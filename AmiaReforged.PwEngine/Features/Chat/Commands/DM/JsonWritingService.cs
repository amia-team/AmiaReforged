using AmiaReforged.PwEngine.Features.Module;
using Anvil.API;
using Anvil.Services;
using Newtonsoft.Json;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.DM;

/// <summary>
///    Service for writing JSON data to files.Spawns crea
/// </summary>
[ServiceBinding(typeof(JsonWritingService))]
public class JsonWritingService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly BlueprintManager _manager;
    private const string AreaToRest = "AnAreatoRest";
    private NwArea? _area;
    private readonly NwObjectDataMapper _mapper;

    private static readonly string[] VarPrefixes = ["day_spawn", "night_spawn"];

    private readonly Dictionary<string, CreatureData> _encounterCreatures = new();

    public JsonWritingService(BlueprintManager manager, NwObjectDataMapper mapper)
    {
        _area = NwModule.Instance.Areas.FirstOrDefault(a => a.Tag == AreaToRest);
        _manager = manager;
        _mapper = mapper;
    }

    public void WriteAllCreatureData()
    {
        // Write all creature data to a JSON file
    }

    public void WriteOnlyEncounterCreatureData()
    {
        IEnumerable<NwArea> areas = NwModule.Instance.Areas.Where(a => a.Tag != AreaToRest);
        if (_area == null) return;

        NwWaypoint? nwWaypoint = _area.FindObjectsOfTypeInArea<NwWaypoint>().FirstOrDefault();

        if (nwWaypoint == null) return;

        foreach (NwArea area in NwModule.Instance.Areas)
        {
            Log.Info("Checking area: " + area.Name);
            List<string> creatureResRefs = GetEncounterResRefs(area).ToList();

            foreach (string creatureResRef in creatureResRefs)
            {
                if (string.IsNullOrWhiteSpace(creatureResRef))
                {
                    Log.Info("Resref is null or empty.");
                    continue;
                }

                if (_encounterCreatures.ContainsKey(creatureResRef))
                {
                    if(_encounterCreatures[creatureResRef].FoundInAreas.Contains(area.Name)) continue;

                    Log.Info("Creature can also be found in: " + area.Name);
                    _encounterCreatures[creatureResRef].FoundInAreas.Add(area.Name);

                    continue;
                }

                NwCreature? creature = NwCreature.Create(creatureResRef, NwModule.Instance.StartingLocation);

                if (creature == null) continue;
                Log.Info("Created creature: " + creature.Name);
                CreatureData creatureAsPlainObject = _mapper.FromCreature(creature);
                creatureAsPlainObject.FoundInAreas.Add(area.Name);
                _encounterCreatures.TryAdd(creatureResRef, creatureAsPlainObject);

                creature.Destroy();
                Log.Info("Destroyed creature: " + creature.Name);
            }
        }

        List<CreatureData> creatureDataList = _encounterCreatures.Values.ToList();

        string toString = JsonConvert.SerializeObject(creatureDataList, Formatting.Indented);
        WriteToFile(toString, "encounters");
    }

    private void WriteToFile(string toString, string fileName)
    {
        string path = NwServer.Instance.UserDirectory + $"/development/{fileName}.json";

        using StreamWriter file = File.CreateText(path);

        file.WriteLine(toString);

        Log.Info("Wrote creatures to file.");
    }

    private string[] GetEncounterResRefs(NwArea area)
    {
        List<string> resRefs = [];

        foreach (string variableName in area.LocalVariables.Where(v => v.Name.Contains("_spawn")).Select(v => v.Name))
        {
            resRefs.Add(NWScript.GetLocalString(area, variableName));
        }

        return resRefs.Distinct().ToArray();
    }

    public void WriteOnlySummonCreatureData()
    {
        // Write only summon creature data to a JSON file
    }

    public void WriteAllFamiliarCreatureData()
    {
        // Write all familiar creature data to a JSON file
    }

    public void WriteAllCompanionCreatureData()
    {
        // Write all companion creature data to a JSON file
    }
}

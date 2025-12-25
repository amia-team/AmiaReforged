using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Features.CharacterTools.CustomSummon;

public sealed class CustomSummonModel
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly NwPlayer _player;
    private readonly NwItem _widget;
    private int _temporarySelection = -1;

    public CustomSummonModel(NwPlayer player, NwItem widget)
    {
        _player = player;
        _widget = widget;
    }

    public List<string> GetSummonNames()
    {
        string? namesJson = _widget.GetObjectVariable<LocalVariableString>("summon_names_json").Value;
        if (string.IsNullOrEmpty(namesJson))
        {
            return new List<string>();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(namesJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public int GetCurrentSelection()
    {
        int selection = _widget.GetObjectVariable<LocalVariableInt>("summon_choice").Value;
        List<string> names = GetSummonNames();

        if (selection < 0 || selection >= names.Count)
        {
            selection = 0;
        }

        return selection;
    }

    public void SetCurrentSelection(int selection)
    {
        List<string> names = GetSummonNames();

        Log.Info($"SetCurrentSelection called with selection: {selection}, names.Count: {names.Count}");

        if (selection < 0 || selection >= names.Count)
        {
            Log.Warn($"Selection {selection} out of bounds, resetting to 0");
            selection = 0;
        }

        _widget.GetObjectVariable<LocalVariableInt>("summon_choice").Value = selection;
        Log.Info($"Set summon_choice variable to: {selection}");

        // Update widget name to reflect the current selection
        if (names.Count > 0 && selection < names.Count)
        {
            string newName = names[selection];
            _widget.Name = $"Summon <cÿÔ¦>{newName}</c>".ColorString(ColorConstants.Cyan);
            Log.Info($"Updated widget to: {newName}");
        }
    }

    public void SetTemporarySelection(int selection)
    {
        _temporarySelection = selection;
    }

    public string GetSelectedSummonName()
    {
        List<string> names = GetSummonNames();
        int selection = GetCurrentSelection();

        Log.Info($"GetSelectedSummonName: selection={selection}, names.Count={names.Count}");

        if (selection >= 0 && selection < names.Count)
        {
            string name = names[selection];
            Log.Info($"Returning summon name: {name}");
            return name;
        }

        Log.Warn("Returning 'Unknown' - selection out of bounds");
        return "Unknown";
    }

    public List<string> GetSummonJsons()
    {
        string? jsonsJson = _widget.GetObjectVariable<LocalVariableString>("summon_jsons_json").Value;
        if (string.IsNullOrEmpty(jsonsJson))
        {
            return new List<string>();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(jsonsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public void SetSummonNames(List<string> names)
    {
        string namesJson = System.Text.Json.JsonSerializer.Serialize(names);
        _widget.GetObjectVariable<LocalVariableString>("summon_names_json").Value = namesJson;
    }

    public void SetSummonJsons(List<string> jsons)
    {
        string jsonsJson = System.Text.Json.JsonSerializer.Serialize(jsons);
        _widget.GetObjectVariable<LocalVariableString>("summon_jsons_json").Value = jsonsJson;
    }

    public void UpdateWidgetAppearance()
    {
        List<string> summonNames = GetSummonNames();

        if (summonNames.Count == 0)
        {
            _widget.Name = "Custom Summon Widget".ColorString(ColorConstants.Cyan);
            _widget.Description = "This widget has no summons configured yet.";
            return;
        }

        string primaryName = summonNames[0];
        _widget.Name = $"<cÿÔ¦>{primaryName}</c>".ColorString(ColorConstants.Cyan);

        string description = $"This widget will summon {primaryName.ColorString(ColorConstants.Cyan)}.";

        if (summonNames.Count > 1)
        {
            description += " Use the widget on yourself to select from other stored summons.";
            for (int i = 1; i < summonNames.Count; i++)
            {
                description += $"\n\nSummon {i + 1}: {summonNames[i].ColorString(ColorConstants.Cyan)}";
            }
        }

        _widget.Description = description;
    }
}


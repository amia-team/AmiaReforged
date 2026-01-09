using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.LanguageChecker;

/// <summary>
/// Model for Language Checker tool - displays a player's languages for DM review.
/// </summary>
public class LanguageCheckerModel
{
    private readonly NwPlayer _dmPlayer;
    private NwCreature? _selectedCharacter;
    private string _languageFilter = string.Empty;

    public List<string> AutomaticLanguages { get; private set; } = new();
    public List<string> ChosenLanguages { get; private set; } = new();
    public List<string> DmAddedLanguages { get; private set; } = new();

    public event EventHandler? OnCharacterSelected;

    public LanguageCheckerModel(NwPlayer dmPlayer)
    {
        _dmPlayer = dmPlayer;
    }

    /// <summary>
    /// Enters targeting mode for the DM to select a character.
    /// </summary>
    public void EnterTargetingMode()
    {
        _dmPlayer.EnterTargetMode(OnTargetSelected);
    }

    private void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.TargetObject is not NwCreature creature)
        {
            _dmPlayer.SendServerMessage("Invalid target. Please select a creature.");
            return;
        }

        if (!creature.IsPlayerControlled)
        {
            _dmPlayer.SendServerMessage("Target must be a player character.");
            return;
        }

        SetCharacter(creature);
        OnCharacterSelected?.Invoke(this, EventArgs.Empty);
    }

    // ...existing code...
    public void SetCharacter(NwCreature? character)
    {
        _selectedCharacter = character;
        AutomaticLanguages.Clear();
        ChosenLanguages.Clear();
        DmAddedLanguages.Clear();

        if (character == null)
            return;

        // Get PC Key
        NwItem? pcKey = character.Inventory.Items.FirstOrDefault(item => item.ResRef == LanguageCheckerData.PcKeyResRef);
        if (pcKey == null)
            return;

        // Load automatic languages
        LocalVariableString autoVar = pcKey.GetObjectVariable<LocalVariableString>(LanguageCheckerData.LanguagesAutomaticVar);
        string? autoLanguages = autoVar.Value;
        if (!string.IsNullOrEmpty(autoLanguages))
        {
            AutomaticLanguages = autoLanguages.Split('|').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }

        // Load chosen languages
        LocalVariableString chosenVar = pcKey.GetObjectVariable<LocalVariableString>(LanguageCheckerData.LanguagesChosenVar);
        string? chosenLanguages = chosenVar.Value;
        if (!string.IsNullOrEmpty(chosenLanguages))
        {
            ChosenLanguages = chosenLanguages.Split('|').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }

        // Load DM-added languages
        LocalVariableString dmVar = pcKey.GetObjectVariable<LocalVariableString>(LanguageCheckerData.LanguagesDmAddedVar);
        string? dmLanguages = dmVar.Value;
        if (!string.IsNullOrEmpty(dmLanguages))
        {
            DmAddedLanguages = dmLanguages.Split('|').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }
    }

    /// <summary>
    /// Gets the selected character's name.
    /// </summary>
    public string GetCharacterName()
    {
        return _selectedCharacter?.Name ?? "(No Character Selected)";
    }

    /// <summary>
    /// Gets all languages (automatic + chosen) sorted alphabetically.
    /// </summary>
    public List<string> GetAllLanguages()
    {
        List<string> all = new List<string>(AutomaticLanguages);
        all.AddRange(ChosenLanguages);
        all = all.Distinct().ToList();
        all.Sort();
        return all;
    }

    /// <summary>
    /// Gets total language count.
    /// </summary>
    public int GetTotalLanguageCount()
    {
        return GetAllLanguages().Count;
    }

    /// <summary>
    /// Gets a list of all available languages in the system.
    /// </summary>
    public List<string> GetAvailableLanguages()
    {
        // Return all possible languages (combine from different sources or a comprehensive list)
        // This can be expanded based on your language definition
        List<string> allLanguages = new List<string>
        {
            "Abyssal", "Aglarondan", "Alzhedo", "Aquan", "Auran", "Celestial", "Chessentan", "Chondathan",
            "Chultan", "Common", "Damaran", "Dambrathan", "Draconic", "Drow", "Drow Sign", "Druidic",
            "Durpari", "Dwarven", "Elven", "Giant", "Gnoll", "Gnomish", "Goblin", "Halruaan", "Hin",
            "Ignan", "Illuskan", "Infernal", "Kozakuran", "Lantanese", "Loross", "Midani", "Mulhorandi",
            "Nexalan", "Orcish", "Rashemi", "Shaaran", "Shou", "Slaadi", "Sylvan", "Tashalan", "Terran",
            "Thayan", "Thieves' Cant", "Thorass", "Tuigan", "Turmic", "Uluik", "Undercommon", "Untheric"
        };

        // Remove duplicates and sort
        allLanguages = allLanguages.Distinct().ToList();
        allLanguages.Sort();

        // Apply filter if one is set
        if (!string.IsNullOrWhiteSpace(_languageFilter))
        {
            allLanguages = allLanguages
                .Where(lang => lang.Contains(_languageFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return allLanguages;
    }

    /// <summary>
    /// Sets the language filter for available languages.
    /// </summary>
    public void SetLanguageFilter(string filter)
    {
        _languageFilter = filter;
    }

    /// <summary>
    /// Removes a language from the character's languages (either chosen or DM-added).
    /// </summary>
    public void RemoveChosenLanguage(string language)
    {
        if (_selectedCharacter == null) return;

        // Remove from DM-added first, then from chosen
        if (DmAddedLanguages.Contains(language))
        {
            DmAddedLanguages.Remove(language);
            SaveDmAddedLanguages();
        }
        else if (ChosenLanguages.Contains(language))
        {
            ChosenLanguages.Remove(language);
            SaveChosenLanguages();
        }
    }

    /// <summary>
    /// Adds a language to the character's DM-added languages (doesn't affect chosen language count).
    /// </summary>
    public void AddChosenLanguage(string language)
    {
        if (_selectedCharacter == null) return;

        // Check if language is already in either list
        if (DmAddedLanguages.Contains(language) || ChosenLanguages.Contains(language))
            return;

        // Add to DM-added languages
        DmAddedLanguages.Add(language);
        SaveDmAddedLanguages();
    }

    /// <summary>
    /// Saves chosen languages to the PC Key.
    /// </summary>
    private void SaveChosenLanguages()
    {
        if (_selectedCharacter == null) return;

        NwItem? pcKey = _selectedCharacter.Inventory.Items.FirstOrDefault(item => item.ResRef == LanguageCheckerData.PcKeyResRef);
        if (pcKey == null) return;

        string chosenLanguagesStr = string.Join("|", ChosenLanguages);
        LocalVariableString chosenVar = pcKey.GetObjectVariable<LocalVariableString>(LanguageCheckerData.LanguagesChosenVar);
        chosenVar.Value = chosenLanguagesStr;
    }

    /// <summary>
    /// Saves DM-added languages to the PC Key.
    /// </summary>
    private void SaveDmAddedLanguages()
    {
        if (_selectedCharacter == null) return;

        NwItem? pcKey = _selectedCharacter.Inventory.Items.FirstOrDefault(item => item.ResRef == LanguageCheckerData.PcKeyResRef);
        if (pcKey == null) return;

        string dmLanguagesStr = string.Join("|", DmAddedLanguages);
        LocalVariableString dmVar = pcKey.GetObjectVariable<LocalVariableString>(LanguageCheckerData.LanguagesDmAddedVar);
        dmVar.Value = dmLanguagesStr;
    }
}


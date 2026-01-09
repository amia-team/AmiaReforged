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

        // Add class-based automatic languages
        AddClassBasedAutomaticLanguages();

        // Save the automatic languages (including class-based ones) back to the PC Key
        SaveAutomaticLanguages();


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
    /// Gets total language count (including automatic languages).
    /// </summary>
    public int GetTotalLanguageCount()
    {
        return GetAllLanguages().Count;
    }

    /// <summary>
    /// Gets the maximum number of languages the character can select (before class/feat bonuses).
    /// </summary>
    public int GetMaxLanguageCount()
    {
        if (_selectedCharacter == null)
            return 0;

        // Base max languages from class/race
        int maxLanguages = GetBaseLanguageMax();

        // Bonus from Lore skill (1 bonus per 10 ranks of base Lore skill)
        int loreBonus = GetLoreSkillBonus();
        maxLanguages += loreBonus;

        // Bonus from Epic Skill Focus: Lore feat
        int epicSkillFocusBonus = HasEpicSkillFocusLore() ? 1 : 0;
        maxLanguages += epicSkillFocusBonus;

        // Bonus from having at least 5 Bard levels
        int bardBonus = GetBardLevelBonus();
        maxLanguages += bardBonus;

        return maxLanguages;
    }

    /// <summary>
    /// Gets the base maximum languages for the character (from class/race, before feat/skill bonuses).
    /// This includes the base language from class/race plus INT modifier bonus.
    /// </summary>
    private int GetBaseLanguageMax()
    {
        if (_selectedCharacter == null)
            return 0;

        try
        {
            // Get INT modifier bonus (characters get 1 bonus language per point of INT modifier)
            int intModifier = _selectedCharacter.GetAbilityModifier(Ability.Intelligence);

            return Math.Max(intModifier, 0); // Ensure at least 0 languages
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets language bonus from Lore skill rank (1 bonus per 10 base ranks, not including gear).
    /// </summary>
    private int GetLoreSkillBonus()
    {
        if (_selectedCharacter == null)
            return 0;

        try
        {
            // Get Lore skill rank
            int skillRank = _selectedCharacter.GetSkillRank(Skill.Lore);

            // Award 1 language per 10 base ranks
            return skillRank / 10;
        }
        catch
        {
            // If there's any error, just return 0
            return 0;
        }
    }

    /// <summary>
    /// Checks if the character has the Epic Skill Focus: Lore feat.
    /// </summary>
    private bool HasEpicSkillFocusLore()
    {
        if (_selectedCharacter == null)
            return false;

        try
        {
            // Check if the character has the Epic Skill Focus: Lore feat
            NwFeat? feat = _selectedCharacter.Feats.FirstOrDefault(f => f.FeatType == Feat.EpicSkillFocusLore);
            return feat != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets language bonus from Bard class level (1 bonus if 5+ levels).
    /// </summary>
    private int GetBardLevelBonus()
    {
        if (_selectedCharacter == null)
            return 0;

        try
        {
            // Get Bard class level - iterate through classes to find Bard
            int bardLevel = 0;

            foreach (var classInfo in _selectedCharacter.Classes)
            {
                string className = classInfo.Class.Name.ToString();

                // Check if this is the Bard class by name
                if (className.Contains("Bard", StringComparison.OrdinalIgnoreCase))
                {
                    bardLevel = classInfo.Level;
                    break;
                }
            }
            return bardLevel >= 5 ? 1 : 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Adds class-based automatic languages to the character's language list.
    /// </summary>
    private void AddClassBasedAutomaticLanguages()
    {
        if (_selectedCharacter == null)
            return;

        try
        {
            // Dragon Disciple: Draconic is automatic if 10+ levels
            int dragonDiscipleLevel = 0;

            foreach (var classInfo in _selectedCharacter.Classes)
            {
                string className = classInfo.Class.Name.ToString();

                // Check if this is the Dragon Disciple class by name
                if (className.Contains("Dragon Disciple", StringComparison.OrdinalIgnoreCase))
                {
                    dragonDiscipleLevel = classInfo.Level;
                    break;
                }
            }

            if (dragonDiscipleLevel >= 10)
            {
                if (!AutomaticLanguages.Contains("Draconic"))
                {
                    AutomaticLanguages.Add("Draconic");
                }
            }
        }
        catch
        {
            // Silently continue if there's an error
        }
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
            "Thayan", "Thieves' Cant", "Thorass", "Tuigan", "Turmic", "Uluik", "Undercommon", "Untheric",
            "Waelan", "Yuan-Ti", "Gith", "Aboleth", "Kenku", "Kentaur"
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

    /// <summary>
    /// Saves automatic languages to the PC Key.
    /// </summary>
    private void SaveAutomaticLanguages()
    {
        if (_selectedCharacter == null) return;

        NwItem? pcKey = _selectedCharacter.Inventory.Items.FirstOrDefault(item => item.ResRef == LanguageCheckerData.PcKeyResRef);
        if (pcKey == null) return;

        string autoLanguagesStr = string.Join("|", AutomaticLanguages);
        LocalVariableString autoVar = pcKey.GetObjectVariable<LocalVariableString>(LanguageCheckerData.LanguagesAutomaticVar);
        autoVar.Value = autoLanguagesStr;
    }
}


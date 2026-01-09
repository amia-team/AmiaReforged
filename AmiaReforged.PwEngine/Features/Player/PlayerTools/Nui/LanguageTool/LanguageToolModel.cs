using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.LanguageTool;

/// <summary>
/// Model for managing character languages including automatic and chosen languages.
/// </summary>
public class LanguageToolModel
{
    private const string PcKeyResRef = "ds_pckey";
    private const string LanguagesChosenVar = "LANGUAGES_CHOSEN";
    private const string LanguagesAutomaticVar = "LANGUAGES_AUTOMATIC";
    private const string LanguagesLockedVar = "LANGUAGES_LOCKED";

    private readonly NwPlayer _player;
    private readonly NwCreature? _character;

    public LanguageToolModel(NwPlayer player)
    {
        _player = player;
        _character = player.LoginCreature;

        LoadLanguages();
    }

    public List<string> AutomaticLanguages { get; private set; } = new();
    public List<string> ChosenLanguages { get; private set; } = new();
    public List<string> SavedLanguages { get; private set; } = new(); // Languages that were loaded from PC Key
    public List<string> AvailableLanguages { get; private set; } = new();
    public int MaxChoosableLanguages { get; private set; }
    public bool IsLocked { get; private set; }

    /// <summary>
    /// Gets a fresh PC Key reference from the character's inventory.
    /// </summary>
    private NwItem? GetPcKey()
    {
        return _character?.Inventory.Items.FirstOrDefault(item => item.ResRef == PcKeyResRef);
    }

    /// <summary>
    /// Loads languages from PC Key if they exist, or calculates them from character stats.
    /// </summary>
    private void LoadLanguages()
    {
        if (_character == null)
        {
            return;
        }

        // Load automatic languages based on racial type
        int racialType = _character.Race.Id;
        AutomaticLanguages = GetAutomaticLanguagesForRace(racialType);

        // Add class-specific automatic languages
        AddClassBasedLanguages();

        // Calculate max choosable languages
        MaxChoosableLanguages = CalculateMaxLanguages();

        // Load previously saved languages from PC Key
        NwItem? pcKey = GetPcKey();
        if (pcKey != null)
        {
            LocalVariableString langVar = pcKey.GetObjectVariable<LocalVariableString>(LanguagesChosenVar);
            string? savedLanguages = langVar.Value;

            if (!string.IsNullOrEmpty(savedLanguages))
            {
                ChosenLanguages = savedLanguages.Split('|').ToList();
                SavedLanguages = new List<string>(ChosenLanguages); // Keep track of what was saved
            }
        }

        // Check if they have any previously saved languages
        IsLocked = SavedLanguages.Count > 0;

        // Build available languages list (excluding automatic ones)
        BuildAvailableLanguagesList();
    }

    /// <summary>
    /// Gets automatic languages for the given racial type.
    /// </summary>
    private List<string> GetAutomaticLanguagesForRace(int racialType)
    {
        if (LanguageData.RacialAutomaticLanguages.TryGetValue(racialType, out List<string>? languages))
        {
            return new List<string>(languages);
        }

        // Default to Common if racial type not found
        return new List<string> { "Common" };
    }

    /// <summary>
    /// Adds class-specific languages that are gained automatically.
    /// </summary>
    private void AddClassBasedLanguages()
    {
        if (_character == null) return;

        // Druids get Druidic automatically at 5+ levels
        if (_character.GetClassInfo(ClassType.Druid)?.Level > 5)
        {
            if (!AutomaticLanguages.Contains(LanguageData.SpecialLanguages.Druidic))
            {
                AutomaticLanguages.Add(LanguageData.SpecialLanguages.Druidic);
            }
        }

        // Rogues get Thieves' Cant automatically at 5+ levels
        if (_character.GetClassInfo(ClassType.Rogue)?.Level > 4)
        {
            if (!AutomaticLanguages.Contains(LanguageData.SpecialLanguages.ThievesCant))
            {
                AutomaticLanguages.Add(LanguageData.SpecialLanguages.ThievesCant);
            }
        }

        // Dragon Disciples get Draconic automatically at 10+ levels
        int ddLevel = _character.GetClassInfo(ClassType.DragonDisciple)?.Level ?? 0;
        if (ddLevel >= 10)
        {
            if (!AutomaticLanguages.Contains("Draconic"))
            {
                AutomaticLanguages.Add("Draconic");
            }
        }
    }

    /// <summary>
    /// Calculates the maximum number of languages a character can choose.
    /// </summary>
    private int CalculateMaxLanguages()
    {
        if (_character == null) return 0;

        // Get base Intelligence modifier (without gear)
        int baseInt = _character.GetAbilityScore(Ability.Intelligence, true);
        int intModifier = (baseInt - 10) / 2;

        // Start with INT modifier only (no base +1)
        int totalLanguages = Math.Max(0, intModifier);

        // Add bonus from Lore skill (1 bonus per 10 base ranks)
        int loreBonus = GetLoreSkillBonus();
        totalLanguages += loreBonus;

        // Add bonus from Epic Skill Focus: Lore feat
        if (HasEpicSkillFocusLore())
        {
            totalLanguages += 1;
        }

        // Add bonus from Bard class (5+ levels = 1 bonus)
        int bardBonus = GetBardLevelBonus();
        totalLanguages += bardBonus;

        return totalLanguages;
    }

    /// <summary>
    /// Gets language bonus from Lore skill rank (1 bonus per 10 base ranks, not including gear).
    /// </summary>
    private int GetLoreSkillBonus()
    {
        if (_character == null)
            return 0;

        try
        {
            // Get Lore skill rank
            int skillRank = _character.GetSkillRank(Skill.Lore, true);

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
        if (_character == null)
            return false;

        try
        {
            // Check if the character has the Epic Skill Focus: Lore feat
            NwFeat? feat = _character.Feats.FirstOrDefault(f => f.FeatType == Feat.EpicSkillFocusLore);
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
        if (_character == null)
            return 0;

        try
        {
            // Get Bard class level by iterating through classes
            int bardLevel = 0;
            foreach (var classInfo in _character.Classes)
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
    /// Builds the list of available languages that can be chosen.
    /// </summary>
    private void BuildAvailableLanguagesList()
    {
        AvailableLanguages = new List<string>();

        foreach (string language in LanguageData.AllSelectableLanguages)
        {
            // Skip if already an automatic language
            if (AutomaticLanguages.Contains(language))
                continue;

            // Skip if already chosen
            if (ChosenLanguages.Contains(language))
                continue;

            AvailableLanguages.Add(language);
        }

        // Add special languages based on requirements
        AddSpecialLanguagesIfQualified();

        // Sort alphabetically
        AvailableLanguages.Sort();
    }

    /// <summary>
    /// Adds special languages if the character qualifies for them.
    /// </summary>
    private void AddSpecialLanguagesIfQualified()
    {
        if (_character == null) return;

        // Thorass requires 5+ INT modifier
        int baseInt = _character.GetAbilityScore(Ability.Intelligence, true);
        int intModifier = (baseInt - 10) / 2;

        if (intModifier >= 5 && !AutomaticLanguages.Contains(LanguageData.SpecialLanguages.Thorass)
            && !ChosenLanguages.Contains(LanguageData.SpecialLanguages.Thorass))
        {
            AvailableLanguages.Add(LanguageData.SpecialLanguages.Thorass);
        }

        // Loross requires 8+ INT modifier
        if (intModifier >= 8 && !AutomaticLanguages.Contains(LanguageData.SpecialLanguages.Loross)
            && !ChosenLanguages.Contains(LanguageData.SpecialLanguages.Loross))
        {
            AvailableLanguages.Add(LanguageData.SpecialLanguages.Loross);
        }

        // Drow Sign Language can be chosen by Half Drow (racial type 41)
        int racialType = _character.Race.Id;
        if (racialType == 41 && !AutomaticLanguages.Contains(LanguageData.SpecialLanguages.DrowSignLanguage)
            && !ChosenLanguages.Contains(LanguageData.SpecialLanguages.DrowSignLanguage))
        {
            AvailableLanguages.Add(LanguageData.SpecialLanguages.DrowSignLanguage);
        }
    }

    /// <summary>
    /// Adds a language to the chosen list.
    /// Can be done even after initial save, as long as there are slots remaining.
    /// </summary>
    public bool AddLanguage(string language)
    {
        // Can add as long as we haven't reached max, regardless of IsLocked status
        if (ChosenLanguages.Count >= MaxChoosableLanguages) return false;
        if (ChosenLanguages.Contains(language)) return false;
        if (!AvailableLanguages.Contains(language)) return false;

        ChosenLanguages.Add(language);
        AvailableLanguages.Remove(language);
        return true;
    }

    /// <summary>
    /// Removes a language from the chosen list.
    /// Can remove languages added in current session, but NOT previously saved languages.
    /// </summary>
    public bool RemoveLanguage(string language)
    {
        if (!ChosenLanguages.Contains(language)) return false;

        // Check if this language was previously saved - can't remove those
        if (SavedLanguages.Contains(language))
        {
            return false; // This language was saved before, can't remove it
        }

        // This language was added in the current session, can remove it
        ChosenLanguages.Remove(language);
        BuildAvailableLanguagesList(); // Rebuild to re-add the language
        return true;
    }

    /// <summary>
    /// Saves the chosen languages to the PC Key.
    /// Can be called multiple times to add more languages.
    /// Also saves automatic languages for DM reference.
    /// </summary>
    public bool SaveLanguages()
    {
        NwItem? pcKey = GetPcKey();
        if (pcKey == null)
        {
            _player.SendServerMessage("Could not find PC Key. Languages cannot be saved.", ColorConstants.Red);
            return false;
        }

        // Save chosen languages
        string languagesString = string.Join("|", ChosenLanguages);

        if (string.IsNullOrEmpty(languagesString))
        {
            // Delete the variable if there are no chosen languages
            pcKey.GetObjectVariable<LocalVariableString>(LanguagesChosenVar).Delete();
        }
        else
        {
            // Save the chosen languages to PC Key
            LocalVariableString langVar = pcKey.GetObjectVariable<LocalVariableString>(LanguagesChosenVar);
            langVar.Value = languagesString;
        }

        // Save automatic languages for DM reference
        string automaticLanguagesString = string.Join("|", AutomaticLanguages.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrEmpty(automaticLanguagesString))
        {
            LocalVariableString autoLangVar = pcKey.GetObjectVariable<LocalVariableString>(LanguagesAutomaticVar);
            autoLangVar.Value = automaticLanguagesString;
        }

        // Update saved languages list to match what we just saved
        SavedLanguages = new List<string>(ChosenLanguages);

        // Mark as locked if there are any saved languages
        IsLocked = SavedLanguages.Count > 0;

        _player.SendServerMessage("Languages saved successfully!", ColorConstants.Green);
        return true;
    }

    /// <summary>
    /// Gets all languages (automatic + chosen) as a formatted string.
    /// </summary>
    public string GetAllLanguagesString()
    {
        List<string> allLanguages = new List<string>(AutomaticLanguages);
        allLanguages.AddRange(ChosenLanguages);
        allLanguages.Sort();
        return string.Join(", ", allLanguages);
    }
}

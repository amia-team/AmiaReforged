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

    public List<string> AutomaticLanguages { get; private set; } = new();
    public List<string> ChosenLanguages { get; private set; } = new();

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
}


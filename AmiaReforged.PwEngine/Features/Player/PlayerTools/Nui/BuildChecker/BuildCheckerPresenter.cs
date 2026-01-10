using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.Races.Races;
using Anvil.API;
using Anvil.API.Events;
using System.Text;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.BuildChecker;

public sealed class BuildCheckerPresenter : ScryPresenter<BuildCheckerView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public override BuildCheckerView View { get; }

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private NuiWindowToken? _autoRebuildModalToken;

    public override NuiWindowToken Token() => _token;

    public BuildCheckerPresenter(BuildCheckerView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 630f, 670f),
            Resizable = true
        };
    }

    public override void Create()
    {
        if (_window is null) InitBefore();

        if (_window is null)
        {
            _player.SendServerMessage("The window could not be created. Screenshot this message and report it to a DM.", ColorConstants.Orange);
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
            return;

        // Initialize bind values
        if (_player.LoginCreature == null)
        {
            Token().SetBindValue(View.CharacterInfo, "No character available");
            Token().SetBindValue(View.LevelupInfo, "You must be logged in to view your build information.");
            return;
        }

        Token().SetBindValue(View.CharacterInfo, $"Character: {_player.LoginCreature.Name} (Level {_player.LoginCreature.Level})");
        Token().SetBindValue(View.LevelupInfo, "");
        Token().SetBindValue(View.LevelFilter, 0); // 0 = All Levels
        Token().SetBindValue(View.RedoButtonsEnabled, true); // Buttons enabled by default

        // Watch for level filter changes
        Token().SetBindWatch(View.LevelFilter, true);

        // Load initial data
        UpdateLevelupInfo();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType == NuiEventType.Watch && ev.ElementId == View.LevelFilter.Key)
        {
            // Level filter changed, update the display
            UpdateLevelupInfo();
            return;
        }

        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_redo_last_level":
                HandleRedoLastLevel();
                break;

            case "btn_redo_last_2_levels":
                HandleRedoLast2Levels();
                break;

            case "btn_auto_rebuild":
                OpenAutoRebuildModal();
                break;
        }
    }

    private void UpdateLevelupInfo()
    {
        if (_player.LoginCreature == null) return;

        StringBuilder sb = new();
        int totalLevels = _player.LoginCreature.Level;
        int levelFilter = Token().GetBindValue(View.LevelFilter);

        // Determine which levels to display
        int startLevel = levelFilter == 0 ? 1 : levelFilter;
        int endLevel = levelFilter == 0 ? totalLevels : levelFilter;

        if (levelFilter == 0)
        {
            sb.AppendLine($"=== Level-up Information for {_player.LoginCreature.Name} ===\n");
        }
        else
        {
            sb.AppendLine($"=== Level {levelFilter} Information for {_player.LoginCreature.Name} ===\n");
        }

        for (int level = startLevel; level <= endLevel; level++)
        {
            CreatureLevelInfo levelInfo = _player.LoginCreature.GetLevelStats(level);

            sb.AppendLine($"--- Level {level} ---");
            sb.AppendLine($"Class: {levelInfo.ClassInfo.Class.Name}");
            sb.AppendLine($"Hit Points: +{levelInfo.HitDie}");

            // Skills - check all skills for points allocated at this level
            List<Skill> allSkills =
            [
                Skill.AnimalEmpathy, Skill.Appraise, Skill.Bluff, Skill.Concentration,
                Skill.CraftArmor, Skill.CraftTrap, Skill.CraftWeapon, Skill.DisableTrap,
                Skill.Discipline, Skill.Heal, Skill.Hide, Skill.Intimidate,
                Skill.Listen, Skill.Lore, Skill.MoveSilently, Skill.OpenLock,
                Skill.Parry, Skill.Perform, Skill.Persuade, Skill.PickPocket,
                Skill.Ride, Skill.Search, Skill.SetTrap, Skill.Spellcraft,
                Skill.Spot, Skill.Taunt, Skill.Tumble, Skill.UseMagicDevice
            ];

            List<string> skillsAtLevel = new();
            foreach (Skill skill in allSkills)
            {
                sbyte skillRank = levelInfo.GetSkillRank(skill);
                if (skillRank > 0)
                {
                    skillsAtLevel.Add($"  - {skill}: +{skillRank}");
                }
            }

            if (skillsAtLevel.Any())
            {
                sb.AppendLine("Skills:");
                foreach (string skillLine in skillsAtLevel)
                {
                    sb.AppendLine(skillLine);
                }
            }

            // Feats
            if (levelInfo.Feats.Any())
            {
                sb.AppendLine("Feats:");
                foreach (var feat in levelInfo.Feats)
                {
                    sb.AppendLine($"  - {feat.Name} (ID: {feat.Id})");
                }
            }

            sb.AppendLine(); // Empty line between levels
        }

        Token().SetBindValue(View.LevelupInfo, sb.ToString());
    }

    private void HandleRedoLastLevel()
    {
        if (_player.LoginCreature == null)
        {
            _player.SendServerMessage("No character available.");
            return;
        }

        RedoLevels(1);
    }

    private void HandleRedoLast2Levels()
    {
        if (_player.LoginCreature == null)
        {
            _player.SendServerMessage("No character available.");
            return;
        }

        RedoLevels(2);
    }

    private void RedoLevels(int levelCount)
    {
        if (_player.LoginCreature == null) return;

        const int MAX_LEVEL = 30;
        const int MAX_LEVEL_XP = 435000; // XP required for level 30

        int currentLevel = _player.LoginCreature.Level;
        int currentXp = _player.LoginCreature.Xp;

        // Validate level count
        if (levelCount < 1 || levelCount > 2)
        {
            _player.SendServerMessage("Invalid level count. Must be 1 or 2.");
            return;
        }

        // Check if character has enough levels
        if (currentLevel <= levelCount)
        {
            _player.SendServerMessage($"You need to be at least level {levelCount + 1} to redo {levelCount} level(s).");
            return;
        }

        // Calculate XP thresholds
        int currentLevelMinXp = currentLevel * (currentLevel - 1) * 500;
        int nextLevelMinXp = (currentLevel + 1) * currentLevel * 500;

        // Check if player has enough XP to reach the next level (they're already leveling up)
        // Exception: Max level characters can have any amount of XP
        if (currentLevel < MAX_LEVEL && currentXp >= nextLevelMinXp)
        {
            _player.SendServerMessage("You cannot use this feature while you have enough experience to level up. Please finish leveling up first.", ColorConstants.Orange);
            return;
        }

        // DISABLE BUTTONS IMMEDIATELY to prevent spam clicking
        Token().SetBindValue(View.RedoButtonsEnabled, false);

        // Calculate target level and XP
        int targetLevel = currentLevel - levelCount;

        // Check for Pale Master or Dragon Disciple restrictions
        int? minAllowedLevel = CheckPrestigeClassRestrictions();
        if (minAllowedLevel.HasValue && targetLevel < minAllowedLevel.Value)
        {
            Token().SetBindValue(View.RedoButtonsEnabled, true); // Re-enable buttons since we're not proceeding
            _player.SendServerMessage($"You cannot delevel below level {minAllowedLevel.Value} due to having 10+ levels of Pale Master or Dragon Disciple. These classes have roleplay ramifications that prevent further deleveling.", ColorConstants.Orange);
            return;
        }

        int targetXp = targetLevel * (targetLevel - 1) * 500;
        int xpToRemove = currentXp - targetXp;

        // Remove XP
        NWN.Core.NWScript.SetXP((uint)_player.LoginCreature, targetXp);
        _player.SendServerMessage($"Removed {xpToRemove:N0} XP. You are now level {targetLevel}.", ColorConstants.Cyan);

        // Check and reduce languages if they've lost language slots
        CheckAndReduceLanguages();

        // Wait a moment for the game to process the level change, then return XP
        NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromMilliseconds(500));

            // Give XP back (same amount that was removed)
            NWN.Core.NWScript.SetXP((uint)_player.LoginCreature, currentXp);
            _player.SendServerMessage($"Returned {xpToRemove:N0} XP. You can now relevel your last {levelCount} level(s)!", ColorConstants.Green);

            // Refresh the display
            UpdateLevelupInfo();

            // Wait 6 seconds total before re-enabling buttons (500ms already passed, so wait 5500ms more)
            await NwTask.Delay(TimeSpan.FromMilliseconds(5500));

            // Re-enable buttons
            Token().SetBindValue(View.RedoButtonsEnabled, true);
        });
    }

    private void OpenAutoRebuildModal()
    {
        // Prevent opening if modal already exists
        if (_autoRebuildModalToken.HasValue)
            return;

        NuiWindow modal = View.BuildAutoRebuildModal();
        if (_player.TryCreateNuiWindow(modal, out NuiWindowToken modalToken))
        {
            _autoRebuildModalToken = modalToken;
            modalToken.SetBindValue(View.AutoRebuildLevel, "");
            _autoRebuildModalToken.Value.OnNuiEvent += HandleAutoRebuildModalEvent;
        }
    }

    private void HandleAutoRebuildModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_auto_rebuild_confirm":
                HandleAutoRebuildConfirm();
                break;

            case "btn_auto_rebuild_cancel":
                CloseAutoRebuildModal();
                break;
        }
    }

    private void HandleAutoRebuildConfirm()
    {
        if (_player.LoginCreature == null)
        {
            _player.SendServerMessage("No character available.");
            return;
        }

        if (!_autoRebuildModalToken.HasValue)
            return;

        // Get the target level input
        string targetLevelStr = _autoRebuildModalToken.Value.GetBindValue(View.AutoRebuildLevel);

        if (!int.TryParse(targetLevelStr, out int targetLevel))
        {
            _player.SendServerMessage("Invalid level. Please enter a number between 1 and 27.", ColorConstants.Red);
            return;
        }

        int currentLevel = _player.LoginCreature.Level;

        // Validate target level
        if (targetLevel < 1 || targetLevel > 27)
        {
            _player.SendServerMessage("Target level must be between 1 and 27.", ColorConstants.Red);
            return;
        }

        if (targetLevel >= currentLevel)
        {
            _player.SendServerMessage("Target level must be lower than your current level.", ColorConstants.Red);
            return;
        }

        // Check for Pale Master or Dragon Disciple restrictions
        int? minAllowedLevel = CheckPrestigeClassRestrictions();
        if (minAllowedLevel.HasValue && targetLevel < minAllowedLevel.Value)
        {
            _player.SendServerMessage($"You cannot delevel below level {minAllowedLevel.Value} due to having 10+ levels of Pale Master or Dragon Disciple. These classes have roleplay ramifications that prevent further deleveling.", ColorConstants.Orange);
            return;
        }

        // Find PC Key
        NwItem? pcKey = null;
        foreach (NwItem item in _player.LoginCreature.Inventory.Items)
        {
            if (item.Tag == "ds_pckey")
            {
                pcKey = item;
                break;
            }
        }

        if (pcKey == null)
        {
            _player.SendServerMessage("PC Key (ds_pckey) not found in your inventory!", ColorConstants.Red);
            return;
        }

        // Check if they've used this feature within the last 6 months
        int lastRebuildTimestamp = NWN.Core.NWScript.GetLocalInt(pcKey, "last_auto_rebuild");
        int currentTimestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int sixMonthsInSeconds = 60 * 60 * 24 * 30 * 6; // ~6 months

        if (lastRebuildTimestamp > 0)
        {
            int timeSinceLastRebuild = currentTimestamp - lastRebuildTimestamp;
            if (timeSinceLastRebuild < sixMonthsInSeconds)
            {
                // Calculate remaining time
                int remainingSeconds = sixMonthsInSeconds - timeSinceLastRebuild;
                int remainingDays = remainingSeconds / (60 * 60 * 24);
                _player.SendServerMessage($"You must wait {remainingDays} more days before using Auto-Rebuild again.", ColorConstants.Orange);
                return;
            }
        }

        // Disable the redo buttons to prevent any interaction during the rebuild
        Token().SetBindValue(View.RedoButtonsEnabled, false);

        // Check for Heritage Feat and remove if necessary
        CheckAndRemoveHeritageFeat(targetLevel, currentLevel);

        // Close the modal
        CloseAutoRebuildModal();

        // Perform the auto-rebuild
        int currentXp = _player.LoginCreature.Xp;
        int targetXp = targetLevel * (targetLevel - 1) * 500;
        int xpToRemove = currentXp - targetXp;

        // Remove XP to target level
        NWN.Core.NWScript.SetXP((uint)_player.LoginCreature, targetXp);
        _player.SendServerMessage($"Auto-Rebuild: Removed {xpToRemove:N0} XP. You are now level {targetLevel}.", ColorConstants.Cyan);

        // Check and reduce languages if they've lost language slots
        CheckAndReduceLanguages();

        // Wait for the game to process
        NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromMilliseconds(500));

            // Give XP back
            NWN.Core.NWScript.SetXP((uint)_player.LoginCreature, currentXp);
            _player.SendServerMessage($"Returned {xpToRemove:N0} XP. You can now relevel your character!", ColorConstants.Green);

            // Update the PC Key with the current timestamp
            NWN.Core.NWScript.SetLocalInt(pcKey, "last_auto_rebuild", currentTimestamp);
            _player.SendServerMessage("Auto-Rebuild timestamp recorded. You can use this again in 6 months.", ColorConstants.Cyan);

            // Refresh the display
            UpdateLevelupInfo();

            // Wait 6 seconds total before re-enabling buttons
            await NwTask.Delay(TimeSpan.FromMilliseconds(5500));

            // Re-enable buttons
            Token().SetBindValue(View.RedoButtonsEnabled, true);
        });
    }

    private void CloseAutoRebuildModal()
    {
        if (_autoRebuildModalToken.HasValue)
        {
            _autoRebuildModalToken.Value.OnNuiEvent -= HandleAutoRebuildModalEvent;
            try
            {
                _autoRebuildModalToken.Value.Close();
            }
            catch
            {
                // ignore
            }
            _autoRebuildModalToken = null;
        }
    }

    private void CheckAndRemoveHeritageFeat(int targetLevel, int currentLevel)
    {
        if (_player.LoginCreature == null) return;

        // Check for Heritage Feat (feat 1238)
        NwFeat? heritageFeat = NwFeat.FromFeatId(1238);
        bool hasHeritageFeat = heritageFeat != null && _player.LoginCreature.KnowsFeat(heritageFeat);

        if (!hasHeritageFeat) return;

        // Find which level they took the heritage feat
        int heritageFeatLevel = -1;
        for (int level = 1; level <= currentLevel; level++)
        {
            CreatureLevelInfo levelInfo = _player.LoginCreature.GetLevelStats(level);
            if (levelInfo.Feats.Any(f => f.Id == 1238))
            {
                heritageFeatLevel = level;
                break;
            }
        }

        // If heritage feat was taken at a level higher than target, remove heritage bonuses
        if (heritageFeatLevel > targetLevel)
        {
            int playerRace = ResolvePlayerRace();

            // Remove heritage abilities if the race is supported
            if (ManagedRaces.RaceHeritageAbilities.ContainsKey(playerRace))
            {
                ManagedRaces.RaceHeritageAbilities[playerRace].RemoveStats(_player);
                _player.SendServerMessage("Removed heritage abilities.", ColorConstants.Green);
            }

            // Remove the heritage feat
            _player.LoginCreature.RemoveFeat(heritageFeat!, true);

            // Delete heritage_setup variable from PC Key
            uint pcKey = NWN.Core.NWScript.GetItemPossessedBy(_player.LoginCreature, "ds_pckey");
            if (NWN.Core.NWScript.GetIsObjectValid(pcKey) == NWN.Core.NWScript.TRUE)
            {
                NWN.Core.NWScript.DeleteLocalInt(pcKey, "heritage_setup");
                _player.SendServerMessage("Removed heritage_setup variable from PC Key.", ColorConstants.Green);
            }

            _player.SendServerMessage("Removed heritage feat.", ColorConstants.Green);
        }
    }

    private int ResolvePlayerRace() =>
        _player.LoginCreature?.SubRace.ToLower() switch
        {
            "aasimar" => (int)ManagedRaces.RacialType.Aasimar,
            "tiefling" => (int)ManagedRaces.RacialType.Tiefling,
            "feytouched" => (int)ManagedRaces.RacialType.Feytouched,
            "feyri" => (int)ManagedRaces.RacialType.Feyri,
            "air genasi" => (int)ManagedRaces.RacialType.AirGenasi,
            "earth genasi" => (int)ManagedRaces.RacialType.EarthGenasi,
            "fire genasi" => (int)ManagedRaces.RacialType.FireGenasi,
            "water genasi" => (int)ManagedRaces.RacialType.WaterGenasi,
            "avariel" => (int)ManagedRaces.RacialType.Avariel,
            "lizardfolk" => (int)ManagedRaces.RacialType.Lizardfolk,
            "half dragon" => (int)ManagedRaces.RacialType.Halfdragon,
            "dragon" => (int)ManagedRaces.RacialType.Halfdragon,
            "centaur" => (int)ManagedRaces.RacialType.Centaur,
            "aquatic elf" => (int)ManagedRaces.RacialType.AquaticElf,
            "elfling" => (int)ManagedRaces.RacialType.Elfling,
            "shadovar" => (int)ManagedRaces.RacialType.Shadovar,
            "drow" => (int)ManagedRaces.RacialType.Drow,
            _ => NWN.Core.NWScript.GetRacialType(_player.LoginCreature)
        };

    private int? CheckPrestigeClassRestrictions()
    {
        if (_player.LoginCreature == null) return null;

        // Class type constants from NWN
        const int CLASS_TYPE_PALEMASTER = 34;
        const int CLASS_TYPE_DRAGON_DISCIPLE = 37;

        int totalLevels = _player.LoginCreature.Level;
        int paleMasterLevels = 0;
        int dragonDiscipleLevels = 0;
        int? paleMasterLevel10At = null;
        int? dragonDiscipleLevel10At = null;

        // Count levels in each prestige class and find when they reached level 10
        for (int level = 1; level <= totalLevels; level++)
        {
            CreatureLevelInfo levelInfo = _player.LoginCreature.GetLevelStats(level);
            int classType = (int)levelInfo.ClassInfo.Class.ClassType;

            if (classType == CLASS_TYPE_PALEMASTER)
            {
                paleMasterLevels++;
                if (paleMasterLevels == 10)
                {
                    paleMasterLevel10At = level;
                }
            }
            else if (classType == CLASS_TYPE_DRAGON_DISCIPLE)
            {
                dragonDiscipleLevels++;
                if (dragonDiscipleLevels == 10)
                {
                    dragonDiscipleLevel10At = level;
                }
            }
        }

        // If they have 10+ levels in either class, return the level at which they reached 10
        if (paleMasterLevels >= 10 && paleMasterLevel10At.HasValue)
        {
            return paleMasterLevel10At.Value;
        }

        if (dragonDiscipleLevels >= 10 && dragonDiscipleLevel10At.HasValue)
        {
            return dragonDiscipleLevel10At.Value;
        }

        return null; // No restrictions
    }

    public override void Close()
    {
        try
        {
            _token.Close();
        }
        catch
        {
            // ignore
        }
    }

    private void CheckAndReduceLanguages()
    {
        Log.Info($"[LANG] CheckAndReduceLanguages called for {_player.LoginCreature?.Name ?? "null"}");

        if (_player.LoginCreature == null)
        {
            Log.Info($"[LANG] Login creature is null, returning");
            return;
        }

        // Find PC Key
        uint pcKeyId = NWN.Core.NWScript.GetItemPossessedBy(_player.LoginCreature, "ds_pckey");
        bool isValid = NWN.Core.NWScript.GetIsObjectValid(pcKeyId) == NWN.Core.NWScript.TRUE;
        Log.Info($"[LANG] PC Key ID: {pcKeyId}, IsValid: {isValid}");

        if (!isValid) return;

        // Get current chosen languages
        string chosenStr = NWN.Core.NWScript.GetLocalString(pcKeyId, "LANGUAGES_CHOSEN");
        Log.Info($"[LANG] LANGUAGES_CHOSEN string: '{chosenStr}'");

        if (string.IsNullOrEmpty(chosenStr))
        {
            Log.Info($"[LANG] LANGUAGES_CHOSEN is null or empty, returning");
            return;
        }

        List<string> chosenLanguages = chosenStr.Split('|').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        Log.Info($"[LANG] Parsed chosen languages count: {chosenLanguages.Count}, languages: {string.Join(", ", chosenLanguages)}");

        // If no languages chosen, nothing to do
        if (chosenLanguages.Count == 0)
        {
            Log.Info($"[LANG] No languages in list after parsing, returning");
            return;
        }

        // Calculate current max language count
        int currentMaxLanguages = CalculateMaxLanguagesForCharacter();
        Log.Info($"[LANG] Current max languages calculated: {currentMaxLanguages}");

        // Get the stored total from when they last saved
        int previousTotal = NWN.Core.NWScript.GetLocalInt(pcKeyId, "LANGUAGES_TOTAL");
        Log.Info($"[LANG] LANGUAGES_TOTAL from PC Key: {previousTotal}");

        // If LANGUAGES_TOTAL doesn't exist (legacy character), use the current chosen count as the previous total
        if (previousTotal == 0)
        {
            previousTotal = chosenLanguages.Count;
            Log.Info($"[LANG] LANGUAGES_TOTAL was 0 (legacy character), setting to {previousTotal}");
            // Save it for future reference
            NWN.Core.NWScript.SetLocalInt(pcKeyId, "LANGUAGES_TOTAL", previousTotal);
        }

        Log.Info($"[LANG] Comparison - Current Max: {currentMaxLanguages}, Previous Total: {previousTotal}");

        // If current max is less than previous total, they've lost language slots
        if (currentMaxLanguages < previousTotal)
        {
            Log.Info($"[LANG] Player has LOST language slots! Processing removal...");

            // Calculate how many languages need to be removed
            int languagesToRemove = previousTotal - currentMaxLanguages;
            int actualRemoveCount = Math.Min(languagesToRemove, chosenLanguages.Count);

            Log.Info($"[LANG] Languages to remove: {languagesToRemove}, Actual remove count: {actualRemoveCount}");

            if (actualRemoveCount > 0)
            {
                // Save the full list to LANGUAGES_SAVED before modifying
                NWN.Core.NWScript.SetLocalString(pcKeyId, "LANGUAGES_SAVED", chosenStr);
                Log.Info($"[LANG] Saved full list to LANGUAGES_SAVED: '{chosenStr}'");

                // Remove languages from the end (last chosen)
                List<string> removedLanguages = new();
                for (int i = 0; i < actualRemoveCount; i++)
                {
                    int lastIndex = chosenLanguages.Count - 1;
                    string removedLang = chosenLanguages[lastIndex];
                    removedLanguages.Add(removedLang);
                    chosenLanguages.RemoveAt(lastIndex);
                    Log.Info($"[LANG] Removed language #{i + 1}: '{removedLang}'");
                }

                // Save the updated chosen languages
                string updatedChosenStr = string.Join("|", chosenLanguages);
                NWN.Core.NWScript.SetLocalString(pcKeyId, "LANGUAGES_CHOSEN", updatedChosenStr);
                Log.Info($"[LANG] Updated LANGUAGES_CHOSEN to: '{updatedChosenStr}'");

                // Notify the player
                string removedList = string.Join(", ", removedLanguages);
                _player.SendServerMessage($"You have lost access to one or more languages: {removedList}", ColorConstants.Orange);
                Log.Info($"[LANG] Notified player of removed languages: {removedList}");
            }
            else
            {
                Log.Info($"[LANG] actualRemoveCount was 0, no languages removed");
            }
        }
        else
        {
            Log.Info($"[LANG] No language slots lost (current {currentMaxLanguages} >= previous {previousTotal}), no action taken");
        }
    }

    private int CalculateMaxLanguagesForCharacter()
    {
        if (_player.LoginCreature == null)
        {
            Log.Info($"[LANG-CALC] Login creature is null, returning 0");
            return 0;
        }

        // Get base Intelligence modifier (without gear)
        int baseInt = _player.LoginCreature.GetRawAbilityScore(Ability.Intelligence);
        int intModifier = (baseInt - 10) / 2;
        Log.Info($"[LANG-CALC] Base INT: {baseInt}, INT Modifier: {intModifier}");

        // Start with INT modifier only
        int totalLanguages = Math.Max(0, intModifier);
        Log.Info($"[LANG-CALC] Starting total: {totalLanguages}");

        // Add bonus from Lore skill (1 bonus per 10 base ranks)
        int loreRank = NWN.Core.NWScript.GetSkillRank(NWN.Core.NWScript.SKILL_LORE, (uint)_player.LoginCreature, NWN.Core.NWScript.TRUE);
        int loreBonus = loreRank / 10;
        totalLanguages += loreBonus;
        Log.Info($"[LANG-CALC] Lore Rank: {loreRank}, Lore Bonus: {loreBonus}, Running Total: {totalLanguages}");

        // Add bonus from Epic Skill Focus: Lore feat (feat 492)
        bool hasEpicLore = NWN.Core.NWScript.GetHasFeat(492, (uint)_player.LoginCreature) == NWN.Core.NWScript.TRUE;
        if (hasEpicLore)
        {
            totalLanguages += 1;
            Log.Info($"[LANG-CALC] Has Epic Skill Focus: Lore, Running Total: {totalLanguages}");
        }
        else
        {
            Log.Info($"[LANG-CALC] Does NOT have Epic Skill Focus: Lore");
        }

        // Add bonus from Bard class (5+ levels = 1 bonus)
        int bardLevel = GetClassLevelForCharacter(35); // Bard is class 35
        Log.Info($"[LANG-CALC] Bard Level: {bardLevel}");
        if (bardLevel >= 5)
        {
            totalLanguages += 1;
            Log.Info($"[LANG-CALC] Bard bonus applied, Running Total: {totalLanguages}");
        }


        Log.Info($"[LANG-CALC] Final calculated max languages: {totalLanguages}");
        return totalLanguages;
    }

    private int GetClassLevelForCharacter(int classType)
    {
        if (_player.LoginCreature == null) return 0;

        int totalLevels = _player.LoginCreature.Level;
        int classLevels = 0;

        for (int level = 1; level <= totalLevels; level++)
        {
            CreatureLevelInfo levelInfo = _player.LoginCreature.GetLevelStats(level);
            if ((int)levelInfo.ClassInfo.Class.ClassType == classType)
            {
                classLevels++;
            }
        }

        return classLevels;
    }
}

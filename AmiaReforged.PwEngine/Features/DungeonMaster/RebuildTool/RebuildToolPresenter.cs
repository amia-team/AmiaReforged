using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using System.Text;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.RebuildTool;

public sealed class RebuildToolPresenter : ScryPresenter<RebuildToolView>
{
    public override RebuildToolView View { get; }

    private readonly NwPlayer _player;
    private readonly RebuildToolModel _model;
    private readonly IRebuildRepository _repository;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private NuiWindowToken? _rebuildModalToken;
    private NuiWindowToken? _raceOptionsModalToken;
    private NuiWindowToken? _fullRebuildModalToken;
    private NuiWindowToken? _findRebuildModalToken;
    private int? _currentRebuildId;
    private bool _isViewingAllFeats = false; // Track if user is viewing "View All Feats"

    public override NuiWindowToken Token() => _token;

    public RebuildToolPresenter(RebuildToolView view, NwPlayer player, IRebuildRepository repository)
    {
        View = view;
        _player = player;
        _repository = repository;
        _model = new RebuildToolModel(player, repository);

        // Subscribe to model events
        _model.OnCharacterSelected += OnCharacterSelected;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 630f, 780f),
            Resizable = false
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
        Token().SetBindValue(View.CharacterSelected, false);
        Token().SetBindValue(View.CharacterInfo, "No character selected");
        Token().SetBindValue(View.LevelupInfo, "");
        Token().SetBindValue(View.FeatId, "");
        Token().SetBindValue(View.Level, "1");
        Token().SetBindValue(View.LevelFilter, 0); // 0 = All Levels

        // Watch for level filter changes
        Token().SetBindWatch(View.LevelFilter, true);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType == NuiEventType.Watch && ev.ElementId == View.LevelFilter.Key)
        {
            // Level filter changed, update the display
            _isViewingAllFeats = false; // User switched to level view
            UpdateLevelupInfo();
            return;
        }

        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case var id when id == View.SelectCharacterButton.Id:
                _model.EnterTargetingMode();
                break;

            case var id when id == View.AddFeatButton.Id:
                HandleAddFeat();
                break;

            case var id when id == View.RemoveFeatButton.Id:
                HandleRemoveFeat();
                break;

            case var id when id == View.InitiateRebuildButton.Id:
                OpenRebuildModal();
                break;

            case var id when id == View.ViewAllFeatsButton.Id:
                _isViewingAllFeats = true; // User is now viewing all feats
                DisplayAllFeats();
                break;

            case var id when id == View.RaceOptionsButton.Id:
                OpenRaceOptionsModal();
                break;
        }
    }

    private void OnCharacterSelected(object? sender, EventArgs e)
    {
        if (_model.SelectedCharacter == null) return;

        Token().SetBindValue(View.CharacterSelected, true);
        Token().SetBindValue(View.CharacterInfo, $"Selected: {_model.SelectedCharacter.Name} (Level {_model.SelectedCharacter.Level})");

        UpdateLevelupInfo();
    }

    private void UpdateLevelupInfo()
    {
        if (_model.SelectedCharacter == null) return;

        StringBuilder sb = new();
        int totalLevels = _model.SelectedCharacter.Level;
        int levelFilter = Token().GetBindValue(View.LevelFilter);

        // Determine which levels to display
        int startLevel = levelFilter == 0 ? 1 : levelFilter;
        int endLevel = levelFilter == 0 ? totalLevels : levelFilter;

        if (levelFilter == 0)
        {
            sb.AppendLine($"=== Level-up Information for {_model.SelectedCharacter.Name} ===\n");
        }
        else
        {
            sb.AppendLine($"=== Level {levelFilter} Information for {_model.SelectedCharacter.Name} ===\n");
        }

        for (int level = startLevel; level <= endLevel; level++)
        {
            CreatureLevelInfo levelInfo = _model.SelectedCharacter.GetLevelStats(level);

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

    private void DisplayAllFeats()
    {
        if (_model.SelectedCharacter == null) return;

        StringBuilder sb = new();
        int totalLevels = _model.SelectedCharacter.Level;

        sb.AppendLine($"=== All Feats for {_model.SelectedCharacter.Name} ===\n");

        // Collect all feats from all levels
        Dictionary<NwFeat, List<int>> featsWithLevels = new();

        for (int level = 1; level <= totalLevels; level++)
        {
            CreatureLevelInfo levelInfo = _model.SelectedCharacter.GetLevelStats(level);

            foreach (var feat in levelInfo.Feats)
            {
                if (!featsWithLevels.ContainsKey(feat))
                {
                    featsWithLevels[feat] = new List<int>();
                }
                featsWithLevels[feat].Add(level);
            }
        }

        // Check for unassigned feats (feats the character has but weren't gained at any level)
        List<NwFeat> unassignedFeats = new();
        foreach (var feat in _model.SelectedCharacter.Feats)
        {
            if (!featsWithLevels.ContainsKey(feat))
            {
                unassignedFeats.Add(feat);
            }
        }

        // Sort feats alphabetically by name
        var sortedFeats = featsWithLevels.OrderBy(kvp => kvp.Key.Name.ToString());
        var sortedUnassignedFeats = unassignedFeats.OrderBy(f => f.Name.ToString());

        int totalFeatCount = featsWithLevels.Count + unassignedFeats.Count;
        sb.AppendLine($"Total Feats: {totalFeatCount}");
        if (unassignedFeats.Count > 0)
        {
            sb.AppendLine($"(Including {unassignedFeats.Count} unassigned feat(s))");
        }
        sb.AppendLine();

        // Display feats with assigned levels
        foreach (var kvp in sortedFeats)
        {
            NwFeat feat = kvp.Key;
            List<int> levels = kvp.Value;

            // Show feat name, ID, and the level(s) it was acquired at
            string levelStr = levels.Count > 1
                ? $"Levels {string.Join(", ", levels)}"
                : $"Level {levels[0]}";

            sb.AppendLine($"- {feat.Name} (ID: {feat.Id}) - {levelStr}");
        }

        // Display unassigned feats
        if (unassignedFeats.Count > 0)
        {
            sb.AppendLine("\n--- UNASSIGNED FEATS ---");
            foreach (var feat in sortedUnassignedFeats)
            {
                sb.AppendLine($"- {feat.Name} (ID: {feat.Id}) - (UNASSIGNED)");
            }
        }

        Token().SetBindValue(View.LevelupInfo, sb.ToString());
    }

    private void HandleAddFeat()
    {
        string featIdStr = Token().GetBindValue(View.FeatId);
        string levelStr = Token().GetBindValue(View.Level);

        if (!int.TryParse(featIdStr, out int featId))
        {
            _player.SendServerMessage("Invalid Feat ID. Please enter a valid number.");
            return;
        }

        if (!int.TryParse(levelStr, out int level))
        {
            _player.SendServerMessage("Invalid Level. Please enter a valid number.");
            return;
        }

        _model.AddFeatToCharacter(featId, level);

        // Refresh the current view (all feats or level-up info)
        if (_isViewingAllFeats)
        {
            DisplayAllFeats();
        }
        else
        {
            UpdateLevelupInfo();
        }
    }

    private async void HandleRemoveFeat()
    {
        string featIdStr = Token().GetBindValue(View.FeatId);

        if (!int.TryParse(featIdStr, out int featId))
        {
            _player.SendServerMessage("Invalid Feat ID. Please enter a valid number.");
            return;
        }

        _model.RemoveFeatFromCharacter(featId);

        // Small delay to allow the game engine to update
        await NwTask.Delay(TimeSpan.FromMilliseconds(100));

        // Refresh the current view (all feats or level-up info)
        if (_isViewingAllFeats)
        {
            DisplayAllFeats();
        }
        else
        {
            UpdateLevelupInfo();
        }
    }

    private void OpenRebuildModal()
    {
        // Prevent opening if modal already exists
        if (_rebuildModalToken.HasValue)
            return;

        if (_model.SelectedCharacter == null)
        {
            _player.SendServerMessage("Please select a character first.");
            return;
        }

        NuiWindow modal = View.BuildRebuildModal();
        if (_player.TryCreateNuiWindow(modal, out NuiWindowToken modalToken))
        {
            _rebuildModalToken = modalToken;
            modalToken.SetBindValue(View.RebuildLevel, "1");
            modalToken.SetBindValue(View.ReturnToLevel, "");
            _rebuildModalToken.Value.OnNuiEvent += HandleRebuildModalEvent;
        }
    }

    private void HandleRebuildModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_full_rebuild":
                OpenFullRebuildModal();
                break;

            case "btn_partial_rebuild":
                HandlePartialRebuild();
                // Keep modal open so DM can return XP if needed
                break;

            case "btn_return_all_xp":
                HandleReturnAllXP();
                // Keep modal open in case DM wants to do more
                break;

            case "btn_rebuild_cancel":
                CloseRebuildModal();
                break;
        }
    }

    private void HandlePartialRebuild()
    {
        if (_model.SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected.");
            return;
        }

        string levelStr = _rebuildModalToken!.Value.GetBindValue(View.RebuildLevel);

        if (!int.TryParse(levelStr, out int targetLevel))
        {
            _player.SendServerMessage("Invalid level. Please enter a valid number between 1 and 29.");
            return;
        }

        // Find the player who owns this character
        NwPlayer? targetPlayer = _model.SelectedCharacter.ControllingPlayer;
        if (targetPlayer == null)
        {
            _player.SendServerMessage("Could not find the player controlling this character.");
            return;
        }

        _model.PartialRebuild(targetLevel, targetPlayer);

        // Refresh the display after rebuild
        UpdateLevelupInfo();
    }

    private void HandleReturnAllXP()
    {
        if (_model.SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected.");
            return;
        }

        // Find the player who owns this character
        NwPlayer? targetPlayer = _model.SelectedCharacter.ControllingPlayer;
        if (targetPlayer == null)
        {
            _player.SendServerMessage("Could not find the player controlling this character.");
            return;
        }

        // Check if a return to level was specified
        string returnToLevelStr = _rebuildModalToken!.Value.GetBindValue(View.ReturnToLevel);
        int? returnToLevel = null;

        if (!string.IsNullOrWhiteSpace(returnToLevelStr))
        {
            if (int.TryParse(returnToLevelStr, out int parsedLevel))
            {
                returnToLevel = parsedLevel;
            }
            else
            {
                _player.SendServerMessage("Invalid return to level. Please enter a valid number between 2 and 30, or leave empty to return all XP.");
                return;
            }
        }

        _model.ReturnAllXP(targetPlayer, returnToLevel);

        // Refresh the display after XP return
        UpdateLevelupInfo();
    }

    private void CloseRebuildModal()
    {
        if (_rebuildModalToken.HasValue)
        {
            _rebuildModalToken.Value.OnNuiEvent -= HandleRebuildModalEvent;
            try
            {
                _rebuildModalToken.Value.Close();
            }
            catch
            {
                // ignore
            }
            _rebuildModalToken = null;
        }
    }

    private void OpenRaceOptionsModal()
    {
        // Prevent opening if modal already exists
        if (_raceOptionsModalToken.HasValue)
            return;

        if (_model.SelectedCharacter == null)
        {
            _player.SendServerMessage("Please select a character first.");
            return;
        }

        // Load all races from racialtypes.2da
        List<(int id, string label, int nameStrRef)> races = _model.LoadRacialTypes();

        // Convert to NuiComboEntry list
        List<NuiComboEntry> raceEntries = races.Select(r => new NuiComboEntry(r.label, r.id)).ToList();

        NuiWindow modal = View.BuildRaceOptionsModal(raceEntries);
        if (_player.TryCreateNuiWindow(modal, out NuiWindowToken modalToken))
        {
            _raceOptionsModalToken = modalToken;

            // Set initial values
            string currentRaceInfo = _model.GetCurrentRaceInfo();
            modalToken.SetBindValue(View.CurrentRaceInfo, currentRaceInfo);
            modalToken.SetBindValue(View.SelectedRaceIndex, 0);
            modalToken.SetBindValue(View.SubRaceInput, "");

            _raceOptionsModalToken.Value.OnNuiEvent += HandleRaceOptionsModalEvent;
        }
    }

    private void HandleRaceOptionsModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_race_save":
                HandleRaceSave();
                break;

            case "btn_clear_subrace":
                HandleClearSubrace();
                break;

            case "btn_race_cancel":
                CloseRaceOptionsModal();
                break;
        }
    }

    private void HandleRaceSave()
    {
        if (_model.SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected.");
            return;
        }

        if (!_raceOptionsModalToken.HasValue)
            return;

        // Get the selected race index
        int selectedRaceIndex = _raceOptionsModalToken.Value.GetBindValue(View.SelectedRaceIndex);

        // Get the optional subrace input
        string subraceInput = _raceOptionsModalToken.Value.GetBindValue(View.SubRaceInput);

        // Find the player who owns this character
        NwPlayer? targetPlayer = _model.SelectedCharacter.ControllingPlayer;
        if (targetPlayer == null)
        {
            _player.SendServerMessage("Could not find the player controlling this character.");
            return;
        }

        // Change the character's race
        _model.ChangeCharacterRace(selectedRaceIndex, targetPlayer, subraceInput);

        // Update the current race info display
        string newRaceInfo = _model.GetCurrentRaceInfo();
        _raceOptionsModalToken.Value.SetBindValue(View.CurrentRaceInfo, newRaceInfo);
    }

    private void HandleClearSubrace()
    {
        if (_model.SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected.");
            return;
        }

        // Find the player who owns this character
        NwPlayer? targetPlayer = _model.SelectedCharacter.ControllingPlayer;
        if (targetPlayer == null)
        {
            _player.SendServerMessage("Could not find the player controlling this character.");
            return;
        }

        // Clear the subrace
        _model.ClearCharacterSubrace(targetPlayer);
    }

    private void CloseRaceOptionsModal()
    {
        if (_raceOptionsModalToken.HasValue)
        {
            _raceOptionsModalToken.Value.OnNuiEvent -= HandleRaceOptionsModalEvent;
            try
            {
                _raceOptionsModalToken.Value.Close();
            }
            catch
            {
                // ignore
            }
            _raceOptionsModalToken = null;
        }
    }

    private void OpenFullRebuildModal()
    {
        // Prevent opening if modal already exists
        if (_fullRebuildModalToken.HasValue)
            return;

        if (_model.SelectedCharacter == null)
        {
            _player.SendServerMessage("Please select a character first.");
            return;
        }

        NuiWindow modal = View.BuildFullRebuildModal();
        if (_player.TryCreateNuiWindow(modal, out NuiWindowToken modalToken))
        {
            _fullRebuildModalToken = modalToken;
            modalToken.SetBindValue(View.FullRebuildReturnLevel, "");
            _fullRebuildModalToken.Value.OnNuiEvent += HandleFullRebuildModalEvent;
        }
    }

    private void HandleFullRebuildModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_start_full_rebuild":
                HandleStartFullRebuild();
                break;

            case "btn_return_inventory":
                EnterReturnInventoryTargetMode();
                break;

            case "btn_full_rebuild_return_xp":
                HandleFullRebuildReturnXP();
                break;

            case "btn_finish_full_rebuild":
                HandleFinishFullRebuild();
                break;

            case "btn_find_rebuild":
                HandleFindRebuild();
                break;

            case "btn_full_rebuild_cancel":
                CloseFullRebuildModal();
                break;
        }
    }

    private async void HandleStartFullRebuild()
    {
        if (_model.SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected.");
            return;
        }

        NwPlayer? targetPlayer = _model.SelectedCharacter.ControllingPlayer;
        if (targetPlayer == null)
        {
            _player.SendServerMessage("Could not find the player controlling this character.");
            return;
        }

        _currentRebuildId = await _model.StartFullRebuild(targetPlayer);

        if (_currentRebuildId.HasValue)
        {
            _player.SendServerMessage($"Full rebuild started. Rebuild ID: {_currentRebuildId.Value}", ColorConstants.Green);
        }
    }

    private async void HandleReturnInventory()
    {
        if (!_currentRebuildId.HasValue)
        {
            _player.SendServerMessage("No active rebuild. Use Find Rebuild to load a pending rebuild.");
            return;
        }

        if (_model.SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected.");
            return;
        }

        NwPlayer? targetPlayer = _model.SelectedCharacter.ControllingPlayer;
        if (targetPlayer == null)
        {
            _player.SendServerMessage("Could not find the player controlling this character.");
            return;
        }

        await _model.ReturnInventory(_currentRebuildId.Value, targetPlayer);
    }

    private void EnterReturnInventoryTargetMode()
    {
        if (!_currentRebuildId.HasValue)
        {
            _player.SendServerMessage("No active rebuild. Use Find Rebuild to load a pending rebuild.");
            return;
        }

        _player.SendServerMessage("Select the NEW character that the player created.", ColorConstants.Cyan);

        _player.EnterTargetMode(OnReturnInventoryTargetSelected, new TargetModeSettings
        {
            CursorType = MouseCursor.Action,
            ValidTargets = ObjectTypes.Creature
        });
    }

    private async void OnReturnInventoryTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        if (!_currentRebuildId.HasValue)
        {
            _player.SendServerMessage("No active rebuild.");
            return;
        }

        if (obj.TargetObject is not NwCreature targetCreature)
        {
            _player.SendServerMessage("Invalid target. Please select a player character.");
            return;
        }

        NwPlayer? targetPlayer = targetCreature.ControllingPlayer;
        if (targetPlayer == null)
        {
            _player.SendServerMessage("Selected creature is not a player character.");
            return;
        }

        // Verify PC Key match
        bool keyMatches = await _model.VerifyPCKeyMatch(_currentRebuildId.Value, targetCreature, _player.ControlledCreature);

        if (!keyMatches)
        {
            _player.SendServerMessage("PC Key verification failed! The PC Key in this character's inventory doesn't match the rebuild record.", ColorConstants.Red);
            return;
        }

        // Update the selected character to the new one
        _model.SetSelectedCharacter(targetCreature);

        // Now proceed with returning inventory
        await _model.ReturnInventory(_currentRebuildId.Value, targetPlayer);
    }

    private void HandleFullRebuildReturnXP()
    {
        if (!_currentRebuildId.HasValue)
        {
            _player.SendServerMessage("No active rebuild. Use Find Rebuild to load a pending rebuild.");
            return;
        }

        if (_model.SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected.");
            return;
        }

        NwPlayer? targetPlayer = _model.SelectedCharacter.ControllingPlayer;
        if (targetPlayer == null)
        {
            _player.SendServerMessage("Could not find the player controlling this character.");
            return;
        }

        // Get the optional return to level input
        string returnToLevelStr = _fullRebuildModalToken!.Value.GetBindValue(View.FullRebuildReturnLevel);
        int? returnToLevel = null;

        if (!string.IsNullOrWhiteSpace(returnToLevelStr))
        {
            if (int.TryParse(returnToLevelStr, out int parsedLevel))
            {
                returnToLevel = parsedLevel;
            }
            else
            {
                _player.SendServerMessage("Invalid return to level. Please enter a valid number between 2 and 30, or leave empty to return all XP.");
                return;
            }
        }

        _model.ReturnFullRebuildXP(_currentRebuildId.Value, targetPlayer, returnToLevel);
    }

    private void HandleFinishFullRebuild()
    {
        if (!_currentRebuildId.HasValue)
        {
            _player.SendServerMessage("No active rebuild to finish.");
            return;
        }

        _player.SendServerMessage("Click on the rebuilt character to finalize this rebuild. THIS CANNOT BE UNDONE!", ColorConstants.Yellow);

        _player.EnterTargetMode(OnFinishRebuildTargetSelected, new TargetModeSettings
        {
            CursorType = MouseCursor.Action,
            ValidTargets = ObjectTypes.Creature
        });
    }

    private void OnFinishRebuildTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.TargetObject is not NwCreature creature)
        {
            _player.SendServerMessage("Invalid target. Please select the rebuilt character.");
            return;
        }

        if (!creature.IsPlayerControlled)
        {
            _player.SendServerMessage("Target must be a player character.");
            return;
        }

        if (!_currentRebuildId.HasValue)
        {
            _player.SendServerMessage("No active rebuild to finish.");
            return;
        }

        _model.FinishFullRebuild(_currentRebuildId.Value, creature);
        _currentRebuildId = null;

        CloseFullRebuildModal();
    }

    private void HandleFindRebuild()
    {
        OpenFindRebuildModal();
    }

    private void OpenFindRebuildModal()
    {
        // Prevent opening if modal already exists
        if (_findRebuildModalToken.HasValue)
            return;

        var pendingRebuilds = _model.GetPendingRebuilds().ToList();

        if (!pendingRebuilds.Any())
        {
            _player.SendServerMessage("No pending rebuilds found.");
            return;
        }

        // Create combo entries with index as value (0, 1, 2, etc.)
        List<NuiComboEntry> rebuildEntries = pendingRebuilds
            .Select((r, index) => new NuiComboEntry($"{r.firstName} {r.lastName}", index))
            .ToList();

        NuiWindow modal = View.BuildFindRebuildModal(rebuildEntries);
        if (_player.TryCreateNuiWindow(modal, out NuiWindowToken modalToken))
        {
            _findRebuildModalToken = modalToken;
            modalToken.SetBindValue(View.SelectedPendingRebuild, 0);
            _findRebuildModalToken.Value.OnNuiEvent += HandleFindRebuildModalEvent;
        }
    }

    private void HandleFindRebuildModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_select_pending_rebuild":
                HandleSelectPendingRebuild();
                break;

            case "btn_find_rebuild_cancel":
                CloseFindRebuildModal();
                break;
        }
    }

    private void HandleSelectPendingRebuild()
    {
        if (!_findRebuildModalToken.HasValue)
            return;

        // Get the selected index from the combo box
        int selectedIndex = _findRebuildModalToken.Value.GetBindValue(View.SelectedPendingRebuild);

        // Get the pending rebuilds list again
        var pendingRebuilds = _model.GetPendingRebuilds().ToList();

        // Validate the selected index
        if (selectedIndex < 0 || selectedIndex >= pendingRebuilds.Count)
        {
            _player.SendServerMessage("Invalid rebuild selection.", ColorConstants.Red);
            return;
        }

        // Get the rebuild ID from the selected entry
        var selectedRebuild = pendingRebuilds[selectedIndex];
        _currentRebuildId = selectedRebuild.rebuildId;

        // Load the rebuild (recreates PC Key in DM inventory)
        _model.LoadPendingRebuild(_currentRebuildId.Value);

        _player.SendServerMessage($"Loaded rebuild for: {selectedRebuild.firstName} {selectedRebuild.lastName}", ColorConstants.Green);

        CloseFindRebuildModal();
    }

    private void CloseFindRebuildModal()
    {
        if (_findRebuildModalToken.HasValue)
        {
            _findRebuildModalToken.Value.OnNuiEvent -= HandleFindRebuildModalEvent;
            try
            {
                _findRebuildModalToken.Value.Close();
            }
            catch
            {
                // ignore
            }
            _findRebuildModalToken = null;
        }
    }

    private void CloseFullRebuildModal()
    {
        if (_fullRebuildModalToken.HasValue)
        {
            _fullRebuildModalToken.Value.OnNuiEvent -= HandleFullRebuildModalEvent;
            try
            {
                _fullRebuildModalToken.Value.Close();
            }
            catch
            {
                // ignore
            }
            _fullRebuildModalToken = null;
        }
    }

    public override void Close()
    {
        CloseRebuildModal();
        CloseRaceOptionsModal();
        CloseFullRebuildModal();
        CloseFindRebuildModal();

        try
        {
            _token.Close();
        }
        catch
        {
            // ignore
        }
    }
}


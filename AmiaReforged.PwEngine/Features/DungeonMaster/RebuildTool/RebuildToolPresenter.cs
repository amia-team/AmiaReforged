using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using System.Text;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.RebuildTool;

public sealed class RebuildToolPresenter : ScryPresenter<RebuildToolView>
{
    public override RebuildToolView View { get; }

    private readonly NwPlayer _player;
    private readonly RebuildToolModel _model;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private NuiWindowToken? _rebuildModalToken;

    public override NuiWindowToken Token() => _token;

    public RebuildToolPresenter(RebuildToolView view, NwPlayer player)
    {
        View = view;
        _player = player;
        _model = new RebuildToolModel(player);

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

        // Refresh the display
        UpdateLevelupInfo();
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

        // Refresh the display
        UpdateLevelupInfo();
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
            _rebuildModalToken.Value.OnNuiEvent += HandleRebuildModalEvent;
        }
    }

    private void HandleRebuildModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_full_rebuild":
                _player.SendServerMessage("Full Rebuild selected (functionality to be implemented)");
                CloseRebuildModal();
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

        _model.ReturnAllXP(targetPlayer);

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

    public override void Close()
    {
        CloseRebuildModal();

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


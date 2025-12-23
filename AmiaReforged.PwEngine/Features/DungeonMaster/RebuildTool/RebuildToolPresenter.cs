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
        }
    }

    private void OnCharacterSelected(RebuildToolModel sender, EventArgs e)
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
}


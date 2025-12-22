using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using System.Text;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.BuildChecker;

public sealed class BuildCheckerPresenter : ScryPresenter<BuildCheckerView>
{
    public override BuildCheckerView View { get; }

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

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
        if (_player.LoginCreature == null)
        {
            Token().SetBindValue(View.CharacterInfo, "No character available");
            Token().SetBindValue(View.LevelupInfo, "You must be logged in to view your build information.");
            return;
        }

        Token().SetBindValue(View.CharacterInfo, $"Character: {_player.LoginCreature.Name} (Level {_player.LoginCreature.Level})");
        Token().SetBindValue(View.LevelupInfo, "");
        Token().SetBindValue(View.LevelFilter, 0); // 0 = All Levels

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


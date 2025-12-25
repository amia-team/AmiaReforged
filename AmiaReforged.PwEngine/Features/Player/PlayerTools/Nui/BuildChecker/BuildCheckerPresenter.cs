﻿using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
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
        int targetXp = targetLevel * (targetLevel - 1) * 500;
        int xpToRemove = currentXp - targetXp;

        // Remove XP
        NWN.Core.NWScript.SetXP((uint)_player.LoginCreature, targetXp);
        _player.SendServerMessage($"Removed {xpToRemove:N0} XP. You are now level {targetLevel}.", ColorConstants.Cyan);

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


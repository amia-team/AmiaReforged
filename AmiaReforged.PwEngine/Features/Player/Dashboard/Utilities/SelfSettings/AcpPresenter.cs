using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.SelfSettings;

public sealed class AcpPresenter : ScryPresenter<AcpView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    // Geometry bind to force window position
    private readonly NuiBind<NuiRect> _geometryBind = new("window_geometry");
    private static readonly NuiRect WindowPosition = new(360f, 100f, 370f, 250f);

    public override AcpView View { get; }
    public override NuiWindowToken Token() => _token;

    public AcpPresenter(AcpView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "Phenotype Changer (WIP)")
        {
            Geometry = _geometryBind,
            Resizable = true,
            Closable = true,
            Collapsed = false
        };
    }

    public override void Create()
    {
        if (_window is null)
        {
            _player.SendServerMessage("The ACP window could not be created.", ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        // Force the window position using the bind
        Token().SetBindValue(_geometryBind, WindowPosition);

        NwCreature? creature = _player.LoginCreature;

        // Populate phenotype style options based on ACP scripts
        List<NuiComboEntry> styleOptions = new()
        {
            new NuiComboEntry("Normal (Default)", 0),
            new NuiComboEntry("Large", 2),
            new NuiComboEntry("Old", 9),
            new NuiComboEntry("Fencing Style", 18),
            new NuiComboEntry("Kensei Style", 15),
            new NuiComboEntry("Heavy/Barbarian Style", 17),
            new NuiComboEntry("Assassin Style", 16)
        };

        // Only add Levitation if the character qualifies
        if (creature != null && CanUseLevitation(creature))
        {
            styleOptions.Add(new NuiComboEntry("Levitation", 19));
        }

        // Add remaining styles
        styleOptions.Add(new NuiComboEntry("Demon Blade", 20));
        styleOptions.Add(new NuiComboEntry("Warrior", 21));
        styleOptions.Add(new NuiComboEntry("Martial Arts - Tiger Fang", 30));
        styleOptions.Add(new NuiComboEntry("Martial Arts - Sun Fist", 31));
        styleOptions.Add(new NuiComboEntry("Martial Arts - Dragon Palm", 32));
        styleOptions.Add(new NuiComboEntry("Martial Arts - Bear Claw", 33));

        Token().SetBindValue(View.StyleOptions, styleOptions);

        // Get current phenotype
        if (creature != null)
        {
            int currentPheno = (int)creature.Phenotype;
            // Set selected to the actual phenotype value (NuiCombo Selected uses the Entry Value)
            Token().SetBindValue(View.StyleSelected, currentPheno);
        }
        else
        {
            Token().SetBindValue(View.StyleSelected, 0);
        }
    }

    /// <summary>
    /// Checks if the creature can use the Levitation phenotype.
    /// Requirements: 5+ Sorcerer or Wizard levels, OR 5+ Cleric levels with Travel Domain, OR Drow race.
    /// </summary>
    private bool CanUseLevitation(NwCreature creature)
    {
        // Check Sorcerer levels (5+)
        int sorcLevels = creature.GetClassInfo(ClassType.Sorcerer)?.Level ?? 0;
        if (sorcLevels >= 5) return true;

        // Check Warlock levels (6+)
        int warlockLevels = creature.GetClassInfo(NwClass.FromClassId(57))?.Level ?? 0;
        if (warlockLevels >= 6) return true;

        // Check Wizard levels (5+)
        int wizLevels = creature.GetClassInfo(ClassType.Wizard)?.Level ?? 0;
        if (wizLevels >= 5) return true;

        // Check Cleric levels (5+) with Travel Domain (feat 323 = Travel Domain Power)
        int clericLevels = creature.GetClassInfo(ClassType.Cleric)?.Level ?? 0;
        if (clericLevels >= 5 && NWScript.GetHasFeat(323, creature) == NWScript.TRUE)
        {
            return true;
        }

        // Check Drow race (racial type 33)
        if ((int)creature.Race.RacialType == 33) return true;

        return false;
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_set":
                HandleSetStyle();
                break;
            case "btn_cancel":
                Close();
                break;
        }
    }

    private void HandleSetStyle()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        // NuiCombo Selected binding returns the VALUE from NuiComboEntry, not an index
        int targetPhenotype = Token().GetBindValue(View.StyleSelected);
        int currentPhenotype = (int)creature.Phenotype;

        // Check if already using this style
        if (currentPhenotype == targetPhenotype)
        {
            _player.SendServerMessage("You're already using this fighting style!", ColorConstants.Orange);
            return;
        }

        // Check if phenotype is 2 (too fat)
        if (currentPhenotype == 2)
        {
            _player.SendServerMessage("You're too fat to use a different fighting style!", ColorConstants.Orange);
            return;
        }

        // Check if resetting to normal (phenotype 0)
        if (targetPhenotype == 0)
        {
            ResetFightingStyle(creature);
        }
        else
        {
            SetCustomFightingStyle(creature, targetPhenotype);
        }
    }

    private void ResetFightingStyle(NwCreature creature)
    {
        int currentPhenotype = (int)creature.Phenotype;

        // Valid phenotypes that can be reset: 15-20, 30-33
        if (currentPhenotype == 15 || currentPhenotype == 16 || currentPhenotype == 17 ||
            currentPhenotype == 18 || currentPhenotype == 19 || currentPhenotype == 20 ||
            currentPhenotype == 30 || currentPhenotype == 31 || currentPhenotype == 32 ||
            currentPhenotype == 33)
        {
            creature.Phenotype = (Phenotype)0;
            _player.SendServerMessage("Fighting style reset to normal.", ColorConstants.Cyan);
        }
        else
        {
            _player.SendServerMessage("This may not work for you...", ColorConstants.Orange);
            creature.Phenotype = (Phenotype)0;
        }
    }

    private void SetCustomFightingStyle(NwCreature creature, int style)
    {
        int currentPhenotype = (int)creature.Phenotype;

        // Valid current phenotypes: 0, 15-20, 30-33
        if (currentPhenotype == 0 ||
            currentPhenotype == 15 || currentPhenotype == 16 || currentPhenotype == 17 ||
            currentPhenotype == 18 || currentPhenotype == 19 || currentPhenotype == 20 ||
            currentPhenotype == 30 || currentPhenotype == 31 || currentPhenotype == 32 ||
            currentPhenotype == 33)
        {
            creature.Phenotype = (Phenotype)style;

            // Give appropriate feedback message based on style
            string styleName = style switch
            {
                15 => "Fencing Style",
                16 => "Kensei Style",
                17 => "Barbarian Style",
                18 => "Assassin Style",
                19 => "Arcane Style",
                20 => "Demon Style",
                30 => "Hung Gar Style",
                31 => "Muay Thai Style",
                32 => "Shaolin Style",
                33 => "Shotokan Style",
                _ => "Custom Style"
            };

            _player.SendServerMessage($"{styleName} activated.", ColorConstants.Cyan);
        }
        else
        {
            _player.SendServerMessage("Your phenotype is non-standard / this may not work...", ColorConstants.Orange);
            creature.Phenotype = (Phenotype)style;
        }
    }

    public override void UpdateView()
    {
        // No dynamic updates needed
    }

    public override void Close()
    {
        // Don't call RaiseCloseEvent() here - it causes infinite recursion
        // The WindowDirector handles cleanup when CloseWindow() is called
        _token.Close();
    }
}

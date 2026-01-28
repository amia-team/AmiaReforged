using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.SelfSettings;

public sealed class HurtYourselfPresenter : ScryPresenter<HurtYourselfView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    // Geometry bind to force window position
    private readonly NuiBind<NuiRect> _geometryBind = new("window_geometry");
    private static readonly NuiRect WindowPosition = new(360f, 100f, 320f, 220f);

    public override HurtYourselfView View { get; }
    public override NuiWindowToken Token() => _token;

    public HurtYourselfPresenter(HurtYourselfView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "Hurt Yourself (WIP)")
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
            _player.SendServerMessage("The hurt yourself window could not be created.", ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        // Force the window position using the bind
        Token().SetBindValue(_geometryBind, WindowPosition);

        // Populate damage options based on ds_emotes.nss
        List<NuiComboEntry> damageOptions = new()
        {
            new NuiComboEntry("1 HP", 0),
            new NuiComboEntry("Near Death (current HP - 1)", 1),
            new NuiComboEntry("1d6 damage", 2),
            new NuiComboEntry("1d10 damage", 3),
            new NuiComboEntry("1d20 damage", 4),
            new NuiComboEntry("1d100 damage", 5),
            new NuiComboEntry("25% of current HP", 6),
            new NuiComboEntry("50% of current HP", 7),
            new NuiComboEntry("75% of current HP", 8)
        };

        Token().SetBindValue(View.DamageOptions, damageOptions);
        Token().SetBindValue(View.DamageSelected, 0);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_apply":
                HandleApplyDamage();
                break;
            case "btn_cancel":
                Close();
                break;
        }
    }

    private void HandleApplyDamage()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        int selectedOption = Token().GetBindValue(View.DamageSelected);
        int damageAmount = CalculateDamage(creature, selectedOption);

        // Apply damage to the creature
        if (damageAmount > 0)
        {
            Effect damageEffect = Effect.Damage(damageAmount);
            creature.ApplyEffect(EffectDuration.Instant, damageEffect);
            _player.SendServerMessage($"You hurt yourself for {damageAmount} damage.", ColorConstants.Orange);
        }
    }

    private int CalculateDamage(NwCreature creature, int option)
    {
        int currentHp = creature.HP;
        int damage = 0;

        switch (option)
        {
            case 0: // 1 HP
                damage = 1;
                break;
            case 1: // Near death (current HP - 1)
                damage = currentHp - 1;
                break;
            case 2: // 1d6
                damage = Random.Shared.Next(1, 7);
                break;
            case 3: // 1d10
                damage = Random.Shared.Next(1, 11);
                break;
            case 4: // 1d20
                damage = Random.Shared.Next(1, 21);
                break;
            case 5: // 1d100
                damage = Random.Shared.Next(1, 101);
                break;
            case 6: // 25% of current HP
                damage = (int)(currentHp * 0.25f);
                break;
            case 7: // 50% of current HP
                damage = (int)(currentHp * 0.50f);
                break;
            case 8: // 75% of current HP
                damage = (int)(currentHp * 0.75f);
                break;
        }

        // Ensure we never kill the player (minimum 1 HP remaining)
        if (damage >= currentHp)
        {
            damage = currentHp - 1;
        }

        return Math.Max(0, damage);
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

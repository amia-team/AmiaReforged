using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;
using NuiUtils = AmiaReforged.PwEngine.Systems.WindowingSystem.NuiUtils;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Spellbook.CreateSpellbook;

public class CreateSpellbookView : ScryView<CreateSpellbookPresenter>, IToolWindow
{
    public readonly NuiBind<List<NuiComboEntry>> ClassNames = new(key: "class_names");
    public readonly NuiBind<int> Selection = new(key: "selected_class1");


    public readonly NuiBind<string> SpellbookName = new(key: "spellbook_name");
    public NuiButton CancelButton = null!;
    public NuiCombo ClassComboBox = null!;

    public NuiButton CreateButton = null!;


    public CreateSpellbookView(NwPlayer player)
    {
        Presenter = new(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public sealed override CreateSpellbookPresenter Presenter { get; protected set; }
    public string Id => "playertools.createspellbook";
    public bool RequiresPersistedCharacter => true;
    public string Title => "Create Spellbook";
    public string CategoryTag { get; } = null!;

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public bool ListInPlayerTools => false;

    public override NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children = new()
            {
                NuiUtils.Assign(new()
                {
                    Entries = ClassNames,
                    Selected = Selection
                }, out ClassComboBox),
                new NuiSpacer(),
                new NuiRow
                {
                    Children = new()
                    {
                        new NuiLabel(label: "Spellbook Name")
                        {
                            Aspect = 2f
                        },
                        new NuiTextEdit(label: "Enter a Name", SpellbookName, 255, false)
                    }
                },
                new NuiRow
                {
                    Children = new()
                    {
                        NuiUtils.Assign(new(label: "Create") { Id = "create_spellbook_db" }, out CreateButton),
                        NuiUtils.Assign(new(label: "Cancel"), out CancelButton)
                    }
                }
            }
        };

        return root;
    }
}
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui;

/// <summary>
///     Model for the trait selection window. Holds state and delegates to
///     <see cref="TraitSelectionService" /> for mutations.
/// </summary>
public class TraitSelectionModel
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private readonly TraitSelectionService _selectionService;
    private readonly ITraitRepository _traitRepository;

    public CharacterId? CharacterId { get; private set; }
    public TraitCategory? ActiveCategory { get; set; }

    public List<Trait> AvailableTraits { get; private set; } = [];
    public List<CharacterTrait> SelectedTraits { get; private set; } = [];
    public TraitBudget Budget { get; private set; } = TraitBudget.CreateDefault();

    public TraitSelectionModel(
        NwPlayer player,
        TraitSelectionService selectionService,
        ITraitRepository traitRepository)
    {
        _player = player;
        _selectionService = selectionService;
        _traitRepository = traitRepository;

        CharacterId = ResolveCharacterId();
    }

    public void Refresh()
    {
        if (CharacterId == null) return;

        SelectedTraits = _selectionService.GetCharacterTraits(CharacterId.Value);
        Budget = _selectionService.CalculateBudget(SelectedTraits);
        LoadAvailableTraits();
    }

    private void LoadAvailableTraits()
    {
        List<Trait> allTraits = _traitRepository.All();

        AvailableTraits = ActiveCategory.HasValue
            ? allTraits.Where(t => t.Category == ActiveCategory.Value && !t.DmOnly).ToList()
            : allTraits.Where(t => !t.DmOnly).ToList();
    }

    public bool IsTraitSelected(string traitTag)
    {
        return SelectedTraits.Any(ct => ct.TraitTag.Value == traitTag);
    }

    public bool SelectTrait(string traitTag, ICharacterInfo characterInfo)
    {
        if (CharacterId == null) return false;

        bool result = _selectionService.SelectTrait(
            CharacterId.Value.Value,
            traitTag,
            characterInfo,
            GetUnlockedTraits());

        if (result) Refresh();
        return result;
    }

    public bool DeselectTrait(string traitTag)
    {
        if (CharacterId == null) return false;

        bool result = _selectionService.DeselectTrait(CharacterId.Value.Value, traitTag);

        if (result) Refresh();
        return result;
    }

    public bool ConfirmTraits()
    {
        if (CharacterId == null) return false;

        bool result = _selectionService.ConfirmTraits(CharacterId.Value.Value);

        if (result) Refresh();
        return result;
    }

    private Dictionary<string, bool> GetUnlockedTraits()
    {
        return SelectedTraits
            .Where(ct => ct.IsUnlocked)
            .ToDictionary(ct => ct.TraitTag.Value, _ => true);
    }

    private CharacterId? ResolveCharacterId()
    {
        try
        {
            NwItem? pcKey = _player.LoginCreature?.Inventory.Items.FirstOrDefault(i => i.ResRef == "ds_pckey");
            if (pcKey == null) return null;

            string dbToken = pcKey.Name.Split("_")[1];
            if (!Guid.TryParse(dbToken, out Guid guid)) return null;

            return SharedKernel.CharacterId.From(guid);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to resolve CharacterId for trait selection");
            return null;
        }
    }
}

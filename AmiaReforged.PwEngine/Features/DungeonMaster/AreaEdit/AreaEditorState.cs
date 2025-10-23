using AmiaReforged.Core.Models.DmModels;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.AreaEdit;

/// <summary>
/// Maintains the current state of the area editor
/// </summary>
public sealed class AreaEditorState
{
    public NwArea? SelectedArea { get; set; }
    public string SearchFilter { get; set; } = string.Empty;
    public List<string> VisibleAreas { get; set; } = [];
    public List<DmArea> SavedInstances { get; set; } = [];

    public bool HasSelectedArea => SelectedArea is not null;
    public bool CanSaveArea => HasSelectedArea &&
                               NWN.Core.NWScript.GetLocalInt(SelectedArea!, "is_instance") != NWN.Core.NWScript.TRUE;
}

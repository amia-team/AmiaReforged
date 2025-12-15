using System.Numerics;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.CharacterTools.HeightChanger;

public sealed class HeightChangerModel
{
    private readonly NwPlayer _player;
    private NwCreature? _selectedTarget;

    public HeightChangerModel(NwPlayer player)
    {
        _player = player;
    }

    public List<NuiComboEntry> GetTargetOptions()
    {
        List<NuiComboEntry> options = new List<NuiComboEntry>();

        // Option 0: Player
        if (_player.ControlledCreature != null)
        {
            options.Add(new NuiComboEntry($"{_player.ControlledCreature.Name} (Self)", 0));
        }

        // Option 1+: Associates
        if (_player.ControlledCreature != null)
        {
            foreach (NwCreature associate in _player.ControlledCreature.Associates)
            {
                string associateTypeLabel = GetAssociateTypeLabel(associate.AssociateType);
                options.Add(new NuiComboEntry($"{associate.Name} ({associateTypeLabel})", options.Count));
            }
        }

        // Option N+: Custom NPCs
        List<NwCreature> customNpcs = GetCustomNpcs();
        foreach (NwCreature npc in customNpcs)
        {
            options.Add(new NuiComboEntry($"{npc.Name} (Bottled Companion)", options.Count));
        }

        return options;
    }

    public void SetSelectedTarget(int selection)
    {
        if (_player.ControlledCreature == null)
        {
            _selectedTarget = null;
            return;
        }

        // Index 0 is the player
        if (selection == 0)
        {
            _selectedTarget = _player.ControlledCreature;
            return;
        }

        int associateCount = _player.ControlledCreature.Associates.Count();

        // Check if it's an associate
        if (selection <= associateCount)
        {
            _selectedTarget = _player.ControlledCreature.Associates.ElementAtOrDefault(selection - 1);
            return;
        }

        // Otherwise it's a custom NPC
        List<NwCreature> customNpcs = GetCustomNpcs();
        int npcIndex = selection - associateCount - 1;
        if (npcIndex >= 0 && npcIndex < customNpcs.Count)
        {
            _selectedTarget = customNpcs[npcIndex];
        }
        else
        {
            _selectedTarget = null;
        }
    }

    public void SetHeight(float height)
    {
        if (_selectedTarget == null)
        {
            _player.SendServerMessage("No target selected.", ColorConstants.Orange);
            return;
        }

        if (!_selectedTarget.IsValid)
        {
            _player.SendServerMessage("Selected target is no longer valid.", ColorConstants.Orange);
            _selectedTarget = null;
            return;
        }

        _selectedTarget.VisualTransform.Translation = new Vector3(
            _selectedTarget.VisualTransform.Translation.X,
            _selectedTarget.VisualTransform.Translation.Y,
            height
        );

        _player.SendServerMessage($"Set height to {height:F1} for {_selectedTarget.Name}", ColorConstants.Green);
    }

    private List<NwCreature> GetCustomNpcs()
    {
        List<NwCreature> customNpcs = new List<NwCreature>();

        if (_player.ControlledCreature == null)
            return customNpcs;

        NwItem? pcKey = _player.ControlledCreature.Inventory.Items
            .FirstOrDefault(item => item.Tag == "ds_pckey");

        if (pcKey == null)
            return customNpcs;

        string publicKey = pcKey.Name.Length >= 8 ? pcKey.Name.Substring(0, 8) : pcKey.Name;
        string expectedTag = $"ds_npc_{publicKey}";

        // Search for custom NPCs in the area
        if (_player.ControlledCreature.Area != null)
        {
            foreach (NwCreature? creature in _player.ControlledCreature.Area.Objects
                .Where(obj => obj is NwCreature)
                .Cast<NwCreature>())
            {
                if (creature.Tag == expectedTag)
                {
                    customNpcs.Add(creature);
                }
            }
        }

        return customNpcs;
    }

    private string GetAssociateTypeLabel(AssociateType associateType)
    {
        return associateType switch
        {
            AssociateType.Henchman => "Henchman",
            AssociateType.AnimalCompanion => "Animal Companion",
            AssociateType.Familiar => "Familiar",
            AssociateType.Summoned => "Summon",
            AssociateType.Dominated => "Dominated",
            _ => "Associate"
        };
    }
}


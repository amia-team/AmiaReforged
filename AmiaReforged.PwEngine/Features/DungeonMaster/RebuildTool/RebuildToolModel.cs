using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.RebuildTool;

public sealed class RebuildToolModel
{
    private readonly NwPlayer _player;
    public NwCreature? SelectedCharacter { get; private set; }

    public delegate void CharacterSelectedEventHandler(RebuildToolModel sender, EventArgs e);
    public event CharacterSelectedEventHandler? OnCharacterSelected;

    public RebuildToolModel(NwPlayer player)
    {
        _player = player;
    }

    public void EnterTargetingMode()
    {
        _player.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            CursorType = MouseCursor.Action,
            ValidTargets = ObjectTypes.Creature
        });
    }

    private void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.TargetObject is not NwCreature creature)
        {
            _player.SendServerMessage("Invalid target. Please select a creature.");
            return;
        }

        if (!creature.IsPlayerControlled)
        {
            _player.SendServerMessage("Target must be a player character.");
            return;
        }

        SelectedCharacter = creature;
        OnCharacterSelected?.Invoke(this, EventArgs.Empty);
    }

    public void AddFeatToCharacter(int featId, int level)
    {
        if (SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected.");
            return;
        }

        if (level < 1 || level > 30)
        {
            _player.SendServerMessage("Level must be between 1 and 30.");
            return;
        }

        NwFeat? feat = NwFeat.FromFeatId(featId);
        if (feat == null)
        {
            _player.SendServerMessage($"Invalid feat ID: {featId}");
            return;
        }

        SelectedCharacter.AddFeat(feat, level);
        _player.SendServerMessage($"Added feat {feat.Name} to {SelectedCharacter.Name} at level {level}.");
    }

    public void RemoveFeatFromCharacter(int featId)
    {
        if (SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected.");
            return;
        }

        NwFeat? feat = NwFeat.FromFeatId(featId);
        if (feat == null)
        {
            _player.SendServerMessage($"Invalid feat ID: {featId}");
            return;
        }

        SelectedCharacter.RemoveFeat(feat);
        _player.SendServerMessage($"Removed feat {feat.Name} from {SelectedCharacter.Name}.");
    }
}


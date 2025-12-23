using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using AmiaReforged.Races.Races;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.RebuildTool;

public sealed class RebuildToolModel
{
    private readonly NwPlayer _player;
    public NwCreature? SelectedCharacter { get; private set; }
    private int _lastRemovedXp = 0;

    public event EventHandler? OnCharacterSelected;

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

        SelectedCharacter.RemoveFeat(feat, true); // true = remove from level list
        _player.SendServerMessage($"Removed feat {feat.Name} from {SelectedCharacter.Name}.");
    }

    public void PartialRebuild(int targetLevel, NwPlayer targetPlayer)
    {
        if (SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected.");
            return;
        }

        if (targetLevel < 1 || targetLevel > 29)
        {
            _player.SendServerMessage("Target level must be between 1 and 29.");
            return;
        }

        int currentLevel = SelectedCharacter.Level;
        if (targetLevel >= currentLevel)
        {
            _player.SendServerMessage($"Target level ({targetLevel}) must be lower than current level ({currentLevel}).");
            return;
        }

        // Check for Heritage Feat (feat 1238)
        NwFeat? heritageFeat = NwFeat.FromFeatId(1238);
        bool hasHeritageFeat = heritageFeat != null && SelectedCharacter.KnowsFeat(heritageFeat);

        if (hasHeritageFeat)
        {
            // Find which level they took the heritage feat
            int heritageFeatLevel = -1;
            for (int level = 1; level <= currentLevel; level++)
            {
                CreatureLevelInfo levelInfo = SelectedCharacter.GetLevelStats(level);
                if (levelInfo.Feats.Any(f => f.Id == 1238))
                {
                    heritageFeatLevel = level;
                    break;
                }
            }

            // If heritage feat was taken at a level higher than target, remove heritage bonuses
            if (heritageFeatLevel > targetLevel)
            {
                int playerRace = ResolvePlayerRace(targetPlayer);

                // Remove heritage abilities if the race is supported
                if (ManagedRaces.RaceHeritageAbilities.ContainsKey(playerRace))
                {
                    ManagedRaces.RaceHeritageAbilities[playerRace].RemoveStats(targetPlayer);
                    _player.SendServerMessage($"Removed heritage abilities for {SelectedCharacter.Name}.");
                }

                // Remove the heritage feat
                SelectedCharacter.RemoveFeat(heritageFeat!, true);

                // Delete heritage_setup variable from PC Key
                uint pcKey = NWN.Core.NWScript.GetItemPossessedBy(SelectedCharacter, "ds_pckey");
                if (NWN.Core.NWScript.GetIsObjectValid(pcKey) == NWN.Core.NWScript.TRUE)
                {
                    NWN.Core.NWScript.DeleteLocalInt(pcKey, "heritage_setup");
                }

                _player.SendServerMessage($"Removed heritage feat from {SelectedCharacter.Name}.");
            }
        }

        // Calculate XP for target level and current level
        // XP Formula: level * (level - 1) * 500
        int currentXp = SelectedCharacter.Xp;
        int targetXp = targetLevel * (targetLevel - 1) * 500;
        int xpRemoved = currentXp - targetXp;

        // Store the removed XP so we can return it later if needed
        _lastRemovedXp = xpRemoved;

        // Set character to target level
        SelectedCharacter.Xp = targetXp;

        // Send messages to both DM and player
        string message = $"Partial Rebuild: {SelectedCharacter.Name} set to level {targetLevel}. Removed {xpRemoved:N0} XP.";
        _player.SendServerMessage(message, ColorConstants.Orange);
        targetPlayer.SendServerMessage(message, ColorConstants.Orange);
    }

    public void ReturnAllXP(NwPlayer targetPlayer)
    {
        if (SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected.");
            return;
        }

        if (_lastRemovedXp <= 0)
        {
            _player.SendServerMessage("No XP was removed in the last rebuild.");
            return;
        }

        int currentLevel = SelectedCharacter.Level;
        int currentXp = SelectedCharacter.Xp;

        // Return exactly the amount that was removed
        int newXp = currentXp + _lastRemovedXp;
        SelectedCharacter.Xp = newXp;

        // Send messages to both DM and player
        string message = $"Return All XP: {SelectedCharacter.Name} gained {_lastRemovedXp:N0} XP (returned to level {currentLevel}).";
        _player.SendServerMessage(message, ColorConstants.Green);
        targetPlayer.SendServerMessage(message, ColorConstants.Green);

        _player.SendServerMessage($"Character will need to relog to properly update.", ColorConstants.Yellow);
        targetPlayer.SendServerMessage($"You will need to relog to properly update your character.", ColorConstants.Yellow);

        // Reset the tracked XP since we've returned it
        _lastRemovedXp = 0;
    }

    private int ResolvePlayerRace(NwPlayer player) =>
        player.LoginCreature?.SubRace.ToLower() switch
        {
            "aasimar" => (int)ManagedRaces.RacialType.Aasimar,
            "tiefling" => (int)ManagedRaces.RacialType.Tiefling,
            "feytouched" => (int)ManagedRaces.RacialType.Feytouched,
            "feyri" => (int)ManagedRaces.RacialType.Feyri,
            "air genasi" => (int)ManagedRaces.RacialType.AirGenasi,
            "earth genasi" => (int)ManagedRaces.RacialType.EarthGenasi,
            "fire genasi" => (int)ManagedRaces.RacialType.FireGenasi,
            "water genasi" => (int)ManagedRaces.RacialType.WaterGenasi,
            "avariel" => (int)ManagedRaces.RacialType.Avariel,
            "lizardfolk" => (int)ManagedRaces.RacialType.Lizardfolk,
            "half dragon" => (int)ManagedRaces.RacialType.Halfdragon,
            "dragon" => (int)ManagedRaces.RacialType.Halfdragon,
            "centaur" => (int)ManagedRaces.RacialType.Centaur,
            "aquatic elf" => (int)ManagedRaces.RacialType.AquaticElf,
            "elfling" => (int)ManagedRaces.RacialType.Elfling,
            "shadovar" => (int)ManagedRaces.RacialType.Shadovar,
            "drow" => (int)ManagedRaces.RacialType.Drow,
            _ => NWN.Core.NWScript.GetRacialType(SelectedCharacter)
        };
}

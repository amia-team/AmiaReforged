using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using AmiaReforged.Races.Races;
using NWN.Core.NWNX;
using AmiaReforged.PwEngine.Database.Entities.Admin;
using NLog;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.RebuildTool;

public sealed class RebuildToolModel
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private readonly IRebuildRepository _repository;
    public NwCreature? SelectedCharacter { get; private set; }
    private int _lastRemovedXp;

    public event EventHandler? OnCharacterSelected;

    public RebuildToolModel(NwPlayer player, IRebuildRepository repository)
    {
        _player = player;
        _repository = repository;
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

    public void SetSelectedCharacter(NwCreature character)
    {
        SelectedCharacter = character;
        OnCharacterSelected?.Invoke(this, EventArgs.Empty);
    }

    public Task<bool> VerifyPCKeyMatch(int rebuildId, NwCreature newCharacter, NwCreature? dmCreature)
    {
        try
        {
            var rebuild = _repository.GetById(rebuildId);
            if (rebuild == null)
            {
                _player.SendServerMessage($"Rebuild ID {rebuildId} not found!", ColorConstants.Red);
                return Task.FromResult(false);
            }

            // Check if we have PC Key data in the database
            if (rebuild.PcKeyData == null || rebuild.PcKeyData.Length == 0)
            {
                _player.SendServerMessage("No PC Key data found in rebuild record!", ColorConstants.Red);
                return Task.FromResult(false);
            }

            // Verify DM has the PC Key in their inventory
            if (dmCreature == null)
            {
                _player.SendServerMessage("DM controlled creature not found!", ColorConstants.Red);
                return Task.FromResult(false);
            }

            NwItem? pcKeyInDm = null;
            foreach (NwItem item in dmCreature.Inventory.Items)
            {
                if (item.Tag == "ds_pckey")
                {
                    pcKeyInDm = item;
                    break;
                }
            }

            if (pcKeyInDm == null)
            {
                _player.SendServerMessage("PC Key not found in your inventory! Did you start the rebuild?", ColorConstants.Red);
                return Task.FromResult(false);
            }

            // Compare the PC Key name with the stored PC Key from database
            // The PC Key name should be unique per character
            string currentKeyName = pcKeyInDm.Name;

            // Deserialize the stored PC Key to get its name
            NwItem? storedKey = DeserializeItem(rebuild.PcKeyData, dmCreature);
            if (storedKey == null)
            {
                _player.SendServerMessage("Failed to deserialize stored PC Key data!", ColorConstants.Red);
                return Task.FromResult(false);
            }

            string storedKeyName = storedKey.Name;
            storedKey.Destroy(); // Clean up the temporary deserialized key

            if (currentKeyName == storedKeyName)
            {
                _player.SendServerMessage("PC Key verified successfully!", ColorConstants.Green);
                return Task.FromResult(true);
            }
            else
            {
                _player.SendServerMessage($"PC Key mismatch! DM has '{currentKeyName}' but rebuild expects '{storedKeyName}'", ColorConstants.Red);
                return Task.FromResult(false);
            }
        }
        catch (Exception ex)
        {
            _player.SendServerMessage($"Error verifying PC Key: {ex.Message}", ColorConstants.Red);
            return Task.FromResult(false);
        }
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

        // Set character to target level using NWScript
        NWN.Core.NWScript.SetXP((uint)SelectedCharacter, targetXp);

        // Check and reduce languages if they've lost language slots
        CheckAndReduceLanguages(targetPlayer);

        // Send messages to both DM and player
        string message = $"Partial Rebuild: {SelectedCharacter.Name} set to level {targetLevel}. Removed {xpRemoved:N0} XP.";
        _player.SendServerMessage(message, ColorConstants.Orange);
        targetPlayer.SendServerMessage(message, ColorConstants.Orange);
    }

    public void ReturnAllXP(NwPlayer targetPlayer, int? returnToLevel = null)
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
        int xpToReturn;
        string message;

        if (returnToLevel.HasValue)
        {
            // Validate the return to level
            int targetLevel = returnToLevel.Value;

            if (targetLevel < 2 || targetLevel > 30)
            {
                _player.SendServerMessage("Return to level must be between 2 and 30.");
                return;
            }

            if (targetLevel <= currentLevel)
            {
                _player.SendServerMessage($"Return to level ({targetLevel}) must be higher than current level ({currentLevel}).");
                return;
            }

            // Calculate XP needed to reach target level
            int targetXp = targetLevel * (targetLevel - 1) * 500;
            xpToReturn = targetXp - currentXp;

            // Make sure we don't return more than what was removed
            if (xpToReturn > _lastRemovedXp)
            {
                xpToReturn = _lastRemovedXp;
                _player.SendServerMessage($"Warning: Requested level requires more XP than was removed. Returning all {_lastRemovedXp:N0} XP instead.", ColorConstants.Yellow);
            }

            // Set character to target XP using NWScript
            NWN.Core.NWScript.SetXP((uint)SelectedCharacter, currentXp + xpToReturn);

            // Reduce the tracked removed XP
            _lastRemovedXp -= xpToReturn;

            message = $"Return XP to Level {targetLevel}: {SelectedCharacter.Name} gained {xpToReturn:N0} XP.";
        }
        else
        {
            // Return all removed XP
            xpToReturn = _lastRemovedXp;
            NWN.Core.NWScript.SetXP((uint)SelectedCharacter, currentXp + xpToReturn);

            message = $"Return All XP: {SelectedCharacter.Name} gained {xpToReturn:N0} XP.";

            // Reset the tracked XP since we've returned it all
            _lastRemovedXp = 0;
        }

        // Send messages to both DM and player
        _player.SendServerMessage(message, ColorConstants.Green);
        targetPlayer.SendServerMessage(message, ColorConstants.Green);
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

    public List<(int id, string label, int nameStrRef)> LoadRacialTypes()
    {
        List<(int id, string label, int nameStrRef)> races = new();

        // Read racialtypes.2da - iterate through rows until we hit an invalid entry
        for (int i = 0; i < 200; i++) // Reasonable upper limit
        {
            string label = NWN.Core.NWScript.Get2DAString("racialtypes", "Label", i);

            // If label is empty or "****", we've reached the end
            if (string.IsNullOrEmpty(label) || label == "****")
                break;

            // Skip races marked as DELETED or INVALID_RACE
            if (label.Contains("DELETED", StringComparison.OrdinalIgnoreCase) ||
                label.Contains("INVALID_RACE", StringComparison.OrdinalIgnoreCase))
                continue;

            string nameStr = NWN.Core.NWScript.Get2DAString("racialtypes", "Name", i);

            // Try to parse the Name column as a strref number
            if (int.TryParse(nameStr, out int nameStrRef))
            {
                races.Add((i, label, nameStrRef));
            }
            else
            {
                // If Name isn't a number, just use the label
                races.Add((i, label, 0));
            }
        }

        return races;
    }

    public string GetCurrentRaceInfo()
    {
        if (SelectedCharacter == null)
            return "No character selected";

        int racialType = NWN.Core.NWScript.GetRacialType((uint)SelectedCharacter);
        string label = NWN.Core.NWScript.Get2DAString("racialtypes", "Label", racialType);

        return $"{label} (ID: {racialType})";
    }

    public void ChangeCharacterRace(int newRacialType, NwPlayer targetPlayer, string? optionalSubrace = null)
    {
        if (SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected.");
            return;
        }

        // Get the race label
        string label = NWN.Core.NWScript.Get2DAString("racialtypes", "Label", newRacialType);

        int currentRace = NWN.Core.NWScript.GetRacialType((uint)SelectedCharacter);

        // Use NWNX to set the racial type
        CreaturePlugin.SetRacialType((uint)SelectedCharacter, newRacialType);

        // Verify the change
        int verifyRace = NWN.Core.NWScript.GetRacialType((uint)SelectedCharacter);

        if (verifyRace == newRacialType)
        {
            _player.SendServerMessage($"Successfully changed race from {currentRace} to {newRacialType} ({label})", ColorConstants.Green);
            targetPlayer.SendServerMessage($"Your racial type has been changed to {label} by a DM.", ColorConstants.Orange);

            // Only set subrace if the DM provided a value
            if (!string.IsNullOrWhiteSpace(optionalSubrace))
            {
                try
                {
                    SelectedCharacter.SubRace = optionalSubrace;
                    _player.SendServerMessage($"Subrace field set to: {optionalSubrace}", ColorConstants.Green);
                    targetPlayer.SendServerMessage($"Your subrace has been set to: {optionalSubrace}", ColorConstants.Orange);
                }
                catch (Exception ex)
                {
                    _player.SendServerMessage($"Could not set subrace field: {ex.Message}", ColorConstants.Yellow);
                }
            }
            else
            {
                _player.SendServerMessage($"Subrace field was not changed.", ColorConstants.Gray);
            }
        }
        else
        {
            _player.SendServerMessage($"Failed to change race. Current race is still: {verifyRace}", ColorConstants.Red);
        }
    }

    public void ClearCharacterSubrace(NwPlayer targetPlayer)
    {
        if (SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected.");
            return;
        }

        try
        {
            SelectedCharacter.SubRace = "";
            _player.SendServerMessage($"Cleared subrace field for {SelectedCharacter.Name}.", ColorConstants.Green);
            targetPlayer.SendServerMessage($"Your subrace has been cleared by a DM.", ColorConstants.Orange);
        }
        catch (Exception ex)
        {
            _player.SendServerMessage($"Could not clear subrace field: {ex.Message}", ColorConstants.Red);
        }
    }

    // Full Rebuild Methods
    public Task<int?> StartFullRebuild(NwPlayer targetPlayer)
    {
        if (SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected.");
            return Task.FromResult<int?>(null);
        }

        try
        {
            // Find PC Key
            NwItem? pcKey = null;
            foreach (NwItem item in SelectedCharacter.Inventory.Items)
            {
                if (item.Tag == "ds_pckey")
                {
                    pcKey = item;
                    break;
                }
            }

            if (pcKey == null)
            {
                _player.SendServerMessage("PC Key (ds_pckey) not found in character inventory!", ColorConstants.Red);
                return Task.FromResult<int?>(null);
            }

            // Copy PC Key to DM inventory
            if (_player.ControlledCreature == null)
            {
                _player.SendServerMessage("DM controlled creature not found!", ColorConstants.Red);
                return Task.FromResult<int?>(null);
            }

            NwItem? pcKeyCopy = pcKey.Clone(_player.ControlledCreature);
            if (pcKeyCopy == null)
            {
                _player.SendServerMessage("Failed to copy PC Key to DM inventory!", ColorConstants.Red);
                return Task.FromResult<int?>(null);
            }

            _player.SendServerMessage("PC Key copied to your inventory.", ColorConstants.Green);

            // Unequip all items first to ensure we capture everything
            _player.SendServerMessage("Unequipping all items...", ColorConstants.Cyan);

            List<InventorySlot> equipmentSlots = new List<InventorySlot>
            {
                InventorySlot.Head,
                InventorySlot.Chest,
                InventorySlot.Boots,
                InventorySlot.Arms,
                InventorySlot.RightHand,
                InventorySlot.LeftHand,
                InventorySlot.Cloak,
                InventorySlot.LeftRing,
                InventorySlot.RightRing,
                InventorySlot.Neck,
                InventorySlot.Belt,
                InventorySlot.Arrows,
                InventorySlot.Bullets,
                InventorySlot.Bolts
            };

            int unequippedCount = 0;
            foreach (var slot in equipmentSlots)
            {
                NwItem? equippedItem = SelectedCharacter.GetItemInSlot(slot);
                if (equippedItem != null)
                {
                    SelectedCharacter.RunUnequip(equippedItem);
                    unequippedCount++;
                }
            }

            if (unequippedCount > 0)
            {
                _player.SendServerMessage($"Unequipped {unequippedCount} items.", ColorConstants.Green);
            }

            // Create rebuild record
            var rebuild = new CharacterRebuild
            {
                PlayerCdKey = targetPlayer.CDKey,
                CharacterId = SelectedCharacter.UUID,
                RequestedUtc = DateTime.UtcNow,
                StoredXp = SelectedCharacter.Xp,
                StoredGold = (int)SelectedCharacter.Gold,
                OriginalFirstName = SelectedCharacter.OriginalFirstName,
                OriginalLastName = SelectedCharacter.OriginalLastName,
                PcKeyData = SerializeItem(pcKeyCopy),
                // Explicitly set navigation properties to null to avoid EF tracking issues
                Player = null,
                Character = null
            };

            _repository.Add(rebuild);
            _repository.SaveChanges();

            _player.SendServerMessage($"Rebuild record created. ID: {rebuild.Id}", ColorConstants.Green);

            // Save all items to database and then destroy them
            // IMPORTANT: Only save root-level items (items directly in inventory, not inside containers)
            // The serialization will automatically include items inside containers
            int itemCount = 0;
            List<NwItem> itemsToDestroy = new List<NwItem>();

            foreach (NwItem item in SelectedCharacter.Inventory.Items)
            {
                if (item.Tag == "ds_pckey") continue; // Skip PC Key

                // Check if this item is at the root level (possessed directly by the character, not by another item)
                if (item.Possessor == SelectedCharacter)
                {
                    byte[] itemData = SerializeItem(item);
                    var itemRecord = new RebuildItemRecord
                    {
                        CharacterRebuildId = rebuild.Id,
                        ItemData = itemData
                    };

                    _repository.AddItemRecord(itemRecord);
                    itemsToDestroy.Add(item);
                    itemCount++;
                }
            }

            _repository.SaveChanges();
            _player.SendServerMessage($"Saved {itemCount} root-level items to database (contents preserved in containers).", ColorConstants.Green);

            // Now destroy all the items that were saved
            foreach (NwItem item in itemsToDestroy)
            {
                item.Destroy();
            }
            _player.SendServerMessage($"Destroyed {itemsToDestroy.Count} items from character inventory.", ColorConstants.Orange);

            // Take all XP
            NWN.Core.NWScript.SetXP((uint)SelectedCharacter, 0);
            _player.SendServerMessage($"Removed {rebuild.StoredXp:N0} XP from character.", ColorConstants.Orange);

            // Take all gold
            SelectedCharacter.Gold = 0;
            _player.SendServerMessage($"Removed {rebuild.StoredGold:N0} gold from character.", ColorConstants.Orange);

            // Add "zzz" prefix to character name
            string newFirstName = "zzz" + SelectedCharacter.OriginalFirstName;
            SelectedCharacter.OriginalFirstName = newFirstName;

            // Update the display name immediately using NWScript
            string fullName = $"{newFirstName} {SelectedCharacter.OriginalLastName}";
            NWN.Core.NWScript.SetName(SelectedCharacter, fullName);

            // Force update the character sheet to show the new name
            PlayerPlugin.UpdateCharacterSheet((uint)SelectedCharacter);

            _player.SendServerMessage($"Character name changed to: {fullName}", ColorConstants.Orange);

            targetPlayer.SendServerMessage("Your character has been prepared for full rebuild. Please log off and create your new character.", ColorConstants.Yellow);
            _player.SendServerMessage("Full rebuild started successfully. Player should log off now.", ColorConstants.Green);

            return Task.FromResult<int?>(rebuild.Id);
        }
        catch (Exception ex)
        {
            _player.SendServerMessage($"Error starting full rebuild: {ex.Message}", ColorConstants.Red);
            return Task.FromResult<int?>(null);
        }
    }

    public Task ReturnInventory(int rebuildId, NwPlayer targetPlayer)
    {
        if (SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected. Please select the NEW character that the player created.", ColorConstants.Red);
            return Task.CompletedTask;
        }

        try
        {
            var rebuild = _repository.GetById(rebuildId);
            if (rebuild == null)
            {
                _player.SendServerMessage($"Rebuild ID {rebuildId} not found!", ColorConstants.Red);
                return Task.CompletedTask;
            }

            // Verify the DM has a controlled creature
            if (_player.ControlledCreature == null)
            {
                _player.SendServerMessage("DM controlled creature not found!", ColorConstants.Red);
                return Task.CompletedTask;
            }

            // Unequip all items first to ensure we can destroy equipped items too
            List<InventorySlot> equipmentSlots = new List<InventorySlot>
            {
                InventorySlot.Head,
                InventorySlot.Chest,
                InventorySlot.Boots,
                InventorySlot.Arms,
                InventorySlot.RightHand,
                InventorySlot.LeftHand,
                InventorySlot.Cloak,
                InventorySlot.LeftRing,
                InventorySlot.RightRing,
                InventorySlot.Neck,
                InventorySlot.Belt,
                InventorySlot.Arrows,
                InventorySlot.Bullets,
                InventorySlot.Bolts
            };

            int unequippedCount = 0;
            foreach (var slot in equipmentSlots)
            {
                NwItem? equippedItem = SelectedCharacter.GetItemInSlot(slot);
                if (equippedItem != null)
                {
                    SelectedCharacter.RunUnequip(equippedItem);
                    unequippedCount++;
                }
            }

            if (unequippedCount > 0)
            {
                _player.SendServerMessage($"Unequipped {unequippedCount} items from new character.", ColorConstants.Cyan);
            }

            // Destroy all existing items in character inventory to prevent duplicates
            List<NwItem> existingItems = SelectedCharacter.Inventory.Items.ToList();

            foreach (NwItem item in existingItems)
            {
                item.Destroy();
            }
            _player.SendServerMessage($"Cleared {existingItems.Count} starting items from character.", ColorConstants.Orange);

            // Restore PC Key first from database
            if (rebuild.PcKeyData != null && rebuild.PcKeyData.Length > 0)
            {
                NwItem? pcKey = DeserializeItem(rebuild.PcKeyData, SelectedCharacter);
                if (pcKey != null)
                {
                    _player.SendServerMessage("PC Key restored to player from database.", ColorConstants.Green);

                    // Remove heritage_setup variable since this is a new character
                    // They will need to reselect their heritage on the new character
                    if (NWN.Core.NWScript.GetLocalInt((uint)pcKey, "heritage_setup") > 0)
                    {
                        NWN.Core.NWScript.DeleteLocalInt((uint)pcKey, "heritage_setup");
                        _player.SendServerMessage("Cleared heritage_setup from PC Key. Player will need to reselect heritage on new character.", ColorConstants.Cyan);
                    }
                }
                else
                {
                    _player.SendServerMessage("Warning: Failed to restore PC Key from database!", ColorConstants.Yellow);
                }
            }

            // Destroy the DM's backup copy of the PC Key
            if (_player.ControlledCreature != null)
            {
                NwItem? pcKeyInDm = null;
                foreach (NwItem item in _player.ControlledCreature.Inventory.Items)
                {
                    if (item.Tag == "ds_pckey")
                    {
                        // Verify it's the right PC Key by comparing names
                        if (rebuild.PcKeyData != null && rebuild.PcKeyData.Length > 0)
                        {
                            NwItem? storedKey = DeserializeItem(rebuild.PcKeyData, _player.ControlledCreature);
                            if (storedKey != null)
                            {
                                if (item.Name == storedKey.Name)
                                {
                                    pcKeyInDm = item;
                                    storedKey.Destroy(); // Clean up temp key
                                    break;
                                }
                                storedKey.Destroy(); // Clean up temp key
                            }
                        }
                    }
                }

                if (pcKeyInDm != null)
                {
                    pcKeyInDm.Destroy();
                    _player.SendServerMessage("Destroyed DM's backup copy of PC Key.", ColorConstants.Cyan);
                }
            }

            // Restore all other items
            var itemRecords = _repository.GetItemRecords(rebuildId);
            int restoredCount = 0;

            foreach (var itemRecord in itemRecords)
            {
                NwItem? item = DeserializeItem(itemRecord.ItemData, SelectedCharacter);
                if (item != null)
                {
                    restoredCount++;
                }
            }

            _player.SendServerMessage($"Restored {restoredCount} items.", ColorConstants.Green);

            // Restore gold
            SelectedCharacter.Gold = (uint)rebuild.StoredGold;
            _player.SendServerMessage($"Restored {rebuild.StoredGold:N0} gold.", ColorConstants.Green);

            // Restore character name
            if (!string.IsNullOrEmpty(rebuild.OriginalFirstName))
            {
                SelectedCharacter.OriginalFirstName = rebuild.OriginalFirstName;
            }
            if (!string.IsNullOrEmpty(rebuild.OriginalLastName))
            {
                SelectedCharacter.OriginalLastName = rebuild.OriginalLastName;
            }

            // Update the character's display name immediately using NWScript
            string fullName = $"{rebuild.OriginalFirstName} {rebuild.OriginalLastName}";
            NWN.Core.NWScript.SetName(SelectedCharacter, fullName);

            // Force update the character sheet to show the new name
            PlayerPlugin.UpdateCharacterSheet((uint)SelectedCharacter);

            _player.SendServerMessage($"Character name set to: {fullName}", ColorConstants.Green);

            // Remove heritage feat if present
            RemoveHeritageFeat(targetPlayer);

            targetPlayer.SendServerMessage("Your inventory and gold have been restored by a DM.", ColorConstants.Green);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _player.SendServerMessage($"Error returning inventory: {ex.Message}", ColorConstants.Red);
            return Task.CompletedTask;
        }
    }

    public void ReturnFullRebuildXP(int rebuildId, NwPlayer targetPlayer, int? returnToLevel = null)
    {
        if (SelectedCharacter == null)
        {
            _player.SendServerMessage("No character selected.");
            return;
        }

        try
        {
            var rebuild = _repository.GetById(rebuildId);
            if (rebuild == null)
            {
                _player.SendServerMessage($"Rebuild ID {rebuildId} not found!", ColorConstants.Red);
                return;
            }

            int currentXp = SelectedCharacter.Xp;
            int xpToReturn;
            string message;

            if (returnToLevel.HasValue)
            {
                int targetLevel = returnToLevel.Value;

                if (targetLevel < 2 || targetLevel > 30)
                {
                    _player.SendServerMessage("Return to level must be between 2 and 30.");
                    return;
                }

                int currentLevel = SelectedCharacter.Level;
                if (targetLevel <= currentLevel)
                {
                    _player.SendServerMessage($"Return to level ({targetLevel}) must be higher than current level ({currentLevel}).");
                    return;
                }

                int targetXp = targetLevel * (targetLevel - 1) * 500;
                xpToReturn = targetXp - currentXp;

                if (xpToReturn > rebuild.StoredXp)
                {
                    xpToReturn = rebuild.StoredXp;
                    _player.SendServerMessage($"Warning: Requested level requires more XP than was stored. Returning all {rebuild.StoredXp:N0} XP instead.", ColorConstants.Yellow);
                }

                NWN.Core.NWScript.SetXP((uint)SelectedCharacter, currentXp + xpToReturn);
                rebuild.StoredXp -= xpToReturn;
                message = $"Return XP to Level {targetLevel}: {SelectedCharacter.Name} gained {xpToReturn:N0} XP.";
            }
            else
            {
                xpToReturn = rebuild.StoredXp;

                // Subtract starting XP (4000) since the new character already has it
                int currentCharacterXp = SelectedCharacter.Xp;
                int finalXp = currentCharacterXp + xpToReturn - 4000;

                // Make sure we don't go negative
                if (finalXp < 0)
                {
                    finalXp = 0;
                }

                NWN.Core.NWScript.SetXP((uint)SelectedCharacter, finalXp);
                rebuild.StoredXp = 0;
                message = $"Return All XP: {SelectedCharacter.Name} set to {finalXp:N0} XP (stored XP minus 4,000 starting XP).";
            }

            _repository.Update(rebuild);
            _repository.SaveChanges();

            _player.SendServerMessage(message, ColorConstants.Green);
            targetPlayer.SendServerMessage(message, ColorConstants.Green);
        }
        catch (Exception ex)
        {
            _player.SendServerMessage($"Error returning XP: {ex.Message}", ColorConstants.Red);
        }
    }

    public void FinishFullRebuild(int rebuildId, NwCreature targetCharacter)
    {
        try
        {
            var rebuild = _repository.GetById(rebuildId);
            if (rebuild == null)
            {
                _player.SendServerMessage($"Rebuild ID {rebuildId} not found!", ColorConstants.Red);
                return;
            }

            // Get the player who owns this character
            NwPlayer? targetPlayer = targetCharacter.ControllingPlayer;
            if (targetPlayer != null)
            {
                // Check and reduce languages if they've lost language slots
                CheckAndReduceLanguages(targetPlayer);
            }

            _repository.CompleteRebuild(rebuildId);
            _repository.DeleteItemRecordsByRebuildId(rebuildId);
            _repository.Delete(rebuildId);
            _repository.SaveChanges();

            _player.SendServerMessage("Full rebuild finalized and cleared from database.", ColorConstants.Green);
        }
        catch (Exception ex)
        {
            _player.SendServerMessage($"Error finishing rebuild: {ex.Message}", ColorConstants.Red);
        }
    }

    public IEnumerable<(int rebuildId, string firstName, string lastName)> GetPendingRebuilds()
    {
        try
        {
            return _repository.GetPendingRebuilds()
                .Select(r => (r.Id, r.OriginalFirstName ?? "Unknown", r.OriginalLastName ?? ""))
                .ToList();
        }
        catch (Exception ex)
        {
            _player.SendServerMessage($"Error loading pending rebuilds: {ex.Message}", ColorConstants.Red);
            return Enumerable.Empty<(int, string, string)>();
        }
    }

    public void LoadPendingRebuild(int rebuildId)
    {
        try
        {
            var rebuild = _repository.GetById(rebuildId);
            if (rebuild == null)
            {
                _player.SendServerMessage($"Rebuild ID {rebuildId} not found!", ColorConstants.Red);
                return;
            }

            // Recreate PC Key in DM inventory
            if (rebuild.PcKeyData != null && rebuild.PcKeyData.Length > 0)
            {
                NwItem? pcKey = DeserializeItem(rebuild.PcKeyData, _player.ControlledCreature);
                if (pcKey != null)
                {
                    _player.SendServerMessage("PC Key recreated in your inventory.", ColorConstants.Green);
                }
            }
        }
        catch (Exception ex)
        {
            _player.SendServerMessage($"Error loading rebuild: {ex.Message}", ColorConstants.Red);
        }
    }

    private byte[] SerializeItem(NwItem item)
    {
        byte[]? serialized = item.Serialize();
        return serialized ?? Array.Empty<byte>();
    }

    private NwItem? DeserializeItem(byte[] itemData, NwGameObject? owner = null)
    {
        try
        {
            // Deserialize the item with its full contents
            NwItem? item = NwItem.Deserialize(itemData);

            if (item == null)
            {
                _player.SendServerMessage("Failed to deserialize an item.", ColorConstants.Yellow);
                return null;
            }

            // If we have an owner (creature), use AcquireItem to properly transfer the item
            // This preserves container contents, unlike Clone
            if (owner is NwCreature creature)
            {
                creature.AcquireItem(item);
                return item;
            }

            return item;
        }
        catch (Exception ex)
        {
            _player.SendServerMessage($"Error deserializing item: {ex.Message}", ColorConstants.Yellow);
            return null;
        }
    }

    private void CheckAndReduceLanguages(NwPlayer targetPlayer)
    {
        Log.Info($"[LANG-DM] CheckAndReduceLanguages called for {SelectedCharacter?.Name ?? "null"}");

        if (SelectedCharacter == null)
        {
            Log.Info($"[LANG-DM] Selected character is null, returning");
            return;
        }

        // Find PC Key
        uint pcKeyId = NWN.Core.NWScript.GetItemPossessedBy(SelectedCharacter, "ds_pckey");
        bool isValid = NWN.Core.NWScript.GetIsObjectValid(pcKeyId) == NWN.Core.NWScript.TRUE;
        Log.Info($"[LANG-DM] PC Key ID: {pcKeyId}, IsValid: {isValid}");

        if (!isValid) return;

        // Get current chosen languages
        string chosenStr = NWN.Core.NWScript.GetLocalString(pcKeyId, "LANGUAGES_CHOSEN");
        Log.Info($"[LANG-DM] LANGUAGES_CHOSEN string: '{chosenStr}'");

        if (string.IsNullOrEmpty(chosenStr))
        {
            Log.Info($"[LANG-DM] LANGUAGES_CHOSEN is null or empty, returning");
            return;
        }

        List<string> chosenLanguages = chosenStr.Split('|').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        Log.Info($"[LANG-DM] Parsed chosen languages count: {chosenLanguages.Count}, languages: {string.Join(", ", chosenLanguages)}");

        // If no languages chosen, nothing to do
        if (chosenLanguages.Count == 0)
        {
            Log.Info($"[LANG-DM] No languages in list after parsing, returning");
            return;
        }

        // Calculate current max language count
        int currentMaxLanguages = CalculateMaxLanguagesForCharacter();
        Log.Info($"[LANG-DM] Current max languages calculated: {currentMaxLanguages}");

        // Get the stored total from when they last saved
        int previousTotal = NWN.Core.NWScript.GetLocalInt(pcKeyId, "LANGUAGES_TOTAL");
        Log.Info($"[LANG-DM] LANGUAGES_TOTAL from PC Key: {previousTotal}");

        // If LANGUAGES_TOTAL doesn't exist (legacy character), use the current chosen count as the previous total
        if (previousTotal == 0)
        {
            previousTotal = chosenLanguages.Count;
            Log.Info($"[LANG-DM] LANGUAGES_TOTAL was 0 (legacy character), setting to {previousTotal}");
            // Save it for future reference
            NWN.Core.NWScript.SetLocalInt(pcKeyId, "LANGUAGES_TOTAL", previousTotal);
        }

        Log.Info($"[LANG-DM] Comparison - Current Max: {currentMaxLanguages}, Previous Total: {previousTotal}");

        // If current max is less than previous total, they've lost language slots
        if (currentMaxLanguages < previousTotal)
        {
            Log.Info($"[LANG-DM] Player has LOST language slots! Processing removal...");

            // Calculate how many languages need to be removed
            int languagesToRemove = previousTotal - currentMaxLanguages;
            int actualRemoveCount = Math.Min(languagesToRemove, chosenLanguages.Count);

            Log.Info($"[LANG-DM] Languages to remove: {languagesToRemove}, Actual remove count: {actualRemoveCount}");

            if (actualRemoveCount > 0)
            {
                // Save the full list to LANGUAGES_SAVED before modifying
                NWN.Core.NWScript.SetLocalString(pcKeyId, "LANGUAGES_SAVED", chosenStr);
                Log.Info($"[LANG-DM] Saved full list to LANGUAGES_SAVED: '{chosenStr}'");

                // Remove languages from the end (last chosen)
                List<string> removedLanguages = new();
                for (int i = 0; i < actualRemoveCount; i++)
                {
                    int lastIndex = chosenLanguages.Count - 1;
                    string removedLang = chosenLanguages[lastIndex];
                    removedLanguages.Add(removedLang);
                    chosenLanguages.RemoveAt(lastIndex);
                    Log.Info($"[LANG-DM] Removed language #{i + 1}: '{removedLang}'");
                }

                // Save the updated chosen languages
                string updatedChosenStr = string.Join("|", chosenLanguages);
                NWN.Core.NWScript.SetLocalString(pcKeyId, "LANGUAGES_CHOSEN", updatedChosenStr);
                Log.Info($"[LANG-DM] Updated LANGUAGES_CHOSEN to: '{updatedChosenStr}'");

                // Notify the player
                string removedList = string.Join(", ", removedLanguages);
                targetPlayer.SendServerMessage($"You have lost access to one or more languages: {removedList}", ColorConstants.Orange);
                _player.SendServerMessage($"Removed {actualRemoveCount} language(s) from {SelectedCharacter.Name}: {removedList}", ColorConstants.Green);
                Log.Info($"[LANG-DM] Notified player of removed languages: {removedList}");
            }
            else
            {
                Log.Info($"[LANG-DM] actualRemoveCount was 0, no languages removed");
            }
        }
        else
        {
            Log.Info($"[LANG-DM] No language slots lost (current {currentMaxLanguages} >= previous {previousTotal}), no action taken");
        }
    }

    private int CalculateMaxLanguagesForCharacter()
    {
        if (SelectedCharacter == null) return 0;

        // Get base Intelligence modifier (without gear)
        int baseInt = SelectedCharacter.GetRawAbilityScore(Ability.Intelligence);
        int intModifier = (baseInt - 10) / 2;
        Log.Info($"[LANG-DM-CALC] Base INT: {baseInt}, INT Modifier: {intModifier}");

        // Start with INT modifier only
        int totalLanguages = Math.Max(0, intModifier);
        Log.Info($"[LANG-DM-CALC] Starting total (INT mod): {totalLanguages}");

        // Add bonus from Lore skill (1 bonus per 10 base ranks)
        int loreRank = NWN.Core.NWScript.GetSkillRank(NWN.Core.NWScript.SKILL_LORE, (uint)SelectedCharacter, NWN.Core.NWScript.TRUE);
        Log.Info($"[LANG-DM-CALC] Base Lore Rank: {loreRank}");
        int loreBonus = loreRank / 10;
        totalLanguages += loreBonus;
        Log.Info($"[LANG-DM-CALC] Lore Bonus: {loreBonus}, Running Total: {totalLanguages}");

        // Add bonus from Epic Skill Focus: Lore feat (feat 492)
        bool hasEpicLore = NWN.Core.NWScript.GetHasFeat(492, (uint)SelectedCharacter) == NWN.Core.NWScript.TRUE;
        Log.Info($"[LANG-DM-CALC] Checking Epic Skill Focus: Lore - Found: {hasEpicLore}");
        if (hasEpicLore)
        {
            totalLanguages += 1;
            Log.Info($"[LANG-DM-CALC] Has Epic Skill Focus: Lore, Running Total: {totalLanguages}");
        }
        else
        {
            Log.Info($"[LANG-DM-CALC] Does NOT have Epic Skill Focus: Lore");
        }

        // Add bonus from Bard class (5+ levels = 1 bonus)
        int bardLevel = GetClassLevel(35); // Bard is class 35
        Log.Info($"[LANG-DM-CALC] Bard Level: {bardLevel}");
        int bardBonus = bardLevel >= 5 ? 1 : 0;
        totalLanguages += bardBonus;
        Log.Info($"[LANG-DM-CALC] Bard Bonus: {bardBonus}, Final Total: {totalLanguages}");

        return totalLanguages;
    }

    private int GetClassLevel(int classType)
    {
        if (SelectedCharacter == null) return 0;

        int totalLevels = SelectedCharacter.Level;
        int classLevels = 0;

        Log.Info($"[LANG-DM-CLASS] Checking for class type {classType} across {totalLevels} levels");

        for (int level = 1; level <= totalLevels; level++)
        {
            CreatureLevelInfo levelInfo = SelectedCharacter.GetLevelStats(level);
            int levelClassType = (int)levelInfo.ClassInfo.Class.ClassType;

            if (levelClassType == classType)
            {
                classLevels++;
            }
        }

        Log.Info($"[LANG-DM-CLASS] Found {classLevels} levels of class type {classType}");
        return classLevels;
    }

    private void RemoveHeritageFeat(NwPlayer targetPlayer)
    {
        if (SelectedCharacter == null) return;

        // Check for Heritage Feat (feat 1238)
        NwFeat? heritageFeat = NwFeat.FromFeatId(1238);
        bool hasHeritageFeat = heritageFeat != null && SelectedCharacter.KnowsFeat(heritageFeat);

        if (!hasHeritageFeat) return;

        // Find PC Key
        uint pcKeyId = NWN.Core.NWScript.GetItemPossessedBy((uint)SelectedCharacter, "ds_pckey");

        if (NWN.Core.NWScript.GetIsObjectValid(pcKeyId) == NWN.Core.NWScript.TRUE)
        {
            NWN.Core.NWScript.DeleteLocalInt(pcKeyId, "heritage_setup");
            _player.SendServerMessage("Removed heritage_setup variable from PC Key.", ColorConstants.Green);
        }

        int playerRace = ResolvePlayerRace(targetPlayer);

        if (ManagedRaces.RaceHeritageAbilities.ContainsKey(playerRace))
        {
            ManagedRaces.RaceHeritageAbilities[playerRace].RemoveStats(targetPlayer);
            _player.SendServerMessage($"Removed heritage abilities.", ColorConstants.Green);
        }

        SelectedCharacter.RemoveFeat(heritageFeat!, true);
        _player.SendServerMessage($"Removed heritage feat.", ColorConstants.Green);
    }
}


﻿using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterArchive;

/// <summary>
/// Service to handle character file operations and BIC file reading.
/// </summary>
[ServiceBinding(typeof(CharacterArchiveService))]
public class CharacterArchiveService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const string VaultDir = "/nwn/home/servervault/";
    private const string ArchiveDir = "/archive/";

    /// <summary>
    /// Gets all character files from the player's vault (excluding .bak files).
    /// </summary>
    public List<CharacterFileInfo> GetVaultCharacters(string cdkey)
    {
        string vaultPath = VaultDir + cdkey + "/";
        return GetCharacterFiles(vaultPath, true);
    }

    /// <summary>
    /// Gets all character files from the player's archive (excluding .bak files).
    /// </summary>
    public List<CharacterFileInfo> GetArchiveCharacters(string cdkey)
    {
        string archivePath = VaultDir + cdkey + ArchiveDir;
        return GetCharacterFiles(archivePath, false);
    }

    /// <summary>
    /// Moves a character file from vault to archive.
    /// </summary>
    public bool MoveToArchive(string cdkey, string fileName)
    {
        try
        {
            string sourcePath = VaultDir + cdkey + "/" + fileName;
            string destPath = VaultDir + cdkey + ArchiveDir;

            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            File.Move(sourcePath, destPath + fileName);
            Log.Info($"Moved {fileName} to archive for {cdkey}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to move {fileName} to archive for {cdkey}");
            return false;
        }
    }

    /// <summary>
    /// Moves a character file from archive to vault.
    /// </summary>
    public bool MoveToVault(string cdkey, string fileName)
    {
        try
        {
            string sourcePath = VaultDir + cdkey + ArchiveDir + fileName;
            string destPath = VaultDir + cdkey + "/" + fileName;

            File.Move(sourcePath, destPath);
            Log.Info($"Moved {fileName} to vault for {cdkey}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to move {fileName} to vault for {cdkey}");
            return false;
        }
    }

    private List<CharacterFileInfo> GetCharacterFiles(string path, bool isVault)
    {
        List<CharacterFileInfo> characters = new();

        if (!Directory.Exists(path))
        {
            Log.Warn($"Directory does not exist: {path}");
            return characters;
        }

        try
        {
            string[] files = Directory.GetFiles(path, "*.bic");

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);

                // Skip .bak files
                if (fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                CharacterFileInfo charInfo = new()
                {
                    FileName = fileName,
                    CharacterName = Path.GetFileNameWithoutExtension(fileName), // Fallback
                    FullPath = file,
                    IsInVault = isVault
                };

                // Try to read character info (name and portrait) from BIC file
                var (actualName, portraitResRef) = ReadCharacterInfoFromBic(file);
                if (!string.IsNullOrEmpty(actualName))
                {
                    charInfo.CharacterName = actualName;
                }
                if (!string.IsNullOrEmpty(portraitResRef))
                {
                    charInfo.PortraitResRef = portraitResRef;
                }

                characters.Add(charInfo);
            }

            Log.Info($"Found {characters.Count} character files in {path}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error reading character files from {path}");
        }

        return characters;
    }

    /// <summary>
    /// Attempts to read character info (name and portrait) from a BIC file by temporarily creating
    /// a creature from the BIC and reading its properties.
    /// </summary>
    /// <returns>Tuple of (CharacterName, PortraitResRef) or (null, null) if failed</returns>
    private (string?, string?) ReadCharacterInfoFromBic(string filePath)
    {
        try
        {
            // Find the spawn waypoint
            NwWaypoint? spawnWaypoint = NwObject.FindObjectsWithTag<NwWaypoint>("ds_copy").FirstOrDefault();

            if (spawnWaypoint == null)
            {
                Log.Warn("Could not find waypoint 'ds_copy' for temporary character creation");
                return (null, null);
            }

            // Read the BIC file as bytes
            byte[] bicData = File.ReadAllBytes(filePath);

            // Deserialize the creature from the BIC data
            NwCreature? tempCreature = NwCreature.Deserialize(bicData);

            if (tempCreature == null)
            {
                Log.Debug($"Failed to deserialize creature from {Path.GetFileName(filePath)}");
                return (null, null);
            }

            // Move creature to the waypoint location so it exists in the world
            tempCreature.Location = spawnWaypoint.Location;

            // Read the character name and portrait from the creature
            string characterName = tempCreature.Name;
            string portraitResRef = tempCreature.PortraitResRef;

            // Clean up - destroy the temporary creature immediately
            tempCreature.Destroy();

            Log.Info($"Read character '{characterName}' with portrait '{portraitResRef}' from {Path.GetFileName(filePath)}");

            return (characterName, portraitResRef);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, $"Could not read character info from {filePath}");
        }

        return (null, null);
    }
}


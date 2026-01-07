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
    /// Attempts to read character info (name and portrait) from a BIC file by parsing the GFF structure.
    /// This reads the binary GFF format directly without creating any game objects.
    /// </summary>
    /// <returns>Tuple of (CharacterName, PortraitResRef) or (null, null) if failed</returns>
    private (string?, string?) ReadCharacterInfoFromBic(string filePath)
    {
        try
        {
            byte[] data = File.ReadAllBytes(filePath);

            // Validate GFF header
            if (data.Length < 56 ||
                data[0] != 'B' || data[1] != 'I' || data[2] != 'C' || data[3] != ' ')
            {
                Log.Debug($"Invalid GFF header in {Path.GetFileName(filePath)}");
                return (null, null);
            }

            // Parse GFF structure
            var gff = new GffParser(data);

            string? firstName = gff.GetCExoLocString("FirstName");
            string? lastName = gff.GetCExoLocString("LastName");
            string? portrait = gff.GetResRef("Portrait");

            // Construct character name
            string? characterName = null;
            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            {
                characterName = $"{firstName} {lastName}".Trim();
            }
            else if (!string.IsNullOrWhiteSpace(firstName))
            {
                characterName = firstName.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(lastName))
            {
                characterName = lastName.Trim();
            }

            if (!string.IsNullOrEmpty(characterName) || !string.IsNullOrEmpty(portrait))
            {
                Log.Info($"Read character '{characterName ?? "Unknown"}' with portrait '{portrait ?? "default"}' from {Path.GetFileName(filePath)}");
            }

            return (characterName, portrait);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, $"Could not read character info from {filePath}");
            return (null, null);
        }
    }

    /// <summary>
    /// Simple GFF parser for reading specific fields from BIC files.
    /// </summary>
    private class GffParser
    {
        private readonly byte[] _data;
        private readonly int _structOffset;
        private readonly int _structCount;
        private readonly int _fieldOffset;
        private readonly int _fieldCount;
        private readonly int _labelOffset;
        private readonly int _fieldDataOffset;
        private readonly int _fieldIndicesOffset;

        public GffParser(byte[] data)
        {
            _data = data;

            // Read GFF header offsets (all little-endian)
            _structOffset = ReadInt32(8);
            _structCount = ReadInt32(12);
            _fieldOffset = ReadInt32(16);
            _fieldCount = ReadInt32(20);
            _labelOffset = ReadInt32(24);
            // LabelCount at 28
            _fieldDataOffset = ReadInt32(32);
            // FieldDataCount at 36
            _fieldIndicesOffset = ReadInt32(40);
            // FieldIndicesCount at 44
            // ListIndicesOffset at 48
            // ListIndicesCount at 52
        }

        public string? GetCExoLocString(string fieldName)
        {
            int? fieldIndex = FindFieldInStruct(0, fieldName);
            if (fieldIndex == null) return null;

            int fieldPos = _fieldOffset + (fieldIndex.Value * 12);
            int fieldType = ReadInt32(fieldPos);
            int dataOrDataOffset = ReadInt32(fieldPos + 8);

            // Type 12 = CExoLocString
            if (fieldType != 12) return null;

            // For CExoLocString, the data is in the Field Data block
            int locStringPos = _fieldDataOffset + dataOrDataOffset;

            if (locStringPos + 12 > _data.Length) return null;

            // CExoLocString structure: TotalSize(4), StringRef(4), StringCount(4), then string entries
            int stringCount = ReadInt32(locStringPos + 8);

            if (stringCount > 0)
            {
                // Read first string entry: ID(4), Length(4), String(Length)
                int stringEntryPos = locStringPos + 12;

                if (stringEntryPos + 8 > _data.Length) return null;

                int stringLength = ReadInt32(stringEntryPos + 4);

                if (stringLength > 0 && stringLength < 1024 && stringEntryPos + 8 + stringLength <= _data.Length)
                {
                    return System.Text.Encoding.UTF8.GetString(_data, stringEntryPos + 8, stringLength);
                }
            }

            return null;
        }

        public string? GetResRef(string fieldName)
        {
            int? fieldIndex = FindFieldInStruct(0, fieldName);
            if (fieldIndex == null) return null;

            int fieldPos = _fieldOffset + (fieldIndex.Value * 12);
            int fieldType = ReadInt32(fieldPos);
            int dataOrDataOffset = ReadInt32(fieldPos + 8);

            // Type 11 = ResRef
            if (fieldType != 11) return null;

            // For ResRef, the data is in the Field Data block
            int resRefPos = _fieldDataOffset + dataOrDataOffset;

            if (resRefPos >= _data.Length) return null;

            // ResRef: Length(1), String(Length) - max 16 chars
            int resRefLength = _data[resRefPos];

            if (resRefLength > 0 && resRefLength <= 16 && resRefPos + 1 + resRefLength <= _data.Length)
            {
                return System.Text.Encoding.ASCII.GetString(_data, resRefPos + 1, resRefLength);
            }

            return null;
        }

        private int? FindFieldInStruct(int structIndex, string fieldName)
        {
            if (structIndex >= _structCount) return null;

            int structPos = _structOffset + (structIndex * 12);
            // int structType = ReadInt32(structPos);
            int dataOrDataOffset = ReadInt32(structPos + 4);
            int fieldCount = ReadInt32(structPos + 8);

            // Find the field in the struct's field indices
            for (int i = 0; i < fieldCount; i++)
            {
                int fieldIndexPos = _fieldIndicesOffset + (dataOrDataOffset + i) * 4;
                int fieldIndex = ReadInt32(fieldIndexPos);

                if (fieldIndex >= _fieldCount) continue;

                int fieldPos = _fieldOffset + (fieldIndex * 12);
                // int fieldType = ReadInt32(fieldPos);
                int labelIndex = ReadInt32(fieldPos + 4);
                // int fieldDataOrOffset = ReadInt32(fieldPos + 8);

                // Check label
                if (labelIndex < 16384) // Max label count
                {
                    int labelPos = _labelOffset + (labelIndex * 16);
                    string label = ReadNullTerminatedString(labelPos, 16);

                    if (label == fieldName)
                    {
                        return fieldIndex;
                    }
                }
            }

            return null;
        }

        private int ReadInt32(int offset)
        {
            return _data[offset] |
                   (_data[offset + 1] << 8) |
                   (_data[offset + 2] << 16) |
                   (_data[offset + 3] << 24);
        }

        private string ReadNullTerminatedString(int offset, int maxLength)
        {
            int length = 0;
            while (length < maxLength && offset + length < _data.Length && _data[offset + length] != 0)
            {
                length++;
            }
            return System.Text.Encoding.ASCII.GetString(_data, offset, length);
        }
    }
}

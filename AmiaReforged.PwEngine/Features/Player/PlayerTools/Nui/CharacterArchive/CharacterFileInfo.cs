namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterArchive;

/// <summary>
/// Represents a character file in the vault or archive.
/// </summary>
public class CharacterFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public string PortraitResRef { get; set; } = "po_hu_m_01_";
    public string FullPath { get; set; } = string.Empty;
    public bool IsInVault { get; set; }
}



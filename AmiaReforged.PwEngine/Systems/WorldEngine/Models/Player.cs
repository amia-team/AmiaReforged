namespace AmiaReforged.PwEngine.Systems.WorldEngine.Models;

public class Player
{
    public required string Key { get; set; }
    public List<Character> Characters { get; set; } = [];
    public bool LoggedInAsDm { get; set; }
}

using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Emotes;

public class EmotesModel
{
    public NwPlayer Player { get; }
    public NwGameObject? SelectedTarget { get; set; }

    // Individual emotes (cases 1-28, 48)
    public List<EmoteOption> IndividualEmotes { get; } = new()
    {
        new EmoteOption(1, "Dodge Side"),
        new EmoteOption(2, "Drink"),
        new EmoteOption(3, "Duck"),
        new EmoteOption(4, "Dead Back"),
        new EmoteOption(5, "Dead Front"),
        new EmoteOption(6, "Read"),
        new EmoteOption(7, "Sit Down"),
        new EmoteOption(11, "Plead"),
        new EmoteOption(12, "Conjure Hands"),
        new EmoteOption(13, "Conjure Above"),
        new EmoteOption(14, "Get Low"),
        new EmoteOption(15, "Get Mid"),
        new EmoteOption(16, "Meditate"),
        new EmoteOption(17, "Talk Forceful"),
        new EmoteOption(18, "Worship"),
        new EmoteOption(21, "Frenzy"),
        new EmoteOption(22, "Drunk"),
        new EmoteOption(24, "Sit in Chair"),
        new EmoteOption(25, "Sit and Drink"),
        new EmoteOption(26, "Sit and Read"),
        new EmoteOption(27, "Spasm"),
        new EmoteOption(28, "Smoke Pipe"),
        new EmoteOption(48, "Dance (Female)")
    };

    // Mutual emotes (cases 38-47)
    public List<EmoteOption> MutualEmotes { get; } = new()
    {
        new EmoteOption(38, "Kiss Standing"),
        new EmoteOption(39, "Kiss Lying Down"),
        new EmoteOption(40, "Hug"),
        new EmoteOption(41, "Hug from Behind"),
        new EmoteOption(42, "Back Close"),
        new EmoteOption(43, "Waltz"),
        new EmoteOption(44, "Lap Sit (Ground, Facing)"),
        new EmoteOption(45, "Lap Sit (Ground, Away)"),
        new EmoteOption(46, "Lap Sit (Chair, Facing)"),
        new EmoteOption(47, "Lap Sit (Chair, Away)")
    };

    public int SelectedIndividualEmote { get; set; } = 0;
    public int SelectedMutualEmote { get; set; } = 0;

    public EmotesModel(NwPlayer player)
    {
        Player = player;
    }
}

public class EmoteOption
{
    public int Id { get; }
    public string Name { get; }

    public EmoteOption(int id, string name)
    {
        Id = id;
        Name = name;
    }
}

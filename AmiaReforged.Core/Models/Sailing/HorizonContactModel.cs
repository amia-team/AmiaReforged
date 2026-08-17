namespace AmiaReforged.Core.Models.Sailing;

public class HorizonContact
{
    public Guid EncounterId { get; set; }

    public string Name { get; set; } = "";

    public float Distance { get; set; }

    public float RelativeAngle { get; set; }

    public EncounterType Type { get; set; }
}
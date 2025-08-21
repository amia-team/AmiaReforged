namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public sealed class ReactionResult
{
    public bool Succeeded { get; }
    public TimeSpan Duration { get; }
    public IReadOnlyList<Quantity> Produced { get; }
    public IReadOnlyList<string> Notes { get; }

    public ReactionResult(bool succeeded, TimeSpan duration, IReadOnlyList<Quantity> produced, IReadOnlyList<string> notes)
    {
        Succeeded = succeeded;
        Duration = duration;
        Produced = produced;
        Notes = notes;
    }
}
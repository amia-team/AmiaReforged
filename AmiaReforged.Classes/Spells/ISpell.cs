namespace AmiaReforged.Classes.Spells;

public interface ISpell
{
    void Trigger();
}

public class SpellCastResult
{
    
}

public enum CastResultEnum
{
    Success,
    Interrupted,
    CriticalFailure,
    SpellResisted,
}
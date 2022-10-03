using Anvil.API;

namespace AmiaReforged.Classes.Spells.Invocations.Pact.Types.Contracts;

public interface IMobilityStrategy
{
    /// <summary>
    /// Move the caster from point A to point B using whatever strategy the implementing object desires.
    /// </summary>
    /// <param name="caster">The caster of wlk_mobility.</param>
    /// <param name="location">The location selected by the caster.</param>
    void Move(NwCreature caster, Location location);
}
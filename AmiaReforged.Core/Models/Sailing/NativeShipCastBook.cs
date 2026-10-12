namespace AmiaReforged.Core.Models.Sailing;

/// <summary>Captures explicit targeting at cast start and permits one impact per engine cast.</summary>
public sealed class NativeShipCastBook
{
    public sealed record Cast(int SpellId, ShipState Source, ShipState? Target, DateTime StartedAt);
    private readonly Dictionary<string, Cast> _pending = new(StringComparer.Ordinal);
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);

    public void Begin(string player, int spellId, ShipState source, ShipState? target, DateTime now)
        => _pending[player] = new(spellId, source, target, now);

    public bool TryComplete(string player, int spellId, ShipState source, DateTime now, out Cast? cast)
    {
        cast = null;
        if (!_pending.TryGetValue(player, out Cast? pending)) return false;
        if (now - pending.StartedAt >= Lifetime || now < pending.StartedAt || !ReferenceEquals(pending.Source, source))
        { _pending.Remove(player); return false; }
        if (pending.SpellId != spellId) return false;
        _pending.Remove(player);
        cast = pending;
        return true;
    }

    public void Cancel(string player) => _pending.Remove(player);
    public void Clear() => _pending.Clear();
}

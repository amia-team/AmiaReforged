using System.Globalization;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>
/// Per-node execution facade passed to <see cref="GlyphNodeBase.RunAsync"/>.
/// Wraps the raw <c>resolveInput</c> delegate with typed accessors so node authors
/// never touch <c>object?</c> or <c>Convert.ToX</c> directly. Unresolvable or
/// mistyped values fall back to the caller's default instead of throwing.
/// </summary>
public sealed class GlyphNodeContext
{
    private readonly Func<string, Task<object?>> _resolve;

    public GlyphNodeContext(
        GlyphNodeInstance node,
        GlyphExecutionContext execution,
        Func<string, Task<object?>> resolve)
    {
        Node = node;
        Execution = execution;
        _resolve = resolve;
    }

    /// <summary>
    /// The node instance being executed.
    /// </summary>
    public GlyphNodeInstance Node { get; }

    /// <summary>
    /// The mutable execution state. Escape hatch for control flags
    /// (e.g. <c>ShouldCancelSpawn</c>); prefer typed inputs for data.
    /// </summary>
    public GlyphExecutionContext Execution { get; }

    /// <summary>
    /// Raw untyped resolution, for list payloads and other non-scalar pins.
    /// Prefer the typed helpers below for scalar pins.
    /// </summary>
    public Task<object?> Raw(string pinId) => _resolve(pinId);

    /// <summary>
    /// Resolves a scalar input pin, falling back when unconnected or mistyped.
    /// </summary>
    public async Task<T> In<T>(string pinId, T fallback = default!)
    {
        object? raw = await _resolve(pinId);
        return ConvertTo(raw, fallback);
    }

    public Task<bool> InBool(string pinId, bool fallback = false) => In(pinId, fallback);

    public Task<int> InInt(string pinId, int fallback = 0) => In(pinId, fallback);

    public Task<double> InFloat(string pinId, double fallback = 0) => In(pinId, fallback);

    public Task<string> InString(string pinId, string fallback = "") =>
        In<string>(pinId, fallback);

    /// <summary>
    /// Resolves an NWN object pin. Defaults to <see cref="NWScript.OBJECT_INVALID"/>
    /// so unconnected pins fail the standard validity guard instead of resolving to 0.
    /// </summary>
    public Task<uint> InObject(string pinId, uint? fallback = null) =>
        In(pinId, fallback ?? NWScript.OBJECT_INVALID);

    /// <summary>
    /// Reads a static property override (editor property-panel value) for this node.
    /// </summary>
    public T Prop<T>(string name, T fallback = default!)
    {
        if (Node.PropertyOverrides.TryGetValue(name, out string? raw))
        {
            return ConvertTo((object?)raw, fallback);
        }

        return fallback;
    }

    internal static T ConvertTo<T>(object? value, T fallback)
    {
        if (value is null)
        {
            return fallback;
        }

        if (value is T direct)
        {
            return direct;
        }

        try
        {
            Type target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (target == typeof(string))
            {
                return (T)(object)(value.ToString() ?? string.Empty);
            }

            if (target == typeof(bool))
            {
                if (value is string s)
                {
                    if (bool.TryParse(s, out bool parsed))
                    {
                        return (T)(object)parsed;
                    }

                    if (s == "1")
                    {
                        return (T)(object)true;
                    }

                    if (s == "0")
                    {
                        return (T)(object)false;
                    }

                    return fallback;
                }

                return (T)(object)Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            }

            return (T)Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            return fallback;
        }
    }
}

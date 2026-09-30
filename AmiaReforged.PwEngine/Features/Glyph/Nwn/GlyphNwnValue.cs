using NWN.Core;
namespace AmiaReforged.PwEngine.Features.Glyph.Nwn;

/// <summary>Typed engine values. Native pointers are never exposed as Glyph integers or CLR members.</summary>
public readonly record struct GlyphNwnLocation(IntPtr Handle);
public readonly record struct GlyphNwnEffect(IntPtr Handle);

public static class GlyphNwnValue
{
    public static uint NormalizeObject(uint handle) => handle == 0 ? NWScript.OBJECT_INVALID : handle;
}

public sealed record GlyphNwnBindingCoverage(string Member, string Signature, string Status, string? Name, string Reason);
public sealed record GlyphNwnConstantCoverage(string Member, string Type, string Value, string? Name);
public sealed record GlyphNwnConstant(string Name, string Namespace, string Type, object Value, string Source, string Description);

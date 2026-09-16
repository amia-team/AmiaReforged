namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// One-line pin factories for node definitions. Removes the repetitive
/// <c>new GlyphPin { Id = ..., Name = ..., DataType = ..., Direction = ... }</c>
/// boilerplate from every <c>CreateDefinition()</c>.
/// </summary>
public static class Pins
{
    public static GlyphPin ExecIn(string id = "exec_in", string name = "Execute") => new()
    {
        Id = id,
        Name = name,
        DataType = GlyphDataType.Exec,
        Direction = GlyphPinDirection.Input,
    };

    public static GlyphPin ExecOut(string id, string name) => new()
    {
        Id = id,
        Name = name,
        DataType = GlyphDataType.Exec,
        Direction = GlyphPinDirection.Output,
    };

    public static GlyphPin In(string id, string name, GlyphDataType type, string? defaultValue = null) => new()
    {
        Id = id,
        Name = name,
        DataType = type,
        Direction = GlyphPinDirection.Input,
        DefaultValue = defaultValue,
    };

    public static GlyphPin Out(string id, string name, GlyphDataType type) => new()
    {
        Id = id,
        Name = name,
        DataType = type,
        Direction = GlyphPinDirection.Output,
    };

    public static GlyphPin InBool(string id, string name, string? defaultValue = "false") =>
        In(id, name, GlyphDataType.Bool, defaultValue);

    public static GlyphPin InInt(string id, string name, string? defaultValue = "0") =>
        In(id, name, GlyphDataType.Int, defaultValue);

    public static GlyphPin InFloat(string id, string name, string? defaultValue = "0") =>
        In(id, name, GlyphDataType.Float, defaultValue);

    public static GlyphPin InString(string id, string name, string? defaultValue = null) =>
        In(id, name, GlyphDataType.String, defaultValue);

    public static GlyphPin InObject(string id, string name) =>
        In(id, name, GlyphDataType.NwObject);
}

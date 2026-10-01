using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

public sealed record GlyphTypeSymbol(
    string Name,
    GlyphDataType? RuntimeType = null,
    GlyphTypeSymbol? ElementType = null,
    GlyphTypeSymbol? KeyType = null,
    GlyphTypeSymbol? ValueType = null)
{
    public bool IsTypeParameter { get; init; }

    public static readonly GlyphTypeSymbol
        Error = new("Error"),
        Void = new("Void"),
        Bool = new("Bool", GlyphDataType.Bool),
        Int = new("Int", GlyphDataType.Int),
        Float = new("Float", GlyphDataType.Float),
        String = new("String", GlyphDataType.String),
        Object = new("Object", GlyphDataType.NwObject),
        Location = new("Location", GlyphDataType.Location),
        Effect = new("Effect", GlyphDataType.Effect),
        Objects = new("List<Object>", GlyphDataType.List, Object),
        Effects = new("List<Effect>", GlyphDataType.List, Effect);

    public static GlyphTypeSymbol From(GlyphPin pin) => pin.DataType == GlyphDataType.Aggregate && pin.AggregateTypeName != null
        ? new(pin.AggregateTypeName, GlyphDataType.Aggregate)
        : pin.DataType == GlyphDataType.Dictionary
        ? Dictionary(From(pin.KeyType!.Value), From(pin.ValueType!.Value))
        : pin.DataType == GlyphDataType.List
        ? new($"List<{From(pin.ElementType ?? GlyphDataType.NwObject).Name}>", GlyphDataType.List, From(pin.ElementType ?? GlyphDataType.NwObject))
        : From(pin.DataType);

    public static GlyphTypeSymbol From(GlyphDataType type) => type switch
    {
        GlyphDataType.Bool => Bool,
        GlyphDataType.Int => Int,
        GlyphDataType.Float => Float,
        GlyphDataType.String => String,
        GlyphDataType.NwObject => Object,
        GlyphDataType.List => Objects,
        GlyphDataType.Location => Location,
        GlyphDataType.Effect => Effect,
        _ => new(type.ToString(), type)
    };

    public static GlyphTypeSymbol List(GlyphTypeSymbol element) => new($"List<{element.Name}>", GlyphDataType.List, element);
    public static GlyphTypeSymbol Dictionary(GlyphTypeSymbol key, GlyphTypeSymbol value) => new($"Dictionary<{key.Name}, {value.Name}>", GlyphDataType.Dictionary, null, key, value);
    public bool IsCollection => RuntimeType is GlyphDataType.List or GlyphDataType.Dictionary;
    public bool IsBasic => RuntimeType is GlyphDataType.NwObject or GlyphDataType.String or GlyphDataType.Int or GlyphDataType.Float or GlyphDataType.Bool;

    public bool IsNumeric => RuntimeType is GlyphDataType.Int or GlyphDataType.Float;
}

public sealed record GlyphFieldSymbol(string Name, GlyphTypeSymbol Type);

public sealed record GlyphStructDefinition(
    string Name,
    GlyphTypeSymbol Type,
    IReadOnlyList<GlyphFieldSymbol> Fields);

public sealed record GlyphVariantDefinition(
    string Name,
    IReadOnlyList<GlyphFieldSymbol> Fields);

public sealed record GlyphAdtDefinition(
    string Name,
    GlyphTypeSymbol Type,
    IReadOnlyList<GlyphVariantDefinition> Variants);

public sealed record BoundProgram(string Name, GlyphEventType Event, IReadOnlyList<BoundStage> Stages) { public int LanguageVersion { get; init; } = 3; }
public sealed record BoundStage(string EntryTypeId, BoundBlock Body, SourceSpan Span);

public abstract record BoundStatement(SourceSpan Span);
public sealed record BoundBlock(IReadOnlyList<BoundStatement> Statements, SourceSpan Span) : BoundStatement(Span);
public sealed record BoundIf(BoundExpression Condition, BoundStatement Then, BoundStatement? Else, SourceSpan Span) : BoundStatement(Span);
public sealed record BoundForeach(int SymbolId, BoundExpression List, BoundBlock Body, SourceSpan Span) : BoundStatement(Span);
public sealed record BoundBreak(SourceSpan Span) : BoundStatement(Span);
public sealed record BoundExpressionStatement(BoundCall Call, SourceSpan Span) : BoundStatement(Span);

public abstract record BoundExpression(GlyphTypeSymbol Type, SourceSpan Span);
public sealed record BoundError(SourceSpan Span) : BoundExpression(GlyphTypeSymbol.Error, Span);
public sealed record BoundLiteral(object Value, GlyphTypeSymbol Type, SourceSpan Span) : BoundExpression(Type, Span);
public sealed record BoundContext(string EntryTypeId, string Pin, GlyphTypeSymbol Type, SourceSpan Span) : BoundExpression(Type, Span);
public sealed record BoundLoopElement(int SymbolId, SourceSpan Span, GlyphTypeSymbol? ElementType = null) : BoundExpression(ElementType ?? GlyphTypeSymbol.Object, Span);
public sealed record BoundStoredValue(int SymbolId, GlyphTypeSymbol ValueType, SourceSpan Span) : BoundExpression(ValueType, Span);
public sealed record BoundLet(int SymbolId, BoundExpression Value, SourceSpan Span) : BoundStatement(Span);
public sealed record BoundCall(GlyphLanguageSymbol Symbol, IReadOnlyDictionary<string, BoundExpression> Arguments, SourceSpan Span) : BoundExpression(Symbol.ReturnType, Span);
public sealed record BoundUnary(string Operator, BoundExpression Operand, GlyphTypeSymbol Type, SourceSpan Span) : BoundExpression(Type, Span);
public sealed record BoundBinary(BoundExpression Left, string Operator, BoundExpression Right, GlyphTypeSymbol Type, SourceSpan Span) : BoundExpression(Type, Span);

public sealed record BoundStruct(
    GlyphStructDefinition Definition,
    IReadOnlyDictionary<string, BoundExpression> Fields,
    SourceSpan Span) : BoundExpression(Definition.Type, Span);

public sealed record BoundVariant(
    GlyphAdtDefinition Definition,
    GlyphVariantDefinition Variant,
    IReadOnlyDictionary<string, BoundExpression> Fields,
    SourceSpan Span) : BoundExpression(Definition.Type, Span);

public sealed record BoundListLiteral(IReadOnlyList<BoundExpression> Values, GlyphTypeSymbol ListType, SourceSpan Span) : BoundExpression(ListType, Span);

public sealed record BoundPlaceholder(
    GlyphTypeSymbol PlaceholderType,
    SourceSpan Span) : BoundExpression(PlaceholderType, Span);

public sealed record BoundSequence(IReadOnlyList<BoundLet> Prefix, BoundExpression Value, SourceSpan Span) : BoundExpression(Value.Type, Span);

public sealed record BoundVar(int SymbolId, BoundExpression Value, SourceSpan Span) : BoundStatement(Span);
public sealed record BoundVariableRead(int SymbolId, GlyphTypeSymbol ValueType, SourceSpan Span, bool Mutable = true) : BoundExpression(ValueType, Span);
public sealed record BoundVariableAssignment(int SymbolId, GlyphTypeSymbol ValueType, BoundExpression Value, SourceSpan Span) : BoundStatement(Span);
public sealed record BoundWhile(BoundExpression Condition, BoundBlock Body, SourceSpan Span) : BoundStatement(Span);
public sealed record BoundForRange(int SymbolId, BoundExpression Start, BoundExpression End, bool Inclusive, BoundExpression? Step, BoundBlock Body, SourceSpan Span) : BoundStatement(Span);
public sealed record BoundContinue(SourceSpan Span) : BoundStatement(Span);
public sealed record BoundReturn(BoundExpression? Value, SourceSpan Span) : BoundStatement(Span);
public sealed record BoundFunctionStatement(BoundExpression Call, SourceSpan Span) : BoundStatement(Span);
public sealed record BoundFunctionCall(IReadOnlyList<BoundVariableAssignment> Arguments, BoundBlock Body, int? ResultSymbol,
    GlyphTypeSymbol ReturnType, SourceSpan Span) : BoundExpression(ReturnType, Span);
public sealed record BoundMatch(int SymbolId, BoundExpression Value, IReadOnlyList<BoundMatchArm> Arms, SourceSpan Span) : BoundStatement(Span);
public sealed record BoundMatchArm(BoundPattern Pattern, BoundBlock Body, SourceSpan Span);
public abstract record BoundPattern;
public sealed record BoundWildcardPattern : BoundPattern;
public sealed record BoundValuePattern(BoundExpression Value) : BoundPattern;
public sealed record BoundVariantPattern(string TypeName, string Variant) : BoundPattern;
public sealed record BoundAggregateField(BoundExpression Receiver, string Field, GlyphTypeSymbol FieldType, SourceSpan Span) : BoundExpression(FieldType, Span);
